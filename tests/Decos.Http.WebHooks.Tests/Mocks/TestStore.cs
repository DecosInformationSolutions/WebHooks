﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Decos.Http.WebHooks.Tests.Mocks
{
    public class TestStore : IWebHookStore<TestWebHookSubscription, TestActions>
    {
        private readonly IList<TestWebHookSubscription> _list;

        public TestStore(IList<TestWebHookSubscription> list)
        {
            _list = list;
        }

        public Task<IReadOnlyCollection<TestWebHookSubscription>> GetSubscriptionsAsync(
            int size, int offset, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<TestWebHookSubscription>>(
                _list.Skip(offset)
                     .Take(size)
                     .ToImmutableHashSet());
        }

        public Task<IReadOnlyCollection<TestWebHookSubscription>> GetSubscriptionsAsync(
            TestActions action, int size, int offset, CancellationToken cancellationToken)
        {
            var results = _list.Where(x => x.SubscribedActions.HasFlag(action))
                     .Skip(offset)
                     .Take(size)
                     .ToImmutableHashSet();
            return Task.FromResult<IReadOnlyCollection<TestWebHookSubscription>>(results);
        }

        public Task SubscribeAsync(TestWebHookSubscription subscription,
            CancellationToken cancellationToken)
            => Task.FromResult(true);

        public Task<bool> UnsubscribeAsync(TestWebHookSubscription subscription,
            CancellationToken cancellationToken)
            => Task.FromResult(true);

        public Task MarkSuccessfulAsync(TestWebHookSubscription subscription,
            CancellationToken cancellationToken)
        {
            subscription.LastSuccess = DateTimeOffset.Now;
            return Task.CompletedTask;
        }
    }
}