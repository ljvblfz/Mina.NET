namespace Mina.Core.Session
{
    /// <summary>
    /// The configuration of <see cref="IOSession"/>.
    /// </summary>
    public interface IOSessionConfig
    {
        /// <summary>
        /// Gets or sets the size for the read buffer.
        /// <remarks>
        /// The default value depends on the transport.
        /// For socket transport it is 2048.
        /// For serial transport it is 0, indicating the system's default buffer size.
        /// </remarks>
        /// </summary>
        int ReadBufferSize { get; set; }
        /// <summary>
        /// Gets or sets the interval (seconds) between each throughput calculation.
        /// The default value is 3 seconds.
        /// </summary>
        int ThroughputCalculationInterval { get; set; }
        /// <summary>
        /// Returns the interval (milliseconds) between each throughput calculation.
        /// The default value is 3 seconds.
        /// </summary>
        long ThroughputCalculationIntervalInMillis { get; }
        /// <summary>
        /// Returns idle time for the specified type of idleness in seconds.
        /// </summary>
        int GetIdleTime(IdleStatus status);
        /// <summary>
        /// Returns idle time for the specified type of idleness in milliseconds.
        /// </summary>
        long GetIdleTimeInMillis(IdleStatus status);
        /// <summary>
        /// Sets idle time for the specified type of idleness in seconds.
        /// </summary>
        void SetIdleTime(IdleStatus status, int idleTime);
        /// <summary>
        /// Gets or sets idle time for <see cref="IdleStatus.ReaderIdle"/> in seconds.
        /// </summary>
        int ReaderIdleTime { get; set; }
        /// <summary>
        /// Gets or sets idle time for <see cref="IdleStatus.WriterIdle"/> in seconds.
        /// </summary>
        int WriterIdleTime { get; set; }
        /// <summary>
        /// Gets or sets idle time for <see cref="IdleStatus.BothIdle"/> in seconds.
        /// </summary>
        int BothIdleTime { get; set; }
        /// <summary>
        /// Gets or set write timeout in seconds.
        /// </summary>
        int WriteTimeout { get; set; }
        /// <summary>
        /// Gets write timeout in milliseconds.
        /// </summary>
        long WriteTimeoutInMillis { get; }
        /// <summary>
        /// Sets all configuration properties retrieved from the specified config.
        /// </summary>
        void SetAll(IOSessionConfig config);
    }
}
