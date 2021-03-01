// ----------------------------------------------------------------------------
// Copyright (c) 2016-2021 Mikhail Korchun <korchoon@gmail.com>
// ----------------------------------------------------------------------------

using System;

namespace Mk.Scopes {
    public static class Scope {
        public static IDisposable New(out IScope scope) {
            scope = new ScopeStack(out var disposable);
            return disposable;
        }

        public static IDisposable ToDisposable<T>(this T target, Action<T> action) {
            return new Disposable<T>(action, target);
        }

        public static void Add<T>(this IScope scope, Action<T> dispose, T target) {
            scope.Add(new Disposable<T>(dispose, target));
        }
    }
}