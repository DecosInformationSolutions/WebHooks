using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Decos.Http.WebHooks.Tests.Mocks
{
    public class FirstDelayedHttpHandler : HttpMessageHandler
    {
        private readonly List<Uri> _invokedUris = new List<Uri>();

        public FirstDelayedHttpHandler(TimeSpan delay)
        {
            Delay = delay;
        }

        public FirstDelayedHttpHandler(int millisecondsDelay)
            : this(TimeSpan.FromMilliseconds(millisecondsDelay))
        {
        }

        public TimeSpan Delay { get; }

        public IReadOnlyCollection<Uri> InvokedUris => _invokedUris.AsReadOnly();

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (!_invokedUris.Contains(request.RequestUri))
            {
                _invokedUris.Add(request.RequestUri);
                await Task.Delay(Delay, cancellationToken);
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(""),
                RequestMessage = request
            };
        }
    }
}