﻿using System;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using IntelligentData.Errors;
using IntelligentData.Internal;
using Microsoft.EntityFrameworkCore.Storage;

namespace IntelligentData.Extensions
{
    /// <summary>
    /// Extension methods for queryable interfaces.
    /// </summary>
    public static class QueryableExtensions
    {
        /// <summary>
        /// Gets the SQL string from this query.
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static string GetSqlString(this IQueryable query)
        {
            if (query is null) throw new ArgumentNullException(nameof(query));

            return new QueryInfo(query).Command.CommandText; 
        }

        /// <summary>
        /// Gets the relational command from this query.
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public static IRelationalCommand GetCommand(this IQueryable query)
        {
            if (query is null) throw new ArgumentNullException(nameof(query));

            return new QueryInfo(query).Command;
        }

        /// <summary>
        /// Tries to get the SQL string from this query.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool TryGetSqlString(this IQueryable query, out string value)
        {
            value = "";
            
            try
            {
                value = query.GetSqlString();
                return true;
            }
            catch (Exception e) when (e is IIntelligentDataException)
            {
                value = "";
                return false;
            }
        }

        /// <summary>
        /// Tries to get the relational command from this query.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="command"></param>
        /// <returns></returns>
        public static bool TryGetCommand(this IQueryable query, [NotNullWhen(true)]out IRelationalCommand? command)
        {
            command = null;
            
            try
            {
                command = query.GetCommand();
                return true;
            }
            catch (Exception e) when (e is IIntelligentDataException)
            {
                command = null;
                return false;
            }
        }

        /// <summary>
        /// Converts a query into a parameterized sql object.
        /// </summary>
        /// <param name="query"></param>
        /// <typeparam name="TEntity"></typeparam>
        /// <returns></returns>
        public static ParameterizedSql<TEntity> ToParameterizedSql<TEntity>(this IQueryable<TEntity> query)
            => new(query);

        /// <summary>
        /// Deletes the records that would be returned by the query.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="transaction"></param>
        /// <typeparam name="TEntity"></typeparam>
        /// <returns>Returns the number of records deleted.</returns>
        public static int BulkDelete<TEntity>(this IQueryable<TEntity> query, DbTransaction? transaction = null)
            => new ParameterizedSql<TEntity>(query).ToDelete().ExecuteNonQuery(transaction);

        /// <summary>
        /// Updates the records that would be returned by the query.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="newValues">An expression creating a new TEntity with the new values to set.</param>
        /// <param name="transaction"></param>
        /// <typeparam name="TEntity"></typeparam>
        /// <returns>Returns the number of records updated.</returns>
        public static int BulkUpdate<TEntity>(this IQueryable<TEntity> query, Expression<Func<TEntity, TEntity>> newValues, DbTransaction? transaction = null)
            => new ParameterizedSql<TEntity>(query).ToUpdate(newValues).ExecuteNonQuery(transaction);
        
        
    }
}
