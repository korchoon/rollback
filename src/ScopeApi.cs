// ----------------------------------------------------------------------------
// Copyright (c) 2016-2021 Mikhail Korchun <korchoon@gmail.com>
// ----------------------------------------------------------------------------
using System;

namespace Mk.Scopes {
    public static class ScopeApi {
        public static IDisposable ToDisposable<T>(this T target, Action<T> action) {
            return new Disposable<T>(action, target);
        }

        public static void AddWith<T>(this Scope scope, Action<T> dispose, T target) {
            scope.Add(new Disposable<T>(dispose, target));
        }
    }
}