namespace IntelligentData.Errors
{
    public class CannotConvertNonSelectToDeleteException : CannotConvertToDeleteException
    {
        public CannotConvertNonSelectToDeleteException()
            : base("Only SELECT statements can be converted to DELETE statements.")
        {
        
        }
    }
}
