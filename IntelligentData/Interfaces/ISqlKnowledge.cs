using System;
using System.Data;

namespace IntelligentData.Interfaces
{
    /// <summary>
    /// An interface that supplies knowledge about the SQL language.
    /// </summary>
    public interface ISqlKnowledge
    {
        /// <summary>
        /// The name of the engine this knowledge applies to.
        /// </summary>
        string EngineName { get; }
        
        /// <summary>
        /// The character used to start quoting an object name.
        /// </summary>
        string ObjectOpenQuote { get; }
        
        /// <summary>
        /// The character used to stop quoting an object name.
        /// </summary>
        string ObjectCloseQuote { get; }
        
        /// <summary>
        /// Optional function used to escape object names.
        /// </summary>
        /// <remarks>
        /// The default behavior is to double up closing quotes within the object name.
        /// This function should not wrap the object name in quotes.
        /// </remarks>
        Func<string,string> EscapeObjectName { get; }

        /// <summary>
        /// Code to insert before a string concatenation.
        /// </summary>
        /// <remarks>
        /// For languages that use a function, this would likely be the function name and opening parenthesis (eg - "CONCAT(").
        /// For languages that use an operator, this would be blank.
        /// </remarks>
        string ConcatStringBefore { get; }
        
        /// <summary>
        /// Code to insert between values in a string concatenation.
        /// </summary>
        /// <remarks>
        /// For languages that use a function, this would likely be a comma.
        /// For languages that use an operator, this would be the operator.
        /// </remarks>
        string ConcatStringMid { get; }
        
        /// <summary>
        /// Code to insert after a string concatenation.
        /// </summary>
        /// <remarks>
        /// For languages that use a function, this would likely be the closing parenthesis.
        /// For languages that use an operator, this would be blank.
        /// </remarks>
        string ConcatStringAfter { get; }
        
        /// <summary>
        /// The command text used to retrieve the ID from the last insert operation.
        /// </summary>
        string GetLastInsertedIdCommand { get; }
        
        /// <summary>
        /// Does the language support table aliases in DELETE statements.
        /// </summary>
        bool DeleteSupportsTableAliases { get; }
        
        /// <summary>
        /// Does the language support table aliases in UPDATE statements.
        /// </summary>
        bool UpdateSupportsTableAliases { get; }
        
        /// <summary>
        /// Does the language support FROM clauses in UPDATE statements.
        /// </summary>
        bool UpdateSupportsFromClause { get; }

        /// <summary>
        /// Is this knowledge relevant for the supplied connection?
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        bool RelevantForConnection(IDbConnection connection);
    }
}
