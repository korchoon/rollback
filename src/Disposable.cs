// ----------------------------------------------------------------------------
// Copyright (c) 2016-2021 Mikhail Korchun <korchoon@gmail.com>
// ----------------------------------------------------------------------------

using System;

namespace Mk.Scopes {
    public class Disposable<T> : IDisposable {
        readonly T _target;
        readonly Action<T> _dispose;

        public Disposable(Action<T> dispose, T target) {
            _target = target;
            _dispose = dispose;
        }

        public void Dispose() {
            _dispose.Invoke(_target);
        }
    }
}