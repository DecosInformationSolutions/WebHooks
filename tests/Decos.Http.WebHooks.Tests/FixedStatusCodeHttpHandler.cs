using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Decos.Http.WebHooks.Tests
{
    internal class FixedStatusCodeHttpHandler : HttpMessageHandler
    {
        private readonly List<Uri> _invokedUris = new List<Uri>();
        private readonly HttpStatusCode _statusCode;

        public FixedStatusCodeHttpHandler()
            : this(HttpStatusCode.OK)
        {
        }

        public FixedStatusCodeHttpHandler(HttpStatusCode statusCode)
        {
            _statusCode = statusCode;
        }

        public IReadOnlyCollection<Uri> InvokedUris => _invokedUris.AsReadOnly();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _invokedUris.Add(request.RequestUri);

            var response = new HttpResponseMessage(_statusCode)
            {
                RequestMessage = request,
                Content = new StringContent("")
            };
            return Task.FromResult(response);
        }
    }
}