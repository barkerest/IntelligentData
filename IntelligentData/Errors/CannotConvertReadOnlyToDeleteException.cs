namespace IntelligentData.Errors
{
    public class CannotConvertReadOnlyToDeleteException : CannotConvertToDeleteException
    {
        public CannotConvertReadOnlyToDeleteException()
            : base("SQL statements must be against the data model to be converted to DELETE statements.")
        {
        
        }
    }
}
