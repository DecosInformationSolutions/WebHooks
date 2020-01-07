using System;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Decos.AspNetCore.BackgroundTasks;
using Microsoft.Extensions.Options;
using Polly;

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
        private readonly IOptionsMonitor<WebHookCallerOptions>? _options;

        public WebHookCaller(IWebHookStore<TSubscription, TActions> webHookStore,
            IBackgroundTaskQueue backgroundTaskQueue,
            HttpClient httpClient)
            : this(webHookStore, backgroundTaskQueue, httpClient, null)
        {
        }

        public WebHookCaller(IWebHookStore<TSubscription, TActions> webHookStore,
            IBackgroundTaskQueue backgroundTaskQueue,
            HttpClient httpClient,
            IOptionsMonitor<WebHookCallerOptions>? options)
        {
            ValidateEnum<TActions>();
            _webHookStore = webHookStore;
            _backgroundTaskQueue = backgroundTaskQueue;
            _httpClient = httpClient;
            _options = options;
        }

        protected WebHookCallerOptions Options
            => _options?.CurrentValue ?? new WebHookCallerOptions();

        /// <summary>
        /// Sends a POST request to all web hook subscriptions that are subscribed to the specified
        /// action.
        /// </summary>
        /// <typeparam name="TPayload">The type of payload to send in the request.</typeparam>
        /// <param name="action">The action that is the reason to invoke the web hook.</param>
        /// <param name="payload">
        /// An object to send as the request body when invoking the web hook.
        /// </param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
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
                    _backgroundTaskQueue.QueueBackgroundWorkItem(async ct =>
                    {
                        var cts = CancellationTokenSource.CreateLinkedTokenSource(ct, cancellationToken); 
                        await InvokeSubscriptionAsync(subscription, payload, cts.Token).ConfigureAwait(false);
                    });
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
            var jsonContent = new StringContent(jsonPayload, Encoding.UTF8,
                JsonMediaType);

            var response = await Policy.Handle<OperationCanceledException>()
                .OrResult<HttpResponseMessage>(x => !x.IsSuccessStatusCode)
                .WaitAndRetryAsync(Options.MaxRetries, Options.RetryPolicy.GetDelayFunc())
                .ExecuteAsync(async ct =>
                {
                    return await _httpClient.PostAsync(subscription.CallbackUri,
                        jsonContent, ct).ConfigureAwait(false);
                }, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                await _webHookStore.UpdateSubscriptionAsync(subscription,
                    cancellationToken).ConfigureAwait(false);
            }
        }

        private static void ValidateEnum<T>()
        {
            var type = typeof(T);
            if (!type.IsEnum)
                throw new InvalidEnumArgumentException($"The type of {nameof(TActions)} is not an enumeration.");

            var flagsAttribute = type.GetCustomAttribute<FlagsAttribute>();
            if (flagsAttribute == null)
                throw new InvalidEnumArgumentException($"The type of {nameof(TActions)} is not a Flags enumeration.");
        }
    }
}