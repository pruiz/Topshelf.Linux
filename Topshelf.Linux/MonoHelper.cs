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
			var args = Environment.GetCommandLineArgs();
			string commandLine = Environment.CommandLine;
			string str2 = args.First<string>();

			// mono-service.exe passes itself as first arg.
			if (str2 != null && str2.EndsWith("mono-service.exe"))
			{
				commandLine = commandLine.Substring(str2.Length).TrimStart();
				str2 = args.ElementAt(1);
			}
			if (commandLine == str2)
			{
				return "";
			}
			if (commandLine.Substring(0, str2.Length) == str2)
			{
				return commandLine.Substring(str2.Length);
			}
			string str3 = "\"" + str2 + "\"";
			if (commandLine.Substring(0, str3.Length) == str3)
			{
				return commandLine.Substring(str3.Length);
			}
			return commandLine;
		}
	}
}
