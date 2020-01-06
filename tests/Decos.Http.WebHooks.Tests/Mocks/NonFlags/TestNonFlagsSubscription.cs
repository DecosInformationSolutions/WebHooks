using System;

namespace Decos.Http.WebHooks.Tests.Mocks.NonFlags
{
    public class TestNonFlagsSubscription : WebHookSubscription<TestNonFlagsAction>
    {
        public TestNonFlagsSubscription(string clientId, Uri callbackUri,
            TestNonFlagsAction subscribedActions)
            : base(clientId, callbackUri, subscribedActions)
        {
        }

        public TestNonFlagsSubscription(string clientId, string callbackUri,
            TestNonFlagsAction subscribedActions)
            : base(clientId, new Uri(callbackUri), subscribedActions)
        {
        }

        public override string ToString() => $"{CallbackUri}: {SubscribedActions}";
    }
}