using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

namespace Decos.Http.WebHooks.EfCore
{
    /// <summary>
    /// Manages web hook subscriptions in an Entity Framework Core database
    /// context.
    /// </summary>
    /// <typeparam name="TContext">The type of DbContext to use.</typeparam>
    /// <typeparam name="TSubscription">
    /// The type of entity to use for web hook subscriptions.
    /// </typeparam>
    /// <typeparam name="TActions">
    /// The type of enum that specifies the events that can be subscribed to.
    /// </typeparam>
    public class DbContextWebHookStore<TContext, TSubscription, TActions> : IWebHookStore<TSubscription, TActions>
        where TContext : DbContext
        where TSubscription : WebHookSubscription<TActions>
        where TActions : Enum
    {
        private readonly TContext _context;

        /// <summary>
        /// Initializes a new instance of the <see
        /// cref="DbContextWebHookStore{TContext, TSubscription, TActions}"/>
        /// class.
        /// </summary>
        /// <param name="context">The context to use.</param>
        public DbContextWebHookStore(TContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Gets the DbSet that contains the subscriptions.
        /// </summary>
        protected DbSet<TSubscription> Subscriptions => _context.Set<TSubscription>();

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
        public async Task<IReadOnlyCollection<TSubscription>> GetSubscriptionsAsync(int size, int offset, CancellationToken cancellationToken)
        {
            return await Subscriptions
                .Skip(offset)
                .Take(size)
                .ToListAsync(cancellationToken).ConfigureAwait(false);
        }

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
        public async Task<IReadOnlyCollection<TSubscription>> GetSubscriptionsAsync(TActions action, int size, int offset, CancellationToken cancellationToken)
        {
            return await Subscriptions
                .Where(x => x.SubscribedActions.HasFlag(action))
                .Skip(offset)
                .Take(size)
                .ToListAsync(cancellationToken).ConfigureAwait(false);
        }

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
        public async Task SubscribeAsync(TSubscription subscription, CancellationToken cancellationToken)
        {
            Subscriptions.Update(subscription);
            await _context.SaveChangesAsync(cancellationToken);
        }

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
        public async Task<bool> UnsubscribeAsync(TSubscription subscription, CancellationToken cancellationToken)
        {
            var s = await Subscriptions.FindAsync(new object[] { subscription.Id }, cancellationToken);
            if (s == null)
                return false;

            Subscriptions.Remove(s);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}