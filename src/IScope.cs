// ----------------------------------------------------------------------------
// Copyright (c) 2016-2021 Mikhail Korchun <korchoon@gmail.com>
// ----------------------------------------------------------------------------

using System;

namespace Mk.Scopes {
    public interface IScope {
        bool IsDisposed { get; }
        void Add(IDisposable disposable);
        void Remove(IDisposable disposable);
        IDisposable SubScope(out IScope scope);
    }
}