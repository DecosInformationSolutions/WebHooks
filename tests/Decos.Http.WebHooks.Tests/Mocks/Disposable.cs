using System;
using System.Collections.Generic;
using System.Text;

namespace Decos.Http.WebHooks.Tests.Mocks
{
    internal class Disposable : IDisposable
    {
        private readonly Action _dispose;

        public Disposable(Action dispose)
        {
            _dispose = dispose;
        }

        public void Dispose() => _dispose();
    }
}
