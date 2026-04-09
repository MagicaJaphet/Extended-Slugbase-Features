using System.Runtime.CompilerServices;

namespace ExtendedSlugbase.Helpers
{
    public static class CWTHelpers
    {
        // Taken from LudoCrypt
        public abstract class ExtraDataClass<T, C> where T : class where C : class 
        {
            private static readonly ConditionalWeakTable<T, C> weakData = new();

            public static C GetData(T obj) {
                return weakData.GetOrCreateValue(obj);
            }

            public static bool TryGetData(T obj, out C value)
            {
                return weakData.TryGetValue(obj, out value);
            }
        }
    }
}
