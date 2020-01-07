using System;
using System.ComponentModel;
using System.Threading;

namespace Decos.Http.WebHooks
{
    public static class RetryPolicyExtensions
    {
        /// <summary>
        /// Returns a function that determine the amount of time to wait between retries based on
        /// the number of retries.
        /// </summary>
        /// <param name="retryPolicy">The retry policy to use.</param>
        /// <returns>A function that determines the amount of time to wait between retries.</returns>
        public static Func<int, TimeSpan> GetDelayFunc(this RetryPolicy retryPolicy)
        {
            return retryPolicy switch
            {
                RetryPolicy.None => _ => Timeout.InfiniteTimeSpan,
                RetryPolicy.Immediate => _ => TimeSpan.Zero,
                RetryPolicy.Fixed => _ => TimeSpan.FromSeconds(30),
                RetryPolicy.Linear => i => TimeSpan.FromSeconds(i * 30),
                RetryPolicy.Exponential => i => TimeSpan.FromSeconds(Math.Pow(5, i)),
                _ => throw new InvalidEnumArgumentException(nameof(retryPolicy), (int)retryPolicy, typeof(RetryPolicy)),
            };
        }
    }
}