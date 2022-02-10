namespace IntelligentData.Errors
{
    public class CannotConvertAggregateToUpdateException : CannotConvertToUpdateException
    {
        public CannotConvertAggregateToUpdateException()
            : base("Cannot convert aggregate SELECT statements to UPDATE statements.")
        {
        
        }
    }
}
