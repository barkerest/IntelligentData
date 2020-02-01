using System;
using IntelligentData.Enums;
using IntelligentData.Interfaces;

namespace IntelligentData.Attributes
{
    /// <summary>
    /// Sets the access rights for the attached entity.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class AccessAttribute : Attribute, IEntityAccessProvider
    {
        /// <inheritdoc />
        public AccessLevel EntityAccessLevel { get; }

        /// <summary>
        /// Sets the access level for the attached entity.
        /// </summary>
        /// <param name="level"></param>
        public AccessAttribute(AccessLevel level)
        {
            EntityAccessLevel = level;
        }
    }
}
