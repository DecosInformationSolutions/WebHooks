using System;

namespace Decos.Http.WebHooks
{
    /// <summary>
    /// Specifies how failed web hook invocations are retried.
    /// </summary>
    public enum RetryPolicy
    {
        /// <summary>
        /// Indicates failed requests are not retried.
        /// </summary>
        None,

        /// <summary>
        /// Indicates failed requests are retried immediately.
        /// </summary>
        Immediate,

        /// <summary>
        /// Indicates failed requests are retried after a fixed amont of time.
        /// </summary>
        Fixed,

        /// <summary>
        /// Indicates failed requests are retried after a linearly increasing time.
        /// </summary>
        Linear,

        /// <summary>
        /// Indicates failed requests are retried after an exponentially increasing time.
        /// </summary>
        Exponential
    }
}