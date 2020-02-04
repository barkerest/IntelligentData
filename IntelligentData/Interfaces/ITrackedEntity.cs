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
    public interface ITrackedEntityWithInt32UserID : ITrackedEntity
    {
        /// <summary>
        /// The ID of the user who created the entity.
        /// </summary>
        int CreatedByID { get; set; }

        /// <summary>
        /// The ID of the user who last modified the entity.
        /// </summary>
        int LastModifiedByID { get; set; }
    }

    /// <summary>
    /// A common entity with created/modified date/time and user tracking.
    /// </summary>
    public interface ITrackedEntityWithInt64UserID : ITrackedEntity
    {
        /// <summary>
        /// The ID of the user who created the entity.
        /// </summary>
        long CreatedByID { get; set; }

        /// <summary>
        /// The ID of the user who last modified the entity.
        /// </summary>
        long LastModifiedByID { get; set; }
    }

    /// <summary>
    /// A common entity with created/modified date/time and user tracking.
    /// </summary>
    public interface ITrackedEntityWithGuidUserID : ITrackedEntity
    {
        /// <summary>
        /// The ID of the user who created the entity.
        /// </summary>
        Guid CreatedByID { get; set; }

        /// <summary>
        /// The ID of the user who last modified the entity.
        /// </summary>
        Guid LastModifiedByID { get; set; }
    }

    /// <summary>
    /// A common entity with created/modified date/time and user tracking.
    /// </summary>
    public interface ITrackedEntityWithStringUserID : ITrackedEntity
    {
        /// <summary>
        /// The ID of the user who created the entity.
        /// </summary>
        string CreatedByID { get; set; }

        /// <summary>
        /// The ID of the user who last modified the entity.
        /// </summary>
        string LastModifiedByID { get; set; }
    }
}
