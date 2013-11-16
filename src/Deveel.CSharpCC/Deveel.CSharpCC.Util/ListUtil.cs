using System;
using System.Collections.Generic;

namespace Deveel.CSharpCC.Util {
    internal static class ListUtil {
        public static void Resize<T>(List<T> list, int sz, T c) {
            int cur = list.Count;
            if (sz < cur)
                list.RemoveRange(sz, cur - sz);
            else if (sz > cur) {
                if (sz > list.Capacity) //this bit is purely an optimisation, to avoid multiple automatic capacity changes.
                    list.Capacity = sz;
                for (int i = 0; i < sz - cur; i++) {
                    list.Add(c);
                }
            }
        }

        public static void Resize<T>(List<T> list, int sz) where T : new() {
            Resize(list, sz, new T());
        }
    }
}