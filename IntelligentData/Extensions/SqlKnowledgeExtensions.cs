using System.Collections.Generic;
using System.Text;
using IntelligentData.Interfaces;

namespace IntelligentData.Extensions
{
    /// <summary>
    /// Extensions to the SQL knowledge interface.
    /// </summary>
    public static class SqlKnowledgeExtensions
    {
        /// <summary>
        /// Quotes an object name.
        /// </summary>
        /// <param name="knowledge"></param>
        /// <param name="objectName"></param>
        /// <returns></returns>
        public static string QuoteObjectName(this ISqlKnowledge knowledge, string objectName)
        {
            if (knowledge is null) return null;
            if (string.IsNullOrEmpty(objectName)) return "";
            
            StringBuilder ret;
            if (knowledge.EscapeObjectName != null)
            {
                ret = new StringBuilder(knowledge.EscapeObjectName(objectName));
            }
            else
            {
                ret = new StringBuilder(objectName);
                ret.Replace(
                    knowledge.ObjectCloseQuote,
                    knowledge.ObjectCloseQuote + knowledge.ObjectCloseQuote
                );
            }

            ret.Insert(0, knowledge.ObjectOpenQuote);
            ret.Append(knowledge.ObjectCloseQuote);
            
            return ret.ToString();
        }

        /// <summary>
        /// Concatenates two values.
        /// </summary>
        /// <param name="knowledge"></param>
        /// <param name="value1"></param>
        /// <param name="value2"></param>
        /// <returns></returns>
        public static string ConcatValues(this ISqlKnowledge knowledge, string value1, string value2)
        {
            var ret = new StringBuilder();

            if (string.IsNullOrEmpty(knowledge.ConcatStringBefore))
            {
                ret.Append('(');
            }
            else
            {
                ret.Append(knowledge.ConcatStringBefore);
            }

            ret.Append(value1);
            if (knowledge.ConcatStringMid == ",")
            {
                ret.Append(", ");
            }
            else
            {
                ret.Append(' ').Append(knowledge.ConcatStringMid).Append(' ');
            }

            ret.Append(value2);
            if (string.IsNullOrEmpty(knowledge.ConcatStringBefore))
            {
                ret.Append(')');
            }
            else
            {
                ret.Append(knowledge.ConcatStringAfter);
            }
            
            return ret.ToString();
        }

        
    }
}
