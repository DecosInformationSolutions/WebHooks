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
            using (Arrange(out var services, GetSubscriptions().Take(ItemsInDb)))
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
            using (Arrange(out var services, GetSubscriptions().Take(ItemsInDb)))
            {
                var store = services.GetRequiredService<DbStore>();
                var result = await store.GetSubscriptionsAsync(RequestedItems, 0, default);
                result.Should().HaveCount(ItemsInDb);
            }
        }

        private IEnumerable<TestWebHookSubscription> GetSubscriptions()
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