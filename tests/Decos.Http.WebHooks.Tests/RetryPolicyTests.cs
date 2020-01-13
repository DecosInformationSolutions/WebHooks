using System;
using System.ComponentModel;
using System.Threading;

using FluentAssertions;

using Xunit;

namespace Decos.Http.WebHooks.Tests
{
    public class RetryPolicyTests
    {
        [Fact]
        public void NoRetryGivesInfiniteDelay()
        {
            var f = RetryPolicy.None.GetDelayFunc();
            f(1).Should().Be(Timeout.InfiniteTimeSpan);
        }

        [Fact]
        public void ImmediateRetryGivesSmallDelay()
        {
            var f = RetryPolicy.Immediate.GetDelayFunc();
            f(1).Should().BeLessThan(TimeSpan.FromMilliseconds(100));
        }

        [Fact]
        public void FixedRetryGivesSameDelayForEveryRetry()
        {
            var f = RetryPolicy.Fixed.GetDelayFunc();
            f(1).Should().Be(f(5));
        }

        [Fact]
        public void LinearRetryGivesConsistentIncreaseInDelay()
        {
            var f = RetryPolicy.Linear.GetDelayFunc();
            var d1 = f(5) - f(4);
            var d2 = f(2) - f(1);
            d1.Should().Be(d2);
        }

        [Fact]
        public void ExponentialRetryDelayIncreases()
        {
            var f = RetryPolicy.Exponential.GetDelayFunc();
            var d1 = f(4) - f(3);
            var d2 = f(3) - f(2);
            var d3 = f(2) - f(1);
            d1.Should().BeGreaterThan(d2);
            d2.Should().BeGreaterThan(d3);
        }

        [Fact]
        public void InvalidRetryRaisesException()
        {
            Action f = () => RetryPolicyExtensions.GetDelayFunc((RetryPolicy)(-1));
            f.Should().Throw<InvalidEnumArgumentException>();
        }
    }
}