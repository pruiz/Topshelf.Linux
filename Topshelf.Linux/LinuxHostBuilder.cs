using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Topshelf.Logging;
using Topshelf.Runtime;

namespace Topshelf.Builders.Linux
{
	public class LinuxHostBuilder : RunBuilder
	{
		static readonly LogWriter _log = HostLogger.Get<LinuxHostBuilder>();

		public LinuxHostBuilder(HostEnvironment environment, HostSettings settings)
			: base(environment, settings)
		{
		}

		public override Host Build(ServiceBuilder serviceBuilder)
		{
			_log.Debug("Running as linux process.");

			return Environment.CreateServiceHost(Settings, serviceBuilder.Build(Settings));
		}
	}
}
