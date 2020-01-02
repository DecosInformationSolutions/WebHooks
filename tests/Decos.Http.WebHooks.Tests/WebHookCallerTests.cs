using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
        public async Task SubscriptionsAreInvoked(int count)
        {
            var subscriptions = Enumerable.Range(1, count)
                .Select(RegularSubscription)
                .ToList();
            var handler = new FixedStatusCodeHttpHandler(HttpStatusCode.OK);
            var serviceProvider = ConfigureServices(handler, subscriptions);
            var caller = serviceProvider.GetRequiredService<TestCaller>();

            await caller.InvokeSubscriptionsAsync(TestActions.Action1, new { }, default);

            await FinishBackgroundTasksAsync(serviceProvider);
            handler.InvokedUris.Should().BeEquivalentTo(subscriptions.Select(x => x.CallbackUri));
        }

        [Fact]
        public async Task OnlyMatchingSubscriptionsAreInvoked()
        {
            var subscriptions = Enumerable.Range(1, 20)
                .Select(MixedActionSubscription)
                .ToList();
            var handler = new FixedStatusCodeHttpHandler(HttpStatusCode.OK);
            var serviceProvider = ConfigureServices(handler, subscriptions);
            var caller = serviceProvider.GetRequiredService<TestCaller>();

            await caller.InvokeSubscriptionsAsync(TestActions.Action1, new { }, default);

            await FinishBackgroundTasksAsync(serviceProvider);
            handler.InvokedUris.Should().BeEquivalentTo(
                subscriptions
                    .Where(x => x.SubscribedActions == TestActions.Action1)
                    .Select(x => x.CallbackUri));
        }

        [Fact]
        public async Task SuccessfulInvocationsUpdateSubscriptions()
        {
            var subscriptions = Enumerable.Range(1, 5)
                .Select(RegularSubscription)
                .ToList();
            var handler = new FixedStatusCodeHttpHandler(HttpStatusCode.OK);
            var serviceProvider = ConfigureServices(handler, subscriptions);
            var caller = serviceProvider.GetRequiredService<TestCaller>();

            await caller.InvokeSubscriptionsAsync(TestActions.Action1, new { }, default);

            subscriptions.Should().OnlyContain(x => x.LastSuccess == null);
            await FinishBackgroundTasksAsync(serviceProvider);
            subscriptions.Should().OnlyContain(x => x.LastSuccess != null);
        }

        [Fact]
        public async Task UnsuccessfulInvocationsDoNotUpdateSubscriptions()
        {
            var subscriptions = Enumerable.Range(1, 5)
                .Select(RegularSubscription)
                .ToList();
            var handler = new FixedStatusCodeHttpHandler(HttpStatusCode.NotFound);
            var serviceProvider = ConfigureServices(handler, subscriptions);
            var caller = serviceProvider.GetRequiredService<TestCaller>();

            await caller.InvokeSubscriptionsAsync(TestActions.Action1, new { }, default);

            await FinishBackgroundTasksAsync(serviceProvider);
            subscriptions.Should().OnlyContain(x => x.LastSuccess == null);
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

        private TestWebHookSubscription MixedActionSubscription(int i)
            => new TestWebHookSubscription($"{i}", $"http://localhost/{i}",
                (i % 2 == 0) ? TestActions.Action1 : TestActions.Action2);

        private async Task FinishBackgroundTasksAsync(IServiceProvider serviceProvider)
        {
            const int DequeueTimeout = 10;

            try
            {
                var queue = serviceProvider.GetRequiredService<IBackgroundTaskQueue>();
                var ct = GetCancellationToken(DequeueTimeout);
                var order = await queue.DequeueAsync(ct);
                while (!ct.IsCancellationRequested)
                {
                    await ((BackgroundWorkItem.WorkOrder)order).Method(CancellationToken.None).ConfigureAwait(false);

                    ct = GetCancellationToken(DequeueTimeout);
                    order = await queue.DequeueAsync(ct);
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