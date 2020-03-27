using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
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
        /// Unquotes an object name.
        /// </summary>
        /// <param name="knowledge"></param>
        /// <param name="objectName"></param>
        /// <returns></returns>
        public static string UnquoteObjectName(this ISqlKnowledge knowledge, string objectName)
        {
            var quoteLen = knowledge.ObjectOpenQuote.Length + knowledge.ObjectCloseQuote.Length;
            if (objectName.Length >= quoteLen &&
                objectName.StartsWith(knowledge.ObjectOpenQuote) &&
                objectName.EndsWith(knowledge.ObjectCloseQuote))
            {
                objectName = objectName.Substring(knowledge.ObjectOpenQuote.Length, objectName.Length - quoteLen);
            }

            if (knowledge.UnescapeObjectName != null)
            {
                return knowledge.UnescapeObjectName(objectName);
            }

            var q = Regex.Escape(knowledge.ObjectCloseQuote);
            return Regex.Replace(objectName, $"{q}{q}", knowledge.ObjectCloseQuote);
        }

        /// <summary>
        /// Concatenates two or more string values.
        /// </summary>
        /// <param name="knowledge"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public static string ConcatValues(this ISqlKnowledge knowledge, params string[] values)
        {
            if (knowledge is null) throw new ArgumentNullException(nameof(knowledge));
            if (values is null) throw new ArgumentNullException(nameof(values));
            if (values.Length < 1) throw new ArgumentException("Cannot be empty.", nameof(values));
            if (values.Length == 1) return values[0];

            var ret = new StringBuilder();

            ret.Append(values[0]);
            
            for (var i = 1; i < values.Length; i++)
            {
                if (string.IsNullOrEmpty(knowledge.ConcatStringBefore))
                {
                    ret.Insert(0, '(');
                }
                else
                {
                    ret.Insert(0, knowledge.ConcatStringBefore);
                }
                
                if (knowledge.ConcatStringMid == ",")
                {
                    ret.Append(", ");
                }
                else
                {
                    ret.Append(' ').Append(knowledge.ConcatStringMid).Append(' ');
                }

                ret.Append(values[i]);
                
                if (string.IsNullOrEmpty(knowledge.ConcatStringBefore))
                {
                    ret.Append(')');
                }
                else
                {
                    ret.Append(knowledge.ConcatStringAfter);
                }
            }

            return ret.ToString();
        }
        
        
        
    }
}
