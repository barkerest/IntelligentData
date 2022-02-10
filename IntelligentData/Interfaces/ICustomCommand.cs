using System;
using System.Collections.Generic;
using System.Data;

namespace IntelligentData.Interfaces
{
    /// <summary>
    /// Defines the interface used for custom generated commands.
    /// </summary>
    public interface ICustomCommand
    {
        /// <summary>
        /// Gets the SQL statement for the command.
        /// </summary>
        string SqlStatement { get; }
        
        /// <summary>
        /// Gets the parameter access for the command.
        /// </summary>
        IReadOnlyList<Func<object,object?>> ParameterAccessors { get; }

        /// <summary>
        /// Creates a DB command using the SQL statement and the supplied parameter source.
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="parameterSource"></param>
        /// <returns></returns>
        IDbCommand CreateCommand(IDbConnection connection, object parameterSource);
        
    }
}
