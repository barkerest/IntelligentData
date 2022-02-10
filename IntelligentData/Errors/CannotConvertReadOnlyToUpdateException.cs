namespace IntelligentData.Errors
{
    public class CannotConvertReadOnlyToUpdateException : CannotConvertToUpdateException
    {
        public CannotConvertReadOnlyToUpdateException()
            : base("SQL statements must be against the data model to be converted to UPDATE statements.")
        {
        
        }
    }
}
