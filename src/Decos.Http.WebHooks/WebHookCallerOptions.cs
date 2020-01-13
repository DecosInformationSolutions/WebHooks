using System;

namespace Decos.Http.WebHooks
{
    /// <summary>
    /// Represents the configurable options for a <see cref="WebHookCaller{TSubscription,
    /// TActions}"/>.
    /// </summary>
    public class WebHookCallerOptions
    {
        /// <summary>
        /// Gets or sets the maximum number of times a POST request should be retried if it does not
        /// succeed.
        /// </summary>
        public int MaxRetries { get; set; } = 5;

        /// <summary>
        /// Gets or sets a value indicating how retries are handled.
        /// </summary>
        public RetryPolicy RetryPolicy { get; set; } = RetryPolicy.Exponential;
    }
}