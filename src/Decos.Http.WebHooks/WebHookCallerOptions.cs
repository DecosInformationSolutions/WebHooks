using System;

namespace Decos.Http.WebHooks
{
    public class WebHookCallerOptions
    {
        public int MaxRetries { get; set; } = 5;

        public RetryPolicy RetryPolicy { get; set; } = RetryPolicy.Exponential;
    }
}