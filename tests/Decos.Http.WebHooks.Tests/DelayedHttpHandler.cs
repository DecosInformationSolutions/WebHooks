using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Decos.Http.WebHooks.Tests
{
    internal class DelayedHttpHandler : HttpMessageHandler
    {
        public DelayedHttpHandler(TimeSpan delay)
        {
            Delay = delay;
        }

        public DelayedHttpHandler(int millisecondsDelay)
            : this(TimeSpan.FromMilliseconds(millisecondsDelay))
        {
        }

        public TimeSpan Delay { get; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Delay(Delay, cancellationToken);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(""),
                RequestMessage = request
            };
        }
    }
}