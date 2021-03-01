// ----------------------------------------------------------------------------
// Copyright (c) 2016-2021 Mikhail Korchun <korchoon@gmail.com>
// ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Mk.Scopes {
    class ScopeStack : IScope {
        public bool IsDisposed { get; private set; }

        LinkedList<IDisposable> _stack;

        public ScopeStack(out IDisposable dispose) {
            IsDisposed = false;
            _stack = new LinkedList<IDisposable>();
            dispose = new Disposable<ScopeStack>(s => s.InnerDispose(), this);
        }

        public void Add(IDisposable disposable) {
            if (IsDisposed) {
                Assert.Fail("Already disposed");
                disposable.Dispose();
                return;
            }

            lock (_stack)
                _stack.AddLast(disposable);
        }

        public void Remove(IDisposable disposable) {
            lock (_stack) {
                if (IsDisposed) {
                    Assert.Fail("Cannot remove during or after disposal");
                    return;
                }

                var any = _stack.Remove(disposable);
                Assert.IsTrue(any, "IDisposable not found. Make sure it's the same which was passed to Add");
            }
        }

        [MustUseReturnValue]
        public IDisposable SubScope(out IScope subScope) {
            lock (_stack) {
                var subDispose = Scope.New(out subScope);
                var unsubscribeSubDispose = new Disposable<(ScopeStack parentScope, IDisposable disposableSub)>(tuple => {
                    if (!tuple.parentScope.IsDisposed)
                        tuple.parentScope.Remove(tuple.disposableSub);
                }, (this, subDispose));
                subScope.Add(unsubscribeSubDispose);

                Add(subDispose);
                return subDispose;
            }
        }

        void InnerDispose() {
            lock (_stack) {
                if (IsDisposed) return;

                IsDisposed = true;
                IDisposable cur = null;
                while (TryPop(ref cur)) {
                    Assert.IsFalse(IsDisposed, "Already disposed");
                    cur.Dispose();
                }

                _stack.Clear();
                _stack = null;
                IsDisposed = true;
            }
        }

        bool TryPop(ref IDisposable value) {
            lock (_stack) {
                if (_stack.Count == 0) return false;

                value = _stack.Last.Value;
                _stack.RemoveLast();

                return true;
            }
        }
    }
}