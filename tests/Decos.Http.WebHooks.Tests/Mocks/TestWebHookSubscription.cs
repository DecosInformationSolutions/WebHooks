using System;

namespace Decos.Http.WebHooks.Tests.Mocks
{
    public class TestWebHookSubscription : WebHookSubscription<TestActions>
    {
        public TestWebHookSubscription(string clientId, Uri callbackUri,
            TestActions subscribedActions)
            : base(clientId, callbackUri, subscribedActions)
        {
        }

        public TestWebHookSubscription(string clientId, string callbackUri,
            TestActions subscribedActions)
            : base(clientId, new Uri(callbackUri), subscribedActions)
        {
        }

        public override string ToString() => $"{CallbackUri}: {SubscribedActions}";
    }
}