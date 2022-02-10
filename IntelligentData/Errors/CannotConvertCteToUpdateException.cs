namespace IntelligentData.Errors
{
    public class CannotConvertCteToUpdateException : CannotConvertToUpdateException
    {
        public CannotConvertCteToUpdateException()
            : base("A SQL statement with a WITH clause cannot be converted to an UPDATE statement.")
        {
        
        }
    }
}
