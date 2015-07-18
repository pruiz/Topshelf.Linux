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
using Topshelf.Runtime.Linux;

namespace Topshelf
{
	public static class LinuxConfigurationExtensions
	{
		public static void UseLinuxIfAvailable(this HostConfigurator configurator)
		{
			if (MonoHelper.RunninOnLinux)
			{
				// Needed to overcome mono-service style arguments.
				configurator.ApplyCommandLine(MonoHelper.GetUnparsedCommandLine());
				configurator.UseEnvironmentBuilder((cfg) => new LinuxHostEnvironmentBuilder(cfg));
			}
		}
	}
}
