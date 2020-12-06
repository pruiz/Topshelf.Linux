using System;
using System.IO;
using System.Net.Sockets;
using System.Text;

using Mono.Unix;

namespace Topshelf.Runtime.Linux
{
    public class SystemdNotifier
    {
        #region ServiceStates
        /// <summary>
        /// Describes a service state change.
        /// </summary>
        public struct ServiceState
        {
            private readonly byte[] _data;

            /// <summary>
            /// Service startup is finished.
            /// </summary>
            public static readonly ServiceState Ready = new ServiceState("READY=1");

            /// <summary>
            /// Service is beginning its shutdown.
            /// </summary>
            public static readonly ServiceState Stopping = new ServiceState("STOPPING=1");

            /// <summary>
            /// Update the watchdog timestamp.
            /// </summary>
            public static ServiceState Watchdog => new ServiceState("WATCHDOG=1");

            /// <summary>
            /// Describes the service state.
            /// </summary>
            public static ServiceState Status(string value) => new ServiceState($"STATUS={value}");

            /// <summary>
            /// Describes the service failure (errno-style).
            /// </summary>
            public static ServiceState Errno(int value) => new ServiceState($"ERRNO={value}");

            /// <summary>
            /// Describes the service failure (D-Bus error).
            /// </summary>
            public static ServiceState BusError(string value) => new ServiceState($"BUSERROR={value}");

            /// <summary>
            /// Main process ID (PID) of the service, in case the service manager did not fork off the process itself.
            /// </summary>
            public static ServiceState MainPid(int value) => new ServiceState($"MAINPID={value}");

            /// <summary>
            /// Create custom ServiceState.
            /// </summary>
            public ServiceState(string state)
            {
                _data = Encoding.UTF8.GetBytes(state ?? throw new ArgumentNullException(nameof(state)));
            }

            /// <summary>
            /// String representation of service state.
            /// </summary>
            public override string ToString() => _data == null ? string.Empty : Encoding.UTF8.GetString(_data);

            internal byte[] GetData() => _data;
        }
        #endregion

        private const string NOTIFY_SOCKET = "NOTIFY_SOCKET";

        private readonly string _socketPath;

        public SystemdNotifier() :
            this(GetNotifySocketPath())
        {
        }

        // For testing
        internal SystemdNotifier(string socketPath)
        {
            _socketPath = socketPath;
        }

        /// <inheritdoc />
        public bool IsEnabled => _socketPath != null;

        /// <inheritdoc />
        public void Notify(ServiceState state)
        {
            if (!IsEnabled)
            {
                return;
            }

            using (var socket = new Socket(AddressFamily.Unix, SocketType.Dgram, ProtocolType.Unspecified))
            {
                var endPoint = new UnixEndPoint(_socketPath);
                socket.Connect(endPoint);

                // It's safe to do a non-blocking call here: messages sent here are much
                // smaller than kernel buffers so we won't get blocked.
                socket.Send(state.GetData());
            }
        }

        private static string GetNotifySocketPath()
        {
            string socketPath = Environment.GetEnvironmentVariable(NOTIFY_SOCKET);

            if (string.IsNullOrEmpty(socketPath))
            {
                return null;
            }

            // Support abstract socket paths.
            if (socketPath[0] == '@')
            {
                socketPath = "\0" + socketPath.Substring(1);
            }

            return socketPath;
        }
    }
}
