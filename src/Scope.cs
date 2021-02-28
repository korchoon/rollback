// ----------------------------------------------------------------------------
// Copyright (c) 2016-2021 Mikhail Korchun <korchoon@gmail.com>
// ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Mk.Scopes {
    public class Scope {
        public bool IsDisposed { get; private set; }

        LinkedList<IDisposable> _stack;
        bool _disposing;

        public Scope(out IDisposable dispose) {
            IsDisposed = false;
            _disposing = false;
            _stack = new LinkedList<IDisposable>();
            dispose = new Disposable<Scope>(s => s.InnerDispose(), this);
        }

        public static IDisposable New(out Scope scope) {
            scope = new Scope(out var disposable);
            return disposable;
        }

        public void Add(IDisposable disposable) {
            if (_disposing) {
                Assert.Fail("Already disposed");
                disposable.Dispose();
                return;
            }

            _stack.AddLast(disposable);
        }

        public void Remove(IDisposable disposable) {
            if (_disposing) {
                Assert.Fail("Cannot remove during or after disposal");
                return;
            }

            var any = _stack.Remove(disposable);
            Assert.IsTrue(any, "IDisposable not found. Make sure it's the same which was passed to Add");
        }

        [MustUseReturnValue]
        public IDisposable SubScope(out Scope subScope) {
            var subDispose = New(out subScope);
            var unsubscribeSubDispose = new Disposable<(Scope parentScope, IDisposable disposableSub)>(tuple => {
                if (!tuple.parentScope._disposing)
                    tuple.parentScope.Remove(tuple.disposableSub);
            }, (this, subDispose));
            subScope.Add(unsubscribeSubDispose);

            Add(subDispose);
            return subDispose;
        }

        void InnerDispose() {
            if (_disposing) return;

            _disposing = true;
            IDisposable cur = null;
            while (TryPop(ref cur)) {
                Assert.IsFalse(IsDisposed, "Already disposed");
                cur.Dispose();
            }

            _stack.Clear();
            _stack = null;
            IsDisposed = true;
        }

        bool TryPop(ref IDisposable value) {
            if (_stack.Count == 0) return false;

            value = _stack.Last.Value;
            _stack.RemoveLast();
            return true;
        }
    }
}