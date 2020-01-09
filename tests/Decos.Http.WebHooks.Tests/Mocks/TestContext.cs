using System;
using Microsoft.EntityFrameworkCore;

namespace Decos.Http.WebHooks.Tests.Mocks
{
    public class TestContext : DbContext
    {
        public TestContext(DbContextOptions options)
            : base(options)
        {
        }

        public virtual DbSet<TestWebHookSubscription> Subscriptions { get; set; } = null!;
    }
}