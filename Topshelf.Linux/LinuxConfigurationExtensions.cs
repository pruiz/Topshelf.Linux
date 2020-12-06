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

using Topshelf.HostConfigurators;
using Topshelf.Runtime;
using Topshelf.Runtime.Linux;
using Topshelf.Builders.Linux;

namespace Topshelf
{
	public static class LinuxConfigurationExtensions
	{
		// XXX: We do support both Linux & MacOS, but I keep the name for compatibility.
		public static void UseLinuxIfAvailable(this HostConfigurator configurator)
		{
			// TODO: Create Topshelf.MacOS in case supporting MacOS here becomes too cumbersome.
			if (RuntimeHelper.RunningOnLinux || (RuntimeHelper.RunningOnMacOS))
			{
				// Needed to overcome mono-service style arguments.
				configurator.UseEnvironmentBuilder((cfg) => new LinuxHostEnvironmentBuilder(cfg));
				configurator.UseHostBuilder((env, settings) => new LinuxHostBuilder(env, settings));
				configurator.ApplyCommandLine(RuntimeHelper.GetUnparsedCommandLine());
			}
		}
	}
}