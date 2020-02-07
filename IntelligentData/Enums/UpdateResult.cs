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
