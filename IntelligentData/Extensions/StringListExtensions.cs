using System.Collections.Generic;
using System.Linq;

namespace IntelligentData.Extensions
{
    internal static class StringListExtensions
    {
        public static string JoinOr(this IEnumerable<string> self)
        {
            var list = self.ToArray();
            if (list.Length == 0) return string.Empty;
            if (list.Length == 1) return list[1];
            if (list.Length == 2) return $"{list[0]} or {list[1]}";
            return string.Join(", ", list[..^1]) + ", or " + list[^1];
        }
    
        public static string JoinAnd(this IEnumerable<string> self)
        {
            var list = self.ToArray();
            if (list.Length == 0) return string.Empty;
            if (list.Length == 1) return list[1];
            if (list.Length == 2) return $"{list[0]} and {list[1]}";
            return string.Join(", ", list[..^1]) + ", and " + list[^1];
        }

    }
}
