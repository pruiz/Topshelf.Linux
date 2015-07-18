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
﻿using Topshelf.Builders;
using Topshelf.HostConfigurators;

namespace Topshelf.Runtime.Linux
{
	public class LinuxHostEnvironmentBuilder : EnvironmentBuilder
	{
		private readonly HostConfigurator configurator;

		public LinuxHostEnvironmentBuilder(HostConfigurator configurator)
		{
			this.configurator = configurator;
		}

		public HostEnvironment Build()
		{
			return new LinuxHostEnvironment(this.configurator);
		}
	}
}