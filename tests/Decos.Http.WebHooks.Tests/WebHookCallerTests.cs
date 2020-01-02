using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Decos.AspNetCore.BackgroundTasks;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace Decos.Http.WebHooks.Tests
{
    using ITestStore = IWebHookStore<TestWebHookSubscription, TestActions>;
    using TestCaller = WebHookCaller<TestWebHookSubscription, TestActions>;

    public class WebHookCallerTests
    {
        [Fact]
        public async Task AllSubscriptionsShouldBeInvoked()
        {
            var subscriptions = new List<TestWebHookSubscription>
            {
                new TestWebHookSubscription("1", "http://localhost/1", TestActions.Action1),
                new TestWebHookSubscription("2", "http://localhost/2", TestActions.Action1),
                new TestWebHookSubscription("3", "http://localhost/3", TestActions.Action1),
                new TestWebHookSubscription("4", "http://localhost/4", TestActions.Action1),
                new TestWebHookSubscription("5", "http://localhost/5", TestActions.Action1)
            };
            var testStore = new TestStore(subscriptions);

            var handler = new Always200OKHttpHandler();
            var httpClient = new HttpClient(handler);

            var serviceProvider = new ServiceCollection()
                .AddBackgroundTasks()
                .AddSingleton<ITestStore>(testStore)
                .AddSingleton(httpClient)
                .AddTransient<TestCaller>()
                .BuildServiceProvider();

            var caller = serviceProvider.GetRequiredService<TestCaller>();
            await caller.InvokeSubscriptionsAsync(TestActions.Action1, new { }, CancellationToken.None);

            await FinishBackgroundTasksAsync(serviceProvider);
            handler.InvokedUris.Should().BeEquivalentTo(subscriptions.Select(x => x.CallbackUri));
        }

        private async Task FinishBackgroundTasksAsync(IServiceProvider serviceProvider)
        {
            try
            {
                var queue = serviceProvider.GetRequiredService<IBackgroundTaskQueue>();
                var ct = GetCancellationToken(500);
                var order = await queue.DequeueAsync(ct);
                while (!ct.IsCancellationRequested)
                {
                    await ((BackgroundWorkItem.WorkOrder)order).Method(CancellationToken.None).ConfigureAwait(false);

                    ct = GetCancellationToken(500);
                    order = await queue.DequeueAsync(GetCancellationToken(500));
                }
            }
            catch (OperationCanceledException) { }
        }

        private CancellationToken GetCancellationToken(int timeout)
        {
            var source = new CancellationTokenSource(timeout);
            return source.Token;
        }
    }
}