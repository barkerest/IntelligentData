using System;

namespace IntelligentData.Interfaces
{
    /// <summary>
    /// A common entity with created/modified date/time tracking.
    /// </summary>
    public interface ITrackedEntity
    {
        /// <summary>
        /// The date/time the entity was created.
        /// </summary>
        DateTime CreatedAt { get; set; }
        
        /// <summary>
        /// The date/time the entity was last modified.
        /// </summary>
        DateTime LastModifiedAt { get; set; }
    }

    /// <summary>
    /// A common entity with created/modified date/time and user tracking.
    /// </summary>
    public interface ITrackedEntityWithUserName : ITrackedEntity
    {
        /// <summary>
        /// The name of the user who created the entity.
        /// </summary>
        string CreatedBy { get; set; }
        
        /// <summary>
        /// The name of the user who last modified the entity.
        /// </summary>
        string LastModifiedBy { get; set; }
    }

    /// <summary>
    /// A common entity with created/modified date/time and user tracking.
    /// </summary>
    public interface ITrackedEntityWithUserID<TUserId> : ITrackedEntity
    {
        /// <summary>
        /// The ID of the user who created the entity.
        /// </summary>
        TUserId CreatedByID { get; set; }
        
        /// <summary>
        /// The ID of the user who last modified the entity.
        /// </summary>
        TUserId LastModifiedByID { get; set; }
    }
}
