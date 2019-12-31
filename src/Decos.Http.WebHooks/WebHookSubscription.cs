using System;

namespace Decos.Http.WebHooks
{
    /// <summary>
    /// Represents a web hook subscription for one or more actions.
    /// </summary>
    /// <typeparam name="TActions">
    /// The type of enum that specifies the events that can be subscribed to.
    /// </typeparam>
    public abstract class WebHookSubscription<TActions> where TActions : Enum
    {
        /// <summary>
        /// Initializes a new instance of the <see
        /// cref="WebHookSubscription{TActions}"/> class.
        /// </summary>
        /// <param name="clientId">
        /// A unique identifier for the application that owns the subscription.
        /// </param>
        /// <param name="callbackUri">
        /// The URI to send a POST request to when the web hook is invoked.
        /// </param>
        /// <param name="subscribedActions">
        /// The actions for which to invoke the subscription.
        /// </param>
        protected WebHookSubscription(string clientId, Uri callbackUri, TActions subscribedActions)
        {
            ClientId = clientId;
            CallbackUri = callbackUri;
            SubscribedActions = subscribedActions;
        }

        /// <summary>
        /// Gets or sets a unique identifier for subscription.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets a unique identifier for the application that owns the
        /// subscription.
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// Gets or sets the URI to send a POST request to when the web hook is
        /// invoked.
        /// </summary>
        public Uri CallbackUri { get; set; }

        /// <summary>
        /// Gets or sets the actions for which to invoke the subscription.
        /// </summary>
        public TActions SubscribedActions { get; set; }

        /// <summary>
        /// Gets or sets the point in time the subscription was created, or
        /// <c>null</c> if the subscription has not been created yet.
        /// </summary>
        public DateTimeOffset? Created { get; set; }

        /// <summary>
        /// Gets or sets the point in time the subscription was last modified,
        /// or <c>null</c> if the subscription has not been modified since its
        /// creation.
        /// </summary>
        public DateTimeOffset? LastModified { get; set; }

        /// <summary>
        /// Gets or sets the point in time the callback for the subscription was
        /// last invoked successfully, or <c>null</c> if the subscription
        /// callback has not been invoked successfully since its creation.
        /// </summary>
        public DateTimeOffset? LastSuccess { get; set; }
    }
}