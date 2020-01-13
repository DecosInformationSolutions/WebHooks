using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Decos.Http.WebHooks.EfCore;
using Decos.Http.WebHooks.Tests.Mocks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Decos.Http.WebHooks.Tests
{
    using DbStore = DbContextWebHookStore<TestContext, TestWebHookSubscription, TestActions>;

    public class DbContextWebHookStoreTests
    {
        private Guid _uniqueId;

        public DbContextWebHookStoreTests()
        {
            _uniqueId = Guid.NewGuid();
        }

        [Fact]
        public async Task StoreReturnsRequestedAmountOfItems()
        {
            const int ItemsInDb = 50;
            const int RequestedItems = 50;
            using (Arrange(out var services, GenerateSubscriptions().Take(ItemsInDb)))
            {
                var store = services.GetRequiredService<DbStore>();
                var result = await store.GetSubscriptionsAsync(RequestedItems, 0, default);
                result.Should().HaveCount(RequestedItems);
            }
        }

        [Fact]
        public async Task StoreReturnsMaximumAmountOfItemsLeft()
        {
            const int ItemsInDb = 50;
            const int RequestedItems = 60;
            using (Arrange(out var services, GenerateSubscriptions().Take(ItemsInDb)))
            {
                var store = services.GetRequiredService<DbStore>();
                var result = await store.GetSubscriptionsAsync(RequestedItems, 0, default);
                result.Should().HaveCount(ItemsInDb);
            }
        }

        [Fact]
        public async Task StoreReturnsOnlyMatchingActions()
        {
            using (Arrange(out var services,
                new TestWebHookSubscription("1", "http://localhost/1", TestActions.Action1),
                new TestWebHookSubscription("2", "http://localhost/2", TestActions.Action2),
                new TestWebHookSubscription("3", "http://localhost/3", TestActions.Action3)))
            {
                var store = services.GetRequiredService<DbStore>();
                var result = await store.GetSubscriptionsAsync(TestActions.Action1, 2, 0, default);
                result.Should().ContainSingle(x => x.ClientId == "1");
            }
        }

        [Fact]
        public async Task StoreReturnsOnlyMatchingActionsWithFlags()
        {
            using (Arrange(out var services,
                new TestWebHookSubscription("1", "http://localhost/1", TestActions.All),
                new TestWebHookSubscription("2", "http://localhost/2", TestActions.Action2),
                new TestWebHookSubscription("3", "http://localhost/3", TestActions.Action3)))
            {
                var store = services.GetRequiredService<DbStore>();
                var result = await store.GetSubscriptionsAsync(TestActions.Action1, 2, 0, default);
                result.Should().ContainSingle(x => x.ClientId == "1");
            }
        }

        [Fact]
        public async Task StoreAddsItemIfItDoesNotExistYet()
        {
            using (Arrange(out var services,
                new TestWebHookSubscription("1", "http://localhost/1", TestActions.Action1),
                new TestWebHookSubscription("2", "http://localhost/2", TestActions.Action2),
                new TestWebHookSubscription("3", "http://localhost/3", TestActions.Action3)))
            {
                var store = services.GetRequiredService<DbStore>();
                await store.SubscribeAsync(new TestWebHookSubscription(
                    "4", "http://localhost/4", TestActions.All
                ), default);

                var context = services.GetRequiredService<TestContext>();
                var result = await context.Subscriptions.SingleOrDefaultAsync(x => x.ClientId == "4");
                result.CallbackUri.Should().Be("http://localhost/4");
                result.SubscribedActions.Should().Be(TestActions.All);
            }
        }

        [Fact]
        public async Task StoreUpdatesItemIfItExists()
        {
            var guid = Guid.NewGuid();
            using (Arrange(out var services,
                new TestWebHookSubscription("1", "http://localhost/1", TestActions.Action1) { Id = guid },
                new TestWebHookSubscription("2", "http://localhost/2", TestActions.Action2),
                new TestWebHookSubscription("3", "http://localhost/3", TestActions.Action3)))
            {
                var store = services.GetRequiredService<DbStore>();
                await store.SubscribeAsync(new TestWebHookSubscription(
                    "1", "http://localhost/123", TestActions.All
                ) { Id = guid }, default);

                var context = services.GetRequiredService<TestContext>();
                var result = await context.Subscriptions.SingleOrDefaultAsync(x => x.ClientId == "1");
                result.CallbackUri.Should().Be("http://localhost/123");
                result.SubscribedActions.Should().Be(TestActions.All);
            }
        }

        [Fact]
        public async Task StoreRemovesItemIfItExists()
        {
            var guid = Guid.NewGuid();
            using (Arrange(out var services,
                new TestWebHookSubscription("1", "http://localhost/1", TestActions.Action1) { Id = guid },
                new TestWebHookSubscription("2", "http://localhost/2", TestActions.Action2),
                new TestWebHookSubscription("3", "http://localhost/3", TestActions.Action3)))
            {
                var store = services.GetRequiredService<DbStore>();
                await store.UnsubscribeAsync(new TestWebHookSubscription(
                    "1", "http://localhost/1", TestActions.Action1
                )
                { Id = guid }, default);

                var context = services.GetRequiredService<TestContext>();
                var result = await context.Subscriptions.SingleOrDefaultAsync(x => x.ClientId == "1");
                result.Should().BeNull();
            }
        }

        [Fact]
        public async Task StoreReturnsFalseWhenRemovingNonexistentItem()
        {
            var guid = Guid.NewGuid();
            using (Arrange(out var services,
                new TestWebHookSubscription("1", "http://localhost/1", TestActions.Action1),
                new TestWebHookSubscription("2", "http://localhost/2", TestActions.Action2),
                new TestWebHookSubscription("3", "http://localhost/3", TestActions.Action3)))
            {
                var store = services.GetRequiredService<DbStore>();
                var result = await store.UnsubscribeAsync(new TestWebHookSubscription(
                    "4", "http://localhost/4", TestActions.All
                )
                { Id = guid }, default);

                result.Should().BeFalse();
            }
        }

        [Fact]
        public async Task StoreUpdatesLastSuccessDate()
        {
            var guid = Guid.NewGuid();
            using (Arrange(out var services,
                new TestWebHookSubscription("1", "http://localhost/1", TestActions.Action1) { Id = guid }))
            {
                var store = services.GetRequiredService<DbStore>();
                await store.MarkSuccessfulAsync(new TestWebHookSubscription(
                    "1", "http://localhost/1", TestActions.Action1)
                { Id = guid }, default);


                var context = services.GetRequiredService<TestContext>();
                var result = await context.Subscriptions.SingleOrDefaultAsync();
                result.LastSuccess.Should().NotBeNull();
            }
        }

        private IEnumerable<TestWebHookSubscription> GenerateSubscriptions()
        {
            for (int i = 0; ; i++)
            {
                yield return new TestWebHookSubscription($"{i}", $"http://localhost/{i}", TestActions.All);
            }
        }

        private IDisposable Arrange(out IServiceProvider services,
            params TestWebHookSubscription[] data)
            => Arrange(out services, data.AsEnumerable());

        private IDisposable Arrange(out IServiceProvider services,
            IEnumerable<TestWebHookSubscription> data)
        {
            var serviceProvider = ConfigureServices();

            var context = serviceProvider.GetRequiredService<TestContext>();
            context.Subscriptions.AddRange(data);
            context.SaveChanges();

            var serviceScope = serviceProvider.CreateScope();
            services = serviceScope.ServiceProvider;

            return new Disposable(() =>
            {
                serviceScope.Dispose();

                using var context = serviceProvider.GetRequiredService<TestContext>();
                context.Database.EnsureDeleted();
            });
        }

        private IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();
            services.AddDbContext<TestContext>(options =>
            {
                options.UseInMemoryDatabase(_uniqueId.ToString());
            });
            services.AddSingleton<DbStore>();
            return services.BuildServiceProvider();
        }
    }
}