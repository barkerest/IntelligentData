using System.Data;

namespace IntelligentData.Errors
{
    public class PersistentConnectionBrokenException : DataException, IIntelligentDataException
    {
        public PersistentConnectionBrokenException()
            : base("The persistent connection has been broken.")
        {
        
        }
    }
}
