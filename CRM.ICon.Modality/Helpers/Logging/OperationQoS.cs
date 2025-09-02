namespace CRM.ICon.Modality.Helpers.Logging
{
    using System;
    using System.Diagnostics;

    /// <summary>
    /// Helps to log QOS events.
    /// For now this will be used only to log QOS for worker role methods
    /// </summary>
    public class OperationQoS : IDisposable
    {
        /// <summary>
        /// Stop watch to calculate elapsed time
        /// </summary>
        private Stopwatch stopWatch = null;

        /// <summary>
        /// Log Context
        /// </summary>
        private ISllLogger sllLogger = null;

        /// <summary>
        /// Denotes if object is already disposed or not
        /// </summary>
        private bool disposed = false;

        /// <summary>
        /// EventInformation
        /// </summary>
        private string eventInformation = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="OperationQoS"/> class
        /// </summary>
        /// <param name="name">Operation QOS name</param>
        /// <param name="logContext">Log Context</param>
        public OperationQoS(ISllLogger sllLogger, String eventInformation)
        {
            this.sllLogger = sllLogger;
            this.eventInformation = eventInformation;

            this.stopWatch = new Stopwatch();
            this.stopWatch.Start();
        }

        /// <summary>
        /// Gets or sets a value indicating whether it was success or failure
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Dispose managed objects
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose managed objects
        /// </summary>
        /// <param name="disposing">True or False</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {

                    sllLogger.WriteInformationalTelemetry("OperationQos", "EventInformation: {0}, ElapsedMilliseconds : {1}, isSuccess: {2}", this.eventInformation, this.stopWatch.ElapsedMilliseconds, this.IsSuccess);
                    this.stopWatch.Stop();
                }

                // event loged already
                this.disposed = true;
            }
        }
    }
}