namespace IntelligentData.Errors
{
    public class CannotConvertNonSelectToUpdateException : CannotConvertToUpdateException
    {
        public CannotConvertNonSelectToUpdateException()
            : base("Only SELECT statements can be converted to UPDATE statements.")
        {
        
        }
    }
}
