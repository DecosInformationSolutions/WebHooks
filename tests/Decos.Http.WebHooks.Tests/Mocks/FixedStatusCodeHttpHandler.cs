using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Decos.Http.WebHooks.Tests.Mocks
{
    public class FixedStatusCodeHttpHandler : HttpMessageHandler
    {
        private readonly List<Uri> _invokedUris = new List<Uri>();

        public FixedStatusCodeHttpHandler()
            : this(HttpStatusCode.OK)
        {
        }

        public FixedStatusCodeHttpHandler(HttpStatusCode statusCode)
        {
            StatusCode = statusCode;
        }

        public IReadOnlyCollection<Uri> InvokedUris => _invokedUris.AsReadOnly();

        public HttpStatusCode StatusCode { get; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _invokedUris.Add(request.RequestUri);

            var response = new HttpResponseMessage(StatusCode)
            {
                RequestMessage = request,
                Content = new StringContent("")
            };
            return Task.FromResult(response);
        }
    }
}