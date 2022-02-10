namespace IntelligentData.Errors
{
    public class CannotConvertAggregateToDeleteException : CannotConvertToDeleteException
    {
        public CannotConvertAggregateToDeleteException()
            : base("Cannot convert aggregate SELECT statements to DELETE statements.")
        {
        
        }
    }
}
