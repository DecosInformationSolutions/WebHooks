using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Decos.Http.WebHooks
{
    /// <summary>
    /// Defines methods for managing web hook subscriptions.
    /// </summary>
    /// <typeparam name="TActions">
    /// The type of enum that specifies the events that can be subscribed to.
    /// </typeparam>
    public interface IWebHookStore<TActions> where TActions : Enum
    {
        /// <summary>
        /// Adds or replaces a web hook subscription.
        /// </summary>
        /// <param name="subscription">
        /// The subscription to add or replace.
        /// </param>
        /// <param name="cancellationToken">
        /// A token to monitor for cancellation requests.
        /// </param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <remarks>
        /// If an ID is specified and a subscription with that ID already
        /// exists, it will be overwritten. Otherwise, a new subscription is
        /// added.
        /// </remarks>
        Task SubscribeAsync(WebHookSubscription<TActions> subscription, CancellationToken cancellationToken);

        /// <summary>
        /// Removes a web hook subscription.
        /// </summary>
        /// <param name="subscription">The subscription to remove.</param>
        /// <param name="cancellationToken">
        /// A token to monitor for cancellation requests.
        /// </param>
        /// <returns>
        /// A task that returns <c>true</c> if the subscription was removed and
        /// <c>false</c> if the subscription does not exist.
        /// </returns>
        Task<bool> UnsubscribeAsync(WebHookSubscription<TActions> subscription, CancellationToken cancellationToken);

        /// <summary>
        /// Returns a collection of all web hook subscriptions.
        /// </summary>
        /// <param name="size">
        /// The number of subscriptions to return at a time.
        /// </param>
        /// <param name="offset">
        /// The number of subscriptions to skip before returning subscriptions.
        /// </param>
        /// <param name="cancellationToken">
        /// A token to monitor for cancellation requests.
        /// </param>
        /// <returns>A part of a collection of web hook subscriptions.</returns>
        Task<IReadOnlyCollection<WebHookSubscription<TActions>>> GetSubscriptionsAsync(int size, int offset, CancellationToken cancellationToken);

        /// <summary>
        /// Returns a collection of web hook subscriptions that are subscribed
        /// to the specified action.
        /// </summary>
        /// <param name="action">The action to check.</param>
        /// <param name="size">
        /// The number of subscriptions to return at a time.
        /// </param>
        /// <param name="offset">
        /// The number of subscriptions to skip before returning subscriptions.
        /// </param>
        /// <param name="cancellationToken">
        /// A token to monitor for cancellation requests.
        /// </param>
        /// <returns>
        /// A part of a collection of web hook subscriptions that match
        /// <paramref name="action"/>.
        /// </returns>
        Task<IReadOnlyCollection<WebHookSubscription<TActions>>> GetSubscriptionsAsync(TActions action, int size, int offset, CancellationToken cancellationToken);
    }
}