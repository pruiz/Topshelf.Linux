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

using Topshelf;
using Topshelf.Hosts;
using Topshelf.HostConfigurators;
using Topshelf.Runtime.Windows;

namespace Topshelf.Runtime.Linux
{
	public class LinuxHostEnvironment : HostEnvironment
	{
		private readonly HostConfigurator configurator;

		public LinuxHostEnvironment(HostConfigurator configurator)
		{
			this.configurator = configurator;
		}

		public string CommandLine => RuntimeHelper.GetUnparsedCommandLine();

		public bool IsAdministrator => RuntimeHelper.RunningAsRoot;

		public bool IsRunningAsAService => RuntimeHelper.RunningUnderMonoService || RuntimeHelper.RunningAsSystemdService;

		public bool IsServiceInstalled(string serviceName)
		{
			// This allows (at least) running service from command line as console.
			return RuntimeHelper.RunningUnderMonoService;
		}

		public bool IsServiceStopped(string serviceName)
		{
			throw new NotImplementedException();
		}

		public void StartService(string serviceName, TimeSpan startTimeOut)
		{
			throw new NotImplementedException();
		}

		public void StopService(string serviceName, TimeSpan stopTimeOut)
		{
			throw new NotImplementedException();
		}

		public void InstallService(InstallHostSettings settings, Action<InstallHostSettings> beforeInstall, Action afterInstall, Action beforeRollback, Action afterRollback)
		{
			throw new NotImplementedException();
		}

		public void UninstallService(HostSettings settings, Action beforeUninstall, Action afterUninstall)
		{
			throw new NotImplementedException();
		}

		public bool RunAsAdministrator()
		{
			// TODO: We could try to use sudo, or impersonating (seteuid) root using Mono.Posix.
			throw new NotImplementedException();
		}

		public Host CreateServiceHost(HostSettings settings, ServiceHandle serviceHandle)
		{
			if (RuntimeHelper.RunningUnderMonoService)
			{
				return new WindowsServiceHost(this, settings, serviceHandle, configurator);
			}
			else
			{
				return new PosixRunHost(settings, this, serviceHandle);
			}
		}

		public void SendServiceCommand(string serviceName, int command)
		{
			throw new NotImplementedException();
		}
	}
}