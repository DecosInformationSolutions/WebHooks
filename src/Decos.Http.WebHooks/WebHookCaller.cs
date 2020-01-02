using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Decos.AspNetCore.BackgroundTasks;

namespace Decos.Http.WebHooks
{
    public class WebHookCaller<TSubscription, TActions> : IWebHookCaller<TActions>
        where TSubscription : WebHookSubscription<TActions>
        where TActions : Enum
    {
        private const string JsonMediaType = "application/json";
        private readonly IWebHookStore<TSubscription, TActions> _webHookStore;
        private readonly IBackgroundTaskQueue _backgroundTaskQueue;
        private readonly HttpClient _httpClient;

        public WebHookCaller(IWebHookStore<TSubscription, TActions> webHookStore,
            IBackgroundTaskQueue backgroundTaskQueue,
            HttpClient httpClient)
        {
            _webHookStore = webHookStore;
            _backgroundTaskQueue = backgroundTaskQueue;
            _httpClient = httpClient;
        }

        /// <summary>
        /// Sends a POST request to all web hook subscriptions that are
        /// subscribed to the specified action.
        /// </summary>
        /// <typeparam name="TPayload">
        /// The type of payload to send in the request.
        /// </typeparam>
        /// <param name="action">
        /// The action that is the reason to invoke the web hook.
        /// </param>
        /// <param name="payload">
        /// An object to send as the request body when invoking the web hook.
        /// </param>
        /// <param name="cancellationToken">
        /// A token to monitor for cancellation requests.
        /// </param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task InvokeSubscriptionsAsync<TPayload>(TActions action,
            TPayload payload, CancellationToken cancellationToken)
        {
            const int Size = 50;
            var offset = 0;

            while (!cancellationToken.IsCancellationRequested)
            {
                var subscriptions = await _webHookStore.GetSubscriptionsAsync(
                    action, Size, offset, cancellationToken);
                if (subscriptions.Count == 0)
                    break;

                foreach (var subscription in subscriptions)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    _backgroundTaskQueue.QueueBackgroundWorkItem(async ct
                        => await InvokeSubscriptionAsync(subscription, payload, ct));
                }

                offset += subscriptions.Count;
                if (subscriptions.Count < Size)
                    break;
            }
        }

        protected virtual async Task InvokeSubscriptionAsync<TPayload>(
            TSubscription subscription, TPayload payload,
            CancellationToken cancellationToken)
        {
            var jsonPayload = JsonSerializer.Serialize(payload);
            var jsonContent = new StringContent(jsonPayload, Encoding.UTF8, JsonMediaType);

            var response = await _httpClient.PostAsync(subscription.CallbackUri, jsonContent, cancellationToken);
            if (response.IsSuccessStatusCode)
                await _webHookStore.UpdateSubscriptionAsync(subscription, cancellationToken);
        }
    }
}