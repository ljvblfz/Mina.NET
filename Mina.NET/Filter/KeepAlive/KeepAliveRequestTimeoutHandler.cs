using System;
using Common.Logging;
using Mina.Core.Session;

namespace Mina.Filter.KeepAlive
{
    /// <summary>
    /// Tells <see cref="KeepAliveFilter"/> what to do when a keep-alive response message
    /// was not received within a certain timeout.
    /// </summary>
    public static class KeepAliveRequestTimeoutHandler
    {
        static readonly ILog log = LogManager.GetLogger(typeof(KeepAliveFilter));

        /// <summary>
        /// Do nothing.
        /// </summary>
        public static IKeepAliveRequestTimeoutHandler Noop => NoopHandler.Instance;

        /// <summary>
        /// Logs a warning message, but doesn't do anything else.
        /// </summary>
        public static IKeepAliveRequestTimeoutHandler Log => LogHandler.Instance;

        /// <summary>
        /// Throws a <see cref="KeepAliveRequestTimeoutException"/>.
        /// </summary>
        public static IKeepAliveRequestTimeoutHandler Exception => ExceptionHandler.Instance;

        /// <summary>
        /// Closes the connection after logging.
        /// </summary>
        public static IKeepAliveRequestTimeoutHandler Close => CloseHandler.Instance;

        /// <summary>
        /// A special handler for the 'deaf speaker' mode.
        /// </summary>
        public static IKeepAliveRequestTimeoutHandler DeafSpeaker => DeafSpeakerHandler.Instance;

        class NoopHandler : IKeepAliveRequestTimeoutHandler
        {
            public static readonly NoopHandler Instance = new NoopHandler();

            private NoopHandler()
            {
            }

            public void KeepAliveRequestTimedOut(KeepAliveFilter filter, IOSession session)
            {
                // do nothing
            }
        }

        class LogHandler : IKeepAliveRequestTimeoutHandler
        {
            public static readonly LogHandler Instance = new LogHandler();

            private LogHandler()
            {
            }

            public void KeepAliveRequestTimedOut(KeepAliveFilter filter, IOSession session)
            {
                if (log.IsWarnEnabled)
                {
                    log.WarnFormat("A keep-alive response message was not received within {0} second(s).",
                        filter.RequestTimeout);
                }
            }
        }

        class ExceptionHandler : IKeepAliveRequestTimeoutHandler
        {
            public static readonly ExceptionHandler Instance = new ExceptionHandler();

            private ExceptionHandler()
            {
            }

            public void KeepAliveRequestTimedOut(KeepAliveFilter filter, IOSession session)
            {
                throw new KeepAliveRequestTimeoutException("A keep-alive response message was not received within "
                                                           + filter.RequestTimeout + " second(s).");
            }
        }

        class CloseHandler : IKeepAliveRequestTimeoutHandler
        {
            public static readonly CloseHandler Instance = new CloseHandler();

            private CloseHandler()
            {
            }

            public void KeepAliveRequestTimedOut(KeepAliveFilter filter, IOSession session)
            {
                if (log.IsWarnEnabled)
                {
                    log.WarnFormat(
                        "Closing the session because a keep-alive response message was not received within {0} second(s).",
                        filter.RequestTimeout);
                }
                session.Close(true);
            }
        }

        class DeafSpeakerHandler : IKeepAliveRequestTimeoutHandler
        {
            public static readonly DeafSpeakerHandler Instance = new DeafSpeakerHandler();

            private DeafSpeakerHandler()
            {
            }

            public void KeepAliveRequestTimedOut(KeepAliveFilter filter, IOSession session)
            {
                throw new ApplicationException("Shouldn't be invoked.  Please file a bug report.");
            }
        }
    }
}
