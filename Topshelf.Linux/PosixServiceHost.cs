using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Topshelf.Logging;

using Mono.Unix;
using Mono.Unix.Native;

namespace Topshelf.Runtime.Linux
{
	public class PosixRunHost :
		Host,
		HostControl
	{
		readonly LogWriter _log = HostLogger.Get<PosixRunHost>();
		readonly HostEnvironment _environment;
		readonly ServiceHandle _serviceHandle;
		readonly HostSettings _settings;
		readonly SystemdNotifier _notifier;
		int _deadThread;

		ManualResetEvent _waiter;
		ManualResetEvent _stopper;
		IEnumerable<UnixSignal> _signals;
		volatile int _cancelling = 0;

		TopshelfExitCode _exitCode;

		public PosixRunHost(HostSettings settings, HostEnvironment environment, ServiceHandle serviceHandle)
		{
			if (settings == null)
				throw new ArgumentNullException(nameof(settings));
			if (environment == null)
				throw new ArgumentNullException(nameof(environment));

			_settings = settings;
			_environment = environment;
			_serviceHandle = serviceHandle;
			_notifier = new SystemdNotifier();
		}

		void StartService()
		{
			_log.InfoFormat("Starting the {0} service", _settings.ServiceName);

			if (true)
			{
				try
				{
					// It is common to run console applications in windowless mode, this prevents
					// the process from crashing when attempting to set the title.
					Console.Title = _settings.DisplayName;
				}
				catch (Exception e) when (e is IOException || e is PlatformNotSupportedException)
				{
					_log.Info("It was not possible to set the console window title. See the inner exception for details.", e);
				}
			}

			_cancelling = 0;
			Console.CancelKeyPress += HandleCancelKeyPress;

			if (!_serviceHandle.Start(this))
				throw new TopshelfException("The service failed to start (return false).");

			_log.InfoFormat("The {0} service is now running, press Control+C to exit.", _settings.ServiceName);

			_notifier.Notify(SystemdNotifier.ServiceState.Ready);
		}

		void StopService()
		{
			try
			{
				_log.InfoFormat("Stopping the {0} service", _settings.ServiceName);

				_notifier.Notify(SystemdNotifier.ServiceState.Stopping);

				if (!_serviceHandle.Stop(this))
					throw new TopshelfException("The service failed to stop (returned false).");
			}
			catch (Exception ex)
			{
				_settings.ExceptionCallback?.Invoke(ex);

				_log.Error("The service did not shut down gracefully", ex);
				_exitCode = TopshelfExitCode.ServiceControlRequestFailed;
			}
			finally
			{
				_stopper.Set();

				_serviceHandle.Dispose();

				_log.InfoFormat("The {0} service has stopped.", _settings.ServiceName);
			}
		}

		void SignalStopAndWait()
		{
			// Try to wait until service stopped gracefully..
			if (!WaitHandle.SignalAndWait(_waiter, _stopper, _settings.StopTimeOut, false))
			{
				_log.Error("Timedout while waiting service to stop gracefully.");
				HostLogger.Shutdown();
			}
		}

		void CatchUnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			_settings.ExceptionCallback?.Invoke((Exception)e.ExceptionObject);

			if (_settings.UnhandledExceptionPolicy == UnhandledExceptionPolicyCode.TakeNoAction)
				return;

			_log.Fatal("The service threw an unhandled exception", (Exception)e.ExceptionObject);

			if (_settings.UnhandledExceptionPolicy == UnhandledExceptionPolicyCode.LogErrorOnly)
				return;

			if (e.IsTerminating)
			{
				// it isn't likely that a TPL thread should land here, but if it does let's no block it
				if (Task.CurrentId.HasValue)
				{
					return;
				}

				// this is evil, but perhaps a good thing to let us clean up properly.
				int deadThreadId = Interlocked.Increment(ref _deadThread);
				Thread.CurrentThread.IsBackground = true;

				// Only set name if thread does not already have one.
				if (Thread.CurrentThread.Name == null)
					Thread.CurrentThread.Name = "Unhandled Exception " + deadThreadId.ToString();

				_exitCode = TopshelfExitCode.AbnormalExit;

				// Try to wait until service stopped gracefully..
				SignalStopAndWait();
			}
		}

		void HandleCancelKeyPress(object sender, ConsoleCancelEventArgs consoleCancelEventArgs)
		{
			var cancelKey = consoleCancelEventArgs.SpecialKey == ConsoleSpecialKey.ControlBreak ? "Break" : "C";

			if (!_settings.CanHandleCtrlBreak)
			{
				if (consoleCancelEventArgs.SpecialKey == ConsoleSpecialKey.ControlBreak)
				{
					_log.Error("Control+Break detected, terminating service (not cleanly, use Control+C to exit cleanly)");
					return;
				}
			}

			consoleCancelEventArgs.Cancel = true;

			if (Interlocked.Increment(ref _cancelling) != 1)
			{
				_log.WarnFormat("Control+{0} detected, while the service is already being stopped, ignoring..", cancelKey);
				return;
			}

			_log.InfoFormat("Control+{0} detected, attempting to stop service.", cancelKey);

			// Try to wait until service stopped gracefully..
			SignalStopAndWait();
		}

		private IEnumerable<UnixSignal> RegisterTerminationSignals()
		{
			var signals = new UnixSignal[] {
				new UnixSignal(Signum.SIGTERM),
				new UnixSignal(Signum.SIGINT),
			};

			new Thread(() =>
			{
				while (true)
				{
					var idx = UnixSignal.WaitAny(signals, -1);

					if (idx > -1)
					{
						_log.InfoFormat("Recived signal {0}, stopping..", signals[idx].Signum);
						signals.ForEach(x => x.Dispose()); //< Avoid repeated handling..

						// Try to wait until service stopped gracefully..
						SignalStopAndWait();

						break;
					}
				}
			})
			{
				Name = "Signal Handler",
				IsBackground = true,
			}
			.Start();

			return signals;
		}

		public TopshelfExitCode Run()
		{
			Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

			AppDomain.CurrentDomain.UnhandledException += CatchUnhandledException;

			bool started = false;
			try
			{
				_log.Debug("Running service using posix primitives..");

				_waiter = new ManualResetEvent(false);
				_stopper = new ManualResetEvent(false);
				_signals = RegisterTerminationSignals();
				_exitCode = TopshelfExitCode.Ok;

				StartService();

				started = true;

				_waiter.WaitOne();
			}
			catch (Exception ex)
			{
				_settings.ExceptionCallback?.Invoke(ex);

				_log.Error("An exception occurred", ex);

				return TopshelfExitCode.AbnormalExit;
			}
			finally
			{
				if (started)
					StopService();

				_signals.ForEach(s => { s.Close(); s.Dispose(); });

				_waiter.Close();
				_waiter.Dispose();

				_stopper.Close();
				_stopper.Dispose();

				HostLogger.Shutdown();
			}

			return _exitCode;
		}

		void HostControl.RequestAdditionalTime(TimeSpan timeRemaining)
		{
			// good for you, maybe we'll use a timer for startup at some point but for debugging
			// it's a pain in the ass
		}

		void HostControl.Stop()
		{
			_log.Info("Service Stop requested, exiting.");
			_waiter.Set();
		}

		void HostControl.Stop(TopshelfExitCode exitCode)
		{
			_log.Info($"Service Stop requested with exit code {exitCode}, exiting.");
			_exitCode = exitCode;
			_waiter.Set();
		}
	}
}
