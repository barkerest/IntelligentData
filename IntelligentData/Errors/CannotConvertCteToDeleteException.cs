namespace IntelligentData.Errors
{
    public class CannotConvertCteToDeleteException : CannotConvertToDeleteException
    {
        public CannotConvertCteToDeleteException()
            : base("A SQL statement with a WITH clause cannot be converted to a DELETE statement.")
        {
        
        }
    }
}
