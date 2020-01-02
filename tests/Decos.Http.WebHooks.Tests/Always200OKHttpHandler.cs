using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Decos.Http.WebHooks.Tests
{
    internal class Always200OKHttpHandler : HttpMessageHandler
    {
        private readonly List<Uri> _invokedUris = new List<Uri>();

        public IReadOnlyCollection<Uri> InvokedUris => _invokedUris.AsReadOnly();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _invokedUris.Add(request.RequestUri);

            var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                RequestMessage = request,
                Content = new StringContent("")
            };
            return Task.FromResult(response);
        }
    }
}