using System;

namespace Decos.Http.WebHooks.Tests.Mocks
{
    [Flags]
    public enum TestActions
    {
        None = 0,
        Action1 = 1 << 0,
        Action2 = 1 << 1,
        Action3 = 1 << 2,
        All = Action1 | Action2 | Action3
    }
}