// ----------------------------------------------------------------------------
// Copyright (c) 2016-2021 Mikhail Korchun <korchoon@gmail.com>
// ----------------------------------------------------------------------------

using System.Diagnostics;

namespace Mk.Scopes {
    static class Assert {
        [Conditional("MK_ASSERTIONS")]
        public static void IsTrue(bool expression, string message) {
            Debug.Assert(expression, message);
        }

        [Conditional("MK_ASSERTIONS")]
        public static void IsFalse(bool expression, string message) {
            Debug.Assert(!expression, message);
        }

        [Conditional("MK_ASSERTIONS")]
        public static void Fail(string message) {
            Debug.Fail(message);
        }
    }
}