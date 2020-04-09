using System.Linq;
using IntelligentData.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace IntelligentData.Extensions
{
    public static class TemporaryListExtensions
    {
        internal static IQueryable<ITempListEntry<T>> TempQuery<T>(this DbContext self, ITempListDefinition tempListDefinition)
            => ((IQueryable<ITempListEntry<T>>) tempListDefinition.GetSet(self));
    }
}
