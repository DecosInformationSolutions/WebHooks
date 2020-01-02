using System;

namespace Decos.Http.WebHooks.Tests
{
    [Flags]
    internal enum TestActions
    {
        None = 0,
        Action1 = 1 << 0,
        Action2 = 1 << 1,
        All = Action1 | Action2
    }
}