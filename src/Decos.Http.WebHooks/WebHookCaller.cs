using System;
using System.ComponentModel;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Decos.AspNetCore.BackgroundTasks;

using Microsoft.Extensions.Options;

using Polly;

namespace Decos.Http.WebHooks
{
    /// <summary>
    /// Sends POST requests to all matching web hook subscriptions.
    /// </summary>
    /// <typeparam name="TSubscription">
    /// The type of object that represents a subscription.
    /// </typeparam>
    /// <typeparam name="TActions">The type of enum that specifies the actions.</typeparam>
    public class WebHookCaller<TSubscription, TActions> : IWebHookCaller<TActions>
        where TSubscription : WebHookSubscription<TActions>
        where TActions : Enum
    {
        private const string JsonMediaType = "application/json";
        private readonly IWebHookStore<TSubscription, TActions> _webHookStore;
        private readonly IBackgroundTaskQueue _backgroundTaskQueue;
        private readonly HttpClient _httpClient;
        private readonly IOptionsMonitor<WebHookCallerOptions>? _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebHookCaller{TSubscription, TActions}"/>
        /// class with the specified services.
        /// </summary>
        /// <param name="webHookStore">Used to access subscriptions.</param>
        /// <param name="backgroundTaskQueue">Used to schedule background tasks.</param>
        /// <param name="httpClient">Used to send requests.</param>
        public WebHookCaller(IWebHookStore<TSubscription, TActions> webHookStore,
            IBackgroundTaskQueue backgroundTaskQueue,
            HttpClient httpClient)
            : this(webHookStore, backgroundTaskQueue, httpClient, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebHookCaller{TSubscription, TActions}"/>
        /// class with the specified services.
        /// </summary>
        /// <param name="webHookStore">Used to access subscriptions.</param>
        /// <param name="backgroundTaskQueue">Used to schedule background tasks.</param>
        /// <param name="httpClient">Used to send requests.</param>
        /// <param name="options">Options for the caller.</param>
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

        /// <summary>
        /// Gets the configured options.
        /// </summary>
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
                    action, Size, offset, cancellationToken).ConfigureAwait(false);
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

        /// <summary>
        /// Sends a POST request a subscription with the specified payload.
        /// </summary>
        /// <typeparam name="TPayload">The type of object to send as request body.</typeparam>
        /// <param name="subscription">The subscription to invoke.</param>
        /// <param name="payload">An object to send as request body.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
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
                }, cancellationToken).ConfigureAwait(false);

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