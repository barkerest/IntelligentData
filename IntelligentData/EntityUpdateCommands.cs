﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using IntelligentData.Errors;
using IntelligentData.Extensions;
using IntelligentData.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging;

namespace IntelligentData
{
    /// <summary>
    /// A basic entity update command set.
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    public class EntityUpdateCommands<TEntity> : IEntityUpdateCommands<TEntity>, IDisposable where TEntity : class
    {
        private class PropertyEqual : IEqualityComparer<IProperty>
        {
            public bool Equals(IProperty? x, IProperty? y)
                => string.Equals(x?.Name, y?.Name, StringComparison.Ordinal);

            public int GetHashCode(IProperty obj)
                => obj.GetHashCode();
        }

        /// <summary>
        /// The DB context used by the commands.
        /// </summary>
        protected readonly DbContext Context;

        /// <summary>
        /// The logger used by the commands.
        /// </summary>
        protected readonly ILogger Logger;

        /// <summary>
        /// The entity type definition.
        /// </summary>
        protected readonly IEntityType EntityType;

        /// <summary>
        /// The named table or view the entity type references.
        /// </summary>
        protected readonly string TableName;
        
        /// <summary>
        /// The store object identifier.
        /// </summary>
        protected readonly StoreObjectIdentifier StoreObjectID;
        
        /// <summary>
        /// The properties defined for the entity type.
        /// </summary>
        protected readonly IProperty[] EntityProperties;

        /// <summary>
        /// The key defined for the entity type.
        /// </summary>
        protected readonly IKey Key;

        /// <summary>
        /// The concurrency tokens defined for the entity type.
        /// </summary>
        protected virtual IEnumerable<IProperty> ConcurrencyTokens
            => EntityProperties.Where(x => x.IsConcurrencyToken);

        /// <summary>
        /// The SQL knowledge used by the commands.
        /// </summary>
        protected virtual ISqlKnowledge Knowledge { get; }

        /// <summary>
        /// Creates the update command set with the supplied context.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="logger"></param>
        public EntityUpdateCommands(DbContext context, ILogger logger)
        {
            Context     = context ?? throw new ArgumentNullException(nameof(context));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var t = typeof(TEntity);

            EntityType = Context.Model.FindEntityType(t)
                         ?? throw new EntityMissingFromModelException(t);

            TableName = EntityType.GetTableName()
                        ?? throw new EntityTypeWithoutTableNameException(EntityType);

            StoreObjectID = EntityType.GetStoreObjectIdentifier()
                            ?? throw new StoreObjectIdentifierNotFoundException(EntityType);

            Key = EntityType.FindPrimaryKey()
                  ?? throw new EntityTypeWithoutPrimaryKeyException(EntityType);

            var stamps = new[] {"timestamp", "rowversion"};
            EntityProperties = EntityType
                               .GetProperties()
                               .Where(x => !stamps.Contains(x.GetColumnType().ToLower()))
                               .ToArray();

            var conn = Context.Database.ProviderName ?? throw new UnnamedDatabaseProviderException();

            Knowledge = SqlKnowledge.For(conn)
                        ?? throw new UnknownSqlProviderException(conn);
        }

        #region IsProperty
        
        // Determines if the member is the model property.
        private bool IsProperty(MemberInfo member, IProperty property)
        {
            if (member is PropertyInfo prop)
            {
                if (property.PropertyInfo is null) return false;
                return property.PropertyInfo.DeclaringType == prop.DeclaringType
                       && property.PropertyInfo.Name == prop.Name;
            }

            if (member is FieldInfo field)
            {
                if (property.FieldInfo is null) return false;
                return property.FieldInfo.DeclaringType == field.DeclaringType
                       && property.FieldInfo.Name == field.Name;
            }

            return false;
        }
        
        #endregion
        
        #region AddParameterTo

        // Adds a parameter to a command with an appropriate data type.
        private DbParameter AddParameterTo(DbCommand cmd, string name, IProperty prop)
        {
            var param = cmd.CreateParameter();
            param.ParameterName = name;

            var type = prop.GetColumnType().ToUpper();

            if (type.Contains('(')) type = type.Split('(')[0];

            switch (type)
            {
                case "BIT":
                    param.DbType = DbType.Boolean;
                    break;
                case "TINYINT":
                    param.DbType = DbType.Byte;
                    break;
                case "SMALLINT":
                    param.DbType = DbType.Int16;
                    break;
                case "INT":
                case "INTEGER":
                    param.DbType = DbType.Int32;
                    break;
                case "BIGINT":
                    param.DbType = DbType.Int64;
                    break;
                case "NUMERIC":
                case "DECIMAL":
                    param.DbType = DbType.Decimal;
                    break;
                case "FLOAT":
                    param.DbType = DbType.Double;
                    break;
                case "REAL":
                    param.DbType = DbType.Single;
                    break;
                case "DATE":
                    param.DbType = DbType.Date;
                    break;
                case "DATETIME":
                    param.DbType = DbType.DateTime;
                    break;
                case "DATETIME2":
                    param.DbType = DbType.DateTime2;
                    break;
                case "TIME":
                    param.DbType = DbType.Time;
                    break;
                case "CHAR":
                case "VARCHAR":
                case "TEXT":
                    param.DbType = DbType.AnsiString;
                    break;
                case "NCHAR":
                case "NVARCHAR":
                case "NTEXT":
                    param.DbType = DbType.String;
                    break;
                case "BINARY":
                case "VARBINARY":
                case "IMAGE":
                case "ROWVERSION":
                    param.DbType = DbType.Binary;
                    break;
                case "UNIQUEIDENTIFIER":
                    param.DbType = DbType.Guid;
                    break;
            }

            cmd.Parameters.Add(param);

            return param;
        }
        
        #endregion

        #region ValidateProperties
        
        /// <summary>
        /// Validates a list of properties and returns the entity properties they correspond with.
        /// </summary>
        /// <param name="properties"></param>
        /// <param name="allowKeyProps"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        protected IProperty[] ValidateProperties(MemberInfo[] properties, bool allowKeyProps)
        {
            if (properties is null) throw new ArgumentNullException(nameof(properties));

            var invalid = properties
                          .Where(x => x.MemberType != MemberTypes.Field && x.MemberType != MemberTypes.Property)
                          .ToArray();

            if (invalid.Any())
            {
                throw new ArgumentException(
                    "Only properties and fields are supported, the following are invalid: " 
                    + invalid.Select(x => x.Name).JoinAnd()
                );
            }

            invalid = properties
                      .Where(x => EntityProperties.All(y => !IsProperty(x, y)))
                      .ToArray();

            if (invalid.Any())
            {
                throw new ArgumentException(
                    "Only entity properties are supported, the following are invalid: "
                    + invalid.Select(x => x.Name).JoinAnd()
                );
            }

            if (!allowKeyProps)
            {
                invalid = properties
                          .Where(x => Key.Properties.Any(y => IsProperty(x, y)))
                          .ToArray();

                if (invalid.Any())
                {
                    throw new ArgumentException(
                        "Key properties cannot be part of the update selection, the following properties are invalid: "
                        + invalid.Select(x => x.Name).JoinAnd()
                    );
                }
            }

            return EntityProperties
                   .Where(x => properties.Any(y => IsProperty(y, x)))
                   .ToArray();
        }
        
        #endregion
        
        #region InsertProperties

        private IProperty[]? _insertProperties;

        /// <summary>
        /// Defines a list of properties required on insert.
        /// </summary>
        protected virtual IEnumerable<IProperty> RequiredInsertProperties { get; } = Array.Empty<IProperty>();

        /// <summary>
        /// Determines if the insert properties have been set.
        /// </summary>
        protected bool HaveInsertProperties => (_insertProperties is not null);

        /// <summary>
        /// Sets the insert properties for this command set if they have not been previously set.
        /// </summary>
        /// <param name="properties"></param>
        /// <exception cref="InvalidOperationException"></exception>
        protected void SetInsertProperties(IEnumerable<IProperty> properties)
        {
            if (HaveInsertProperties)
            {
                throw new InvalidOperationException("Insert properties have already been set.");
            }

            _insertProperties = properties
                                .Union(Key.Properties.Where(x => x.ValueGenerated == ValueGenerated.Never))
                                .Union(RequiredInsertProperties)
                                .Distinct(new PropertyEqual())
                                .ToArray();
        }

        /// <inheritdoc />
        public virtual void SetInsertProperties(params MemberInfo[] properties)
        {
            SetInsertProperties(ValidateProperties(properties, true));
        }

        private IProperty[] GetInsertProperties()
        {
            if (!HaveInsertProperties)
            {
                // default to the update properties adding the key properties if they are not auto-generated.
                SetInsertProperties(GetUpdateProperties());
            }

            return _insertProperties!;
        }
        
        #endregion

        #region UpdateProperties
        
        private IProperty[]? _updateProperties;

        /// <summary>
        /// Determines if the update properties have been set.
        /// </summary>
        protected bool HaveUpdateProperties => (_updateProperties is not null);

        /// <summary>
        /// Defines a list of properties required for update.
        /// </summary>
        protected virtual IEnumerable<IProperty> RequiredUpdateProperties { get; } = Array.Empty<IProperty>();

        /// <summary>
        /// Sets the update properties for this command set if they have not been previously set.
        /// </summary>
        /// <param name="properties"></param>
        /// <exception cref="InvalidOperationException"></exception>
        protected void SetUpdateProperties(IEnumerable<IProperty> properties)
        {
            if (HaveUpdateProperties)
            {
                throw new InvalidOperationException("Update properties have already been set.");
            }

            _updateProperties = properties
                                .Union(RequiredUpdateProperties)
                                .Distinct(new PropertyEqual())
                                .ToArray();
        }

        /// <inheritdoc />
        public virtual void SetUpdateProperties(params MemberInfo[] properties)
        {
            SetUpdateProperties(ValidateProperties(properties, false));
        }

        private IProperty[] GetUpdateProperties()
        {
            if (!HaveUpdateProperties)
            {
                SetUpdateProperties(
                    EntityProperties
                        .Where(
                            x => !x.IsShadowProperty()
                                 && (x.PropertyInfo is not null || x.FieldInfo is not null)
                                 && Key.Properties.All(y => !string.Equals(y.Name, x.Name, StringComparison.Ordinal))
                        )
                        .ToArray()
                );
            }

            return _updateProperties!;
        }
        
        #endregion
        
        #region RemoveProperties
        
        private IProperty[]? _removeProperties;

        /// <summary>
        /// Determines if the remove properties have been set.
        /// </summary>
        protected bool HaveRemoveProperties => (_removeProperties is not null);

        /// <summary>
        /// Defines a list of properties required for removal (hiding instead of deleting).
        /// </summary>
        protected virtual IEnumerable<IProperty> RequiredRemoveProperties { get; } = Array.Empty<IProperty>();
        
        /// <summary>
        /// Sets the remove properties for this command set if they have not been previously set.
        /// </summary>
        /// <param name="properties"></param>
        /// <exception cref="InvalidOperationException"></exception>
        protected void SetRemoveProperties(IEnumerable<IProperty> properties)
        {
            if (HaveRemoveProperties)
            {
                throw new InvalidOperationException("Remove properties have already been set.");
            }

            _removeProperties = properties
                                .Union(RequiredRemoveProperties)
                                .Distinct(new PropertyEqual())
                                .ToArray();
        }

        /// <inheritdoc />
        public virtual void SetRemoveProperties(params MemberInfo[] properties)
        {
            SetRemoveProperties(ValidateProperties(properties, false));
        }

        private IProperty[] GetRemoveProperties()
        {
            if (!HaveRemoveProperties)
            {
                SetRemoveProperties(Array.Empty<IProperty>());
            }

            return _removeProperties!;
        }

        #endregion
        
        #region ExecNonQuery/ExecScalar/ExecCommand
        
        private int ExecNonQuery(IDbCommand cmd)
        {
            try
            {
                var conn = cmd.Transaction?.Connection as DbConnection ?? Context.Database.GetDbConnection();
                var open = conn.State == ConnectionState.Open;
                if (!open)
                {
                    conn.Open();
                }
                try
                {
                    var sw = new Stopwatch();
                    sw.Start();
                    cmd.Connection = conn;
                    var ret = cmd.ExecuteNonQuery();
                    sw.Stop();
                    Logger.LogDebug($"Executed DbCommand ({sw.ElapsedMilliseconds:#,##0}ms).\n{cmd.CommandText}");
                    return ret;
                }
                finally
                {
                    if (!open)
                    {
                        conn.Close();
                    }
                }
            }
            catch
            {
                Logger.LogError($"Failed to execute DbCommand.\n{cmd.CommandText}");
                throw;
            }
        }

        private object? ExecScalar(IDbCommand cmd)
        {
            try
            {
                var conn = cmd.Transaction?.Connection as DbConnection ?? Context.Database.GetDbConnection();
                var open = conn.State == ConnectionState.Open;
                if (!open)
                {
                    conn.Open();
                }

                try
                {
                    var sw = new Stopwatch();
                    sw.Start();
                    cmd.Connection = conn;
                    var ret = cmd.ExecuteScalar();
                    sw.Stop();
                    Logger.LogDebug($"Executed DbCommand ({sw.ElapsedMilliseconds:#,##0}ms).\n{cmd.CommandText}");
                    return ret is DBNull ? null : ret;
                }
                finally
                {
                    if (!open)
                    {
                        conn.Close();
                    }
                }
            }
            catch
            {
                Logger.LogError($"Failed to execute DbCommand.\n{cmd.CommandText}");
                throw;
            }
        }

        private object? ExecuteCommandForValue(TEntity entity, (IDbCommand, IDictionary<string, Func<TEntity, object?>>) cmd)
        {
            foreach (var param in cmd.Item2)
            {
                var paramValue = param.Value(entity) ?? DBNull.Value;
                ((IDataParameter) cmd.Item1.Parameters[param.Key]).Value = paramValue;
            }

            var result = ExecScalar(cmd.Item1);
            Context.Entry(entity).State = EntityState.Detached;
            return result;
        }
        
        private bool ExecuteCommand(TEntity entity, (IDbCommand, IDictionary<string, Func<TEntity, object?>>) cmd)
        {
            foreach (var param in cmd.Item2)
            {
                var paramValue = param.Value(entity) ?? DBNull.Value;
                ((IDataParameter) cmd.Item1.Parameters[param.Key]).Value = paramValue;
            }

            var result = ExecNonQuery(cmd.Item1);
            Context.Entry(entity).State = EntityState.Detached;

            return (result > 0);
        }
        
        #endregion

        #region Insert

        private IDbCommand?                                  _insertCommand;
        private IDictionary<string, Func<TEntity, object?>>? _insertParameters;

        private (IDbCommand,IDictionary<string, Func<TEntity, object?>>) GetInsertCommand(IDbTransaction? transaction)
        {
            if (_insertCommand is not null && _insertParameters is not null)
            {
                _insertCommand.Transaction = transaction;
                return (_insertCommand, _insertParameters);
            }

            var cmd   = (transaction?.Connection as DbConnection ?? Context.Database.GetDbConnection()).CreateCommand();
            var qry   = new StringBuilder();
            var props = GetInsertProperties();
            var first = true;
            var list  = new Dictionary<string, Func<TEntity, object?>>();

            qry.Append("INSERT INTO ").Append(Knowledge.QuoteObjectName(TableName)).Append(" (");

            var autoKeyProps = Key.Properties.Where(x => x.ValueGenerated == ValueGenerated.Never).ToArray();

            foreach (var prop in autoKeyProps)
            {
                var param = AddParameterTo(cmd, $"@p_{list.Count}", prop);
                if (prop.PropertyInfo is not null)
                {
                    list.Add(param.ParameterName, x => prop.PropertyInfo.GetValue(x));
                }
                else if (prop.FieldInfo is not null)
                {
                    list.Add(param.ParameterName, x => prop.FieldInfo.GetValue(x));
                }
                else
                {
                    throw new PropertyWithoutAccessorException(prop);
                }

                if (!first)
                {
                    qry.Append(", ");
                }

                first = false;
                var propName = prop.GetColumnName(StoreObjectID)
                               ?? throw new PropertyWithoutColumnNameException(prop);
                qry.Append(Knowledge.QuoteObjectName(propName));
            }

            foreach (var prop in props)
            {
                if (autoKeyProps.Contains(prop)) continue;

                if (!first)
                {
                    qry.Append(", ");
                }

                first = false;

                var param = AddParameterTo(cmd, $"@p_{list.Count}", prop);
                if (prop.PropertyInfo is not null)
                {
                    list.Add(param.ParameterName, x => prop.PropertyInfo.GetValue(x));
                }
                else if (prop.FieldInfo is not null)
                {
                    list.Add(param.ParameterName, x => prop.FieldInfo.GetValue(x));
                }
                else
                {
                    throw new PropertyWithoutAccessorException(prop);
                }

                var propName = prop.GetColumnName(StoreObjectID)
                               ?? throw new PropertyWithoutColumnNameException(prop);
                
                qry.Append(Knowledge.QuoteObjectName(propName));
            }

            qry.Append(") VALUES (");
            first = true;
            foreach (IDataParameter param in cmd.Parameters)
            {
                if (!first)
                {
                    qry.Append(", ");
                }

                first = false;
                qry.Append(param.ParameterName);
            }

            qry.Append(')');

            cmd.CommandText   = qry.ToString();
            _insertCommand    = cmd;
            _insertParameters = list;

            _insertCommand.Transaction = transaction;
            return (_insertCommand, _insertParameters);
        }
        
        private IDbCommand? _lastInsertIdCommand;

        private IDbCommand GetLastInsertIdCommand(IDbTransaction? transaction)
        {
            if (_lastInsertIdCommand is not null)
            {
                _lastInsertIdCommand.Transaction = transaction;
                return _lastInsertIdCommand;
            }

            _lastInsertIdCommand             = (transaction?.Connection as DbConnection ?? Context.Database.GetDbConnection()).CreateCommand();
            _lastInsertIdCommand.CommandText = Knowledge.GetLastInsertedIdCommand;

            _lastInsertIdCommand.Transaction = transaction;
            return _lastInsertIdCommand;
        }

        private static bool IsBlankId([NotNullWhen(false)]object? val)
            => val switch
            {
                null       => true,
                byte u8    => u8 == 0,
                sbyte i8   => i8 == 0,
                ushort u16 => u16 == 0,
                short i16  => i16 == 0,
                uint u32   => u32 == 0,
                int i32    => i32 == 0,
                ulong u64  => u64 == 0,
                long i64   => i64 == 0,
                string s   => string.IsNullOrWhiteSpace(s),
                Guid guid  => guid == Guid.Empty,
                _          => false
            };
        
        /// <inheritdoc />
        public virtual bool Insert(TEntity entity, IDbTransaction? transaction)
        {
            if (Key.Properties.Count == 1)
            {
                if (!ExecuteCommand(entity, GetInsertCommand(transaction))) return false;

                var prop = Key.Properties[0];
                if (prop.ValueGenerated == ValueGenerated.OnAdd &&
                    prop.ClrType.IsPrimitive)
                {
                    
                    var lastId = ExecScalar(GetLastInsertIdCommand(transaction));
                    if (IsBlankId(lastId))
                    {
                        return false;
                    }
                    
                    // database generated IDs may be provided in BIGINT format
                    // and the entity may just be an INT
                    // perform the conversion now to prevent a type-cast error.
                    if (lastId.GetType() != prop.ClrType)
                    {
                        lastId = Convert.ChangeType(lastId, prop.ClrType);
                    }
                    
                    if (prop.PropertyInfo is not null)
                    {
                        prop.PropertyInfo.SetValue(entity, lastId);
                    }
                    else if (prop.FieldInfo is not null)
                    {
                        prop.FieldInfo.SetValue(entity, lastId);
                    }
                    else
                    {
                        throw new PropertyWithoutAccessorException(prop);
                    }
                }
            }
            else
            {
                if (!ExecuteCommand(entity, GetInsertCommand(transaction))) return false;
            }
            
            Context.Entry(entity).State = EntityState.Detached;

            return true;
        }

        #endregion
        
        #region Update
        
        private IDbCommand?                                  _updateCommand;
        private IDictionary<string, Func<TEntity, object?>>? _updateParameters;

        private (IDbCommand,IDictionary<string, Func<TEntity, object?>>) GetUpdateCommand(IDbTransaction? transaction)
        {
            if (_updateCommand is not null && _updateParameters is not null)
            {
                _updateCommand.Transaction = transaction;
                return (_updateCommand, _updateParameters);
            }

            var cmd   = (transaction?.Connection as DbConnection ?? Context.Database.GetDbConnection()).CreateCommand();
            var qry   = new StringBuilder();
            var props = GetUpdateProperties();
            var first = true;
            var list  = new Dictionary<string, Func<TEntity, object?>>();

            qry.Append("UPDATE ").Append(Knowledge.QuoteObjectName(TableName)).Append(" SET ");

            foreach (var prop in props)
            {
                if (Key.Properties.Contains(prop)) continue;

                if (!first)
                {
                    qry.Append(", ");
                }

                first = false;

                var param = AddParameterTo(cmd, $"@p_{list.Count}", prop);
                if (prop.PropertyInfo is not null)
                {
                    list.Add(param.ParameterName, x => prop.PropertyInfo.GetValue(x));
                }
                else if (prop.FieldInfo is not null)
                {
                    list.Add(param.ParameterName, x => prop.FieldInfo.GetValue(x));
                }
                else
                {
                    throw new PropertyWithoutAccessorException(prop);
                }

                var propName = prop.GetColumnName(StoreObjectID)
                               ?? throw new PropertyWithoutColumnNameException(prop);
                
                qry.Append(Knowledge.QuoteObjectName(propName))
                   .Append(" = ")
                   .Append(param.ParameterName);
            }

            qry.Append(" WHERE ");
            first = true;
            foreach (var prop in Key.Properties)
            {
                if (!first)
                {
                    qry.Append(" AND ");
                }

                first = false;

                var param = AddParameterTo(cmd, $"@p_{list.Count}", prop);
                if (prop.PropertyInfo is not null)
                {
                    list.Add(param.ParameterName, x => prop.PropertyInfo.GetValue(x));
                }
                else if (prop.FieldInfo is not null)
                {
                    list.Add(param.ParameterName, x=> prop.FieldInfo.GetValue(x));
                }
                else
                {
                    throw new PropertyWithoutAccessorException(prop);
                }

                var propName = prop.GetColumnName(StoreObjectID) 
                               ?? throw new PropertyWithoutColumnNameException(prop);

                qry.Append('(')
                   .Append(Knowledge.QuoteObjectName(propName))
                   .Append(" = ")
                   .Append(param.ParameterName)
                   .Append(')');
            }

            foreach (var prop in ConcurrencyTokens)
            {
                var param = AddParameterTo(cmd, $"@p_{list.Count}", prop);
                list.Add(param.ParameterName, x => Context.Entry(x).Property(prop.Name).OriginalValue);

                var propName = prop.GetColumnName(StoreObjectID)
                               ?? throw new PropertyWithoutColumnNameException(prop);
                
                qry.Append(" AND (")
                   .Append(Knowledge.QuoteObjectName(propName))
                   .Append(" = ")
                   .Append(param.ParameterName)
                   .Append(')');
            }

            cmd.CommandText = qry.ToString();

            _updateCommand = cmd;
            _updateParameters = list;

            _updateCommand.Transaction = transaction;
            
            return (_updateCommand, _updateParameters);
        }
        
        
        /// <inheritdoc />
        public virtual bool Update(TEntity entity, IDbTransaction? transaction)
        {
            return ExecuteCommand(entity, GetUpdateCommand(transaction));
        }
        
        #endregion

        #region Remove
        
        private IDbCommand?                                  _removeCommand;
        private IDictionary<string, Func<TEntity, object?>>? _removeParameters;
        
        private (IDbCommand, IDictionary<string, Func<TEntity, object?>>) GetRemoveCommand(IDbTransaction? transaction)
        {
            if (_removeCommand is not null && _removeParameters is not null)
            {
                _removeCommand.Transaction = transaction;
                return (_removeCommand, _removeParameters);
            }

            var cmd   = (transaction?.Connection as DbConnection ?? Context.Database.GetDbConnection()).CreateCommand();
            var qry   = new StringBuilder();
            var props = GetRemoveProperties();
            var first = true;
            var list  = new Dictionary<string, Func<TEntity, object?>>();

            if (props.Any())
            {
                qry.Append("UPDATE ").Append(Knowledge.QuoteObjectName(TableName)).Append(" SET ");

                foreach (var prop in props)
                {
                    if (Key.Properties.Contains(prop)) continue;

                    if (!first)
                    {
                        qry.Append(", ");
                    }

                    first = false;

                    var param = AddParameterTo(cmd, $"@p_{list.Count}", prop);
                    if (prop.PropertyInfo is not null)
                    {
                        list.Add(param.ParameterName, x => prop.PropertyInfo.GetValue(x));
                    }
                    else if (prop.FieldInfo is not null)
                    {
                        list.Add(param.ParameterName, x => prop.FieldInfo.GetValue(x));
                    }
                    else
                    {
                        throw new PropertyWithoutAccessorException(prop);
                    }

                    var propName = prop.GetColumnName(StoreObjectID)
                                   ?? throw new PropertyWithoutColumnNameException(prop);
                    
                    qry.Append(Knowledge.QuoteObjectName(propName))
                       .Append(" = ")
                       .Append(param.ParameterName);
                }
            }
            else
            {
                qry.Append("DELETE FROM ").Append(Knowledge.QuoteObjectName(TableName));
            }

            qry.Append(" WHERE ");
            first = true;
            foreach (var prop in Key.Properties)
            {
                if (!first)
                {
                    qry.Append(" AND ");
                }

                first = false;

                var param = AddParameterTo(cmd, $"@p_{list.Count}", prop);
                if (prop.PropertyInfo is not null)
                {
                    list.Add(param.ParameterName, x => prop.PropertyInfo.GetValue(x));
                }
                else if (prop.FieldInfo is not null)
                {
                    list.Add(param.ParameterName, x=> prop.FieldInfo.GetValue(x));
                }
                else
                {
                    throw new PropertyWithoutAccessorException(prop);
                }

                var propName = prop.GetColumnName(StoreObjectID)
                               ?? throw new PropertyWithoutColumnNameException(prop);
                
                qry.Append('(')
                   .Append(Knowledge.QuoteObjectName(propName))
                   .Append(" = ")
                   .Append(param.ParameterName)
                   .Append(')');
            }

            foreach (var prop in ConcurrencyTokens)
            {
                var param = AddParameterTo(cmd, $"@p_{list.Count}", prop);
                list.Add(param.ParameterName, x => Context.Entry(x).Property(prop.Name).OriginalValue);

                var propName = prop.GetColumnName(StoreObjectID)
                               ?? throw new PropertyWithoutColumnNameException(prop);
                
                qry.Append(" AND (")
                   .Append(Knowledge.QuoteObjectName(propName))
                   .Append(" = ")
                   .Append(param.ParameterName)
                   .Append(')');
            }

            cmd.CommandText = qry.ToString();

            _removeCommand = cmd;
            _removeParameters = list;
            
            _removeCommand.Transaction = transaction;
            return (_removeCommand, _removeParameters);
        }

        /// <inheritdoc />
        public virtual bool Remove(TEntity entity, IDbTransaction? transaction)
        {
            return ExecuteCommand(entity, GetRemoveCommand(transaction));
        }
        
        #endregion

        /// <inheritdoc />
        public void Dispose()
        {
            _insertCommand?.Dispose();
            _lastInsertIdCommand?.Dispose();
            _updateCommand?.Dispose();
            _removeCommand?.Dispose();
            
            GC.SuppressFinalize(this);
        }
    }
}
