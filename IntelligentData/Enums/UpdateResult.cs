namespace IntelligentData.Enums
{
    /// <summary>
    /// The update result for intelligent entity operations.
    /// </summary>
    public enum UpdateResult
    {
        /// <summary>
        /// The update was successful.
        /// </summary>
        Success,
        
        /// <summary>
        /// The update failed because inserting is disabled for the entity in the DB context.
        /// </summary>
        FailedInsertDisallowed,
        
        /// <summary>
        /// The update failed because updating is disabled for the entity in the DB context.
        /// </summary>
        FailedUpdateDisallowed,
        
        /// <summary>
        /// The update failed because deleting is disable for the entity in the DB context.
        /// </summary>
        FailedDeleteDisallowed,
        
        /// <summary>
        /// The update failed because the entity was deleted by another thread.
        /// </summary>
        FailedDeletedByOther,
        
        /// <summary>
        /// The update failed because the entity was updated by another thread.
        /// </summary>
        FailedUpdatedByOther,
        
        /// <summary>
        /// The update failed for an unknown reason.
        /// </summary>
        FailedUnknownReason
    }
}
