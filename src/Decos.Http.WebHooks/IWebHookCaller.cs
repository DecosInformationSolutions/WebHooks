using System;
using System.Threading;
using System.Threading.Tasks;

namespace Decos.Http.WebHooks
{
    /// <summary>
    /// Defines methods for sending POST requests to all web hook subscriptions.
    /// </summary>
    /// <typeparam name="TActions">
    /// The type of enum that specifies the events that can be subscribed to.
    /// </typeparam>
    public interface IWebHookCaller<TActions> where TActions : Enum
    {
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
        Task InvokeSubscriptionsAsync<TPayload>(TActions action, TPayload payload, CancellationToken cancellationToken);
    }
}