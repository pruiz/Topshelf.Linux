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
using System.Linq;
using System.Runtime.InteropServices;

namespace Topshelf.Runtime.Linux
{
	internal static class MonoHelper
	{
		[DllImport("libc")]
		private static extern uint getuid();

		public static bool RunningAsRoot
		{
			// TODO: Use Mono.Unix instead (it's safer)
			get { return getuid() == 0; }
		}

		public static bool RunningOnMono
		{
			get
			{
				Type t = Type.GetType("Mono.Runtime");
				if (t != null)
					return true;

				return false;
			}
		}

		public static bool RunninOnUnix
		{
			get
			{
				int p = (int)Environment.OSVersion.Platform;
				return ((p == 4) || (p == 6) || (p == 128));
			}
		}

		public static bool RunninOnLinux
		{
			get
			{
				int p = (int)Environment.OSVersion.Platform;
				return ((p == 4) || (p == 128));
			}
		}

		public static string GetUnparsedCommandLine()
		{
			var args = new Stack<string>((Environment.GetCommandLineArgs() ?? new string[]{ }).Reverse());
			string commandLine = Environment.CommandLine;
			string exeName = args.Peek();

			// If we are not being run under mono-service, just return.
			// NOTE: mono-service.exe passes itself as first arg.
			if (exeName == null || !exeName.EndsWith("mono-service.exe"))
			{
				return commandLine;
			}

			// strip mono-service.exe + arguments from cmdline.
			commandLine = commandLine.Substring(exeName.Length).TrimStart();
			do
			{
				args.Pop();
			} while (args.Count > 0 && args.Peek().StartsWith("-"));
			exeName = args.Peek();

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
