using System;
using System.Collections;
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
        [Theory]
        [InlineData(1)]
        [InlineData(50)]
        public async Task AllSubscriptionsShouldBeInvoked(int count)
        {
            var subscriptions = Enumerable.Range(1, count)
                .Select(RegularSubscription)
                .ToList();
            var handler = new Always200OKHttpHandler();
            var serviceProvider = ConfigureServices(handler, subscriptions);
            var caller = serviceProvider.GetRequiredService<TestCaller>();

            await caller.InvokeSubscriptionsAsync(TestActions.Action1, new { }, default);

            await FinishBackgroundTasksAsync(serviceProvider);
            handler.InvokedUris.Should().BeEquivalentTo(subscriptions.Select(x => x.CallbackUri));
        }

        private IServiceProvider ConfigureServices(
            HttpMessageHandler httpMessageHandler,
            IEnumerable<TestWebHookSubscription> subscriptions)
        {
            var testStore = new TestStore(new List<TestWebHookSubscription>(subscriptions));
            var httpClient = new HttpClient(httpMessageHandler);
            return new ServiceCollection()
                .AddBackgroundTasks()
                .AddSingleton<ITestStore>(testStore)
                .AddSingleton(httpClient)
                .AddTransient<TestCaller>()
                .BuildServiceProvider();
        }

        private TestWebHookSubscription RegularSubscription(int i)
            => new TestWebHookSubscription($"{i}", $"http://localhost/{i}", TestActions.Action1 | TestActions.Action2);

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