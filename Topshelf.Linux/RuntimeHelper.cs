#region license
// Copyright 2013 - Pablo Ruiz Garcia <pablo.ruiz at gmail.com>
//  
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use 
// this file except in compliance with the License. You may obtain a copy of the 
// License at 
// 
//     http://www.apache.org/licenses/LICENSE-2.0 
// 
// Unless required by applicable law or agreed to in writing, software distributed 
// under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR 
// CONDITIONS OF ANY KIND, either express or implied. See the License for the 
// specific language governing permissions and limitations under the License.
#endregion
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

using Topshelf.Logging;

namespace Topshelf.Runtime
{
	internal static class RuntimeHelper
	{
		private readonly static object InitLock = new object();
		private volatile static bool initialized;

		private static readonly Lazy<bool> _RunningOnMono = new Lazy<bool>(DetectMono);
		private static readonly Lazy<bool> _RunningOnWindows = new Lazy<bool>(DetectWindows);
		private static readonly Lazy<bool> _RunningAsSystemdService = new Lazy<bool>(DetectSystemd);

		private static bool runningOnUnix, runningOnMacOS, runningOnLinux;

		/// <summary>Gets a System.Boolean indicating whether running on a Windows platform.</summary>
		public static bool RunningOnWindows => _RunningOnWindows.Value;
		/// <summary> Gets a System.Boolean indicating whether running on the Mono runtime. </summary>
		public static bool RunningOnMono => _RunningOnMono.Value;
		/// <summary> Gets a <see cref="System.Boolean"/> indicating whether running on a Unix platform. </summary>
		public static bool RunningOnUnix { get { Init(); return runningOnUnix; } }
		/// <summary>Gets a System.Boolean indicating whether running on an Linux platform.</summary>
		public static bool RunningOnLinux { get { Init(); return runningOnLinux; } }
		/// <summary>Gets a System.Boolean indicating whether running on a MacOS platform.</summary>
		public static bool RunningOnMacOS { get { Init(); return runningOnMacOS; } }
		/// <summary>Gets a System.Boolean indicating whether running on 64 bit OS.</summary>
		public static bool RunningIn64Bits { get { return IntPtr.Size == 8; } }
		/// <summary>Get a System.Boolean indicating whether running as root.</summary>
		public static bool RunningAsRoot => getuid() == 0; //< TODO: Use Mono.Unix instead (it's safer)
		/// <summary>Get a System.Boolean indicating whether running as a systemd service/unit.</summary>
		public static bool RunningAsSystemdService => _RunningAsSystemdService.Value;

		#region Private Methods

		[DllImport("libc", EntryPoint = "getppid")]
		private static extern int GetParentPid();

		[DllImport("libc")]
		private static extern uint getuid();

		private static bool DetectWindows()
		{
			return
					Environment.OSVersion.Platform == PlatformID.Win32NT ||
					Environment.OSVersion.Platform == PlatformID.Win32S ||
					Environment.OSVersion.Platform == PlatformID.Win32Windows ||
					Environment.OSVersion.Platform == PlatformID.WinCE;
		}

		private static bool DetectMono()
		{
			// Detect the Mono runtime (code taken from http://mono.wikia.com/wiki/Detecting_if_program_is_running_in_Mono).
			Type t = Type.GetType("Mono.Runtime");
			return t != null;
		}

		private static bool DetectSystemd()
		{
			var result = false;

			// No point in testing anything unless it's Unix
			if (Environment.OSVersion.Platform == PlatformID.Unix)
			{
				try
				{
					// First try with systemd's INVOCATION env variable..
					// See: https://www.freedesktop.org/software/systemd/man/systemd.exec.html
					if (Environment.GetEnvironmentVariable("INVOCATION_ID") != null)
						result = true;

					// Units with Type=notify pass NOTIFY_SOCKET env variable..
					if (!result && Environment.GetEnvironmentVariable("NOTIFY_SOCKET") != null)
						result = true;

					// Last resort: check whether our direct parent is 'systemd'.
					if (!result && RunningOnLinux)
					{
						// Based on: https://github.com/dotnet/extensions/blob/master/src/Hosting/Systemd/src/SystemdHelpers.cs
						var pid = GetParentPid();
						var pidstr = pid.ToString(NumberFormatInfo.InvariantInfo);

						if (pid != 1 && Environment.GetEnvironmentVariable("MANAGERPID") != pidstr)
						{
							// Parent is not 1 (init), but MANAGERPID envvar does not match. :(
							return false;
						}

						var proc = File.ReadAllText($"/proc/{pidstr}/comm");
						result = proc.Equals("systemd\n");
					}
				}
				catch (Exception ex)
				{
					HostLogger.Get("Topshelf.Linux").Debug("DetectSystemd failed.", ex);
				}
			}

			return result;
		}

		#region private static string DetectUnixKernel()

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
		struct utsname
		{
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
			public string sysname;

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
			public string nodename;

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
			public string release;

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
			public string version;

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
			public string machine;

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1024)]
			public string extraJustInCase;
		}

		[DllImport("libc")]
		private static extern void uname(out utsname uname_struct);

		/// <summary>
		/// Detects the unix kernel by p/invoking uname (libc).
		/// </summary>
		/// <returns></returns>
		private static string DetectUnixKernel()
		{
			utsname uts = new utsname();
			uname(out uts);

			var logger = HostLogger.Get("Topshelf.Linux");
			if (logger.IsDebugEnabled)
			{
				logger.Debug($"System: {uts.sysname}, {uts.nodename}, {uts.release}, {uts.version}, {uts.machine}");
			}

			return uts.sysname;
		}
		#endregion

		private static void DetectUnix(ref bool unix, ref bool linux, ref bool macos)
		{
			string kernel_name = DetectUnixKernel();
			switch (kernel_name)
			{
				case null:
				case "":
					throw new PlatformNotSupportedException("Unknown platform?!");

				case "Linux":
					linux = unix = true;
					break;

				case "Darwin":
					macos = unix = true;
					break;

				default:
					unix = true;
					break;
			}
		}

		private static void Init()
		{
			lock (InitLock)
			{
				if (!initialized)
				{
					initialized = true;

					if (!RunningOnWindows)
					{
						DetectUnix(ref runningOnUnix, ref runningOnLinux, ref runningOnMacOS);
					}
				}
			}
		}


		#endregion

		public static bool RunningUnderMonoService
		{
			get
			{
				var args = GetArgs();
				return args.Peek().EndsWith("mono-service.exe");
			}
		}

		private static Stack<string> GetArgs()
		{
			return new Stack<string>((Environment.GetCommandLineArgs() ?? new string[] { }).Reverse());
		}

		public static string GetUnparsedCommandLine()
		{
			var args = GetArgs();
			string commandLine = Environment.CommandLine;
			string exeName = args.Peek();

			if (exeName == null) return commandLine;

			// If we are being run under mono-service, strip
			// mono-service.exe + arguments from cmdline.
			// NOTE: mono-service.exe passes itself as first arg.
			if (RunningUnderMonoService)
			{
				commandLine = commandLine.Substring(exeName.Length).TrimStart();
				do
				{
					args.Pop();
				} while (args.Count > 0 && args.Peek().StartsWith("-"));
				exeName = args.Peek();
			}

			// Now strip real program's executable name from cmdline.

			// Let's try first with a quoted executable..
			var qExeName = "\"" + exeName + "\"";
			if (commandLine.IndexOf(qExeName) > 0)
			{
				commandLine = commandLine.Substring(commandLine.IndexOf(qExeName) + qExeName.Length);
			}
			else
			{
				commandLine = commandLine.Substring(commandLine.IndexOf(exeName) + exeName.Length);
			}

			return (commandLine ?? "").Trim();
		}
	}
}
