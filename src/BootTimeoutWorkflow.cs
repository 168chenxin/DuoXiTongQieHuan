using System;

namespace DualBootSwitcher
{
    internal enum BootTimeoutChangeResult
    {
        Cancelled,
        Unchanged,
        Applied
    }

    internal sealed class BootTimeoutEditResult
    {
        public BootTimeoutEditResult(BootTimeoutChangeResult result, int requestedSeconds)
        {
            Result = result;
            RequestedSeconds = requestedSeconds;
        }

        public BootTimeoutChangeResult Result { get; private set; }

        public int RequestedSeconds { get; private set; }
    }

    internal sealed class BootTimeoutWorkflow
    {
        private readonly Func<int, int?> requestTimeout;
        private readonly Action<int> applyTimeout;

        public BootTimeoutWorkflow(Func<int, int?> requestTimeout, Action<int> applyTimeout)
        {
            if (requestTimeout == null)
            {
                throw new ArgumentNullException("requestTimeout");
            }

            if (applyTimeout == null)
            {
                throw new ArgumentNullException("applyTimeout");
            }

            this.requestTimeout = requestTimeout;
            this.applyTimeout = applyTimeout;
        }

        public BootTimeoutEditResult Run(int currentSeconds)
        {
            int? requestedSeconds = requestTimeout(currentSeconds);
            if (!requestedSeconds.HasValue)
            {
                return new BootTimeoutEditResult(
                    BootTimeoutChangeResult.Cancelled,
                    currentSeconds);
            }

            if (requestedSeconds.Value == currentSeconds)
            {
                return new BootTimeoutEditResult(
                    BootTimeoutChangeResult.Unchanged,
                    currentSeconds);
            }

            applyTimeout(requestedSeconds.Value);
            return new BootTimeoutEditResult(
                BootTimeoutChangeResult.Applied,
                requestedSeconds.Value);
        }
    }
}
