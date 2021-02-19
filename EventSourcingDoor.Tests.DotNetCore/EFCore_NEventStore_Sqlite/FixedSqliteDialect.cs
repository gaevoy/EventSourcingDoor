using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Transactions;
using NEventStore.Persistence.Sql;
using NEventStore.Persistence.Sql.SqlDialects;

namespace EventSourcingDoor.Tests.EFCore_NEventStore_Sqlite
{
    public class FixedSqliteDialect : SqliteDialect
    {
        public override DateTime ToDateTime(object value)
        {
            if (value is string str && !string.IsNullOrEmpty(str))
                return DateTime
                    .Parse(str, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal)
                    .ToUniversalTime();
            return base.ToDateTime(value);
        }

        public override IDbStatement BuildStatement(TransactionScope scope, IDbConnection connection,
            IDbTransaction transaction)
        {
            // var con = AmbientContext.CurrentConnection;

            var con = AmbientContext.CurrentConnection;
            con?.Open();

            var statement = base.BuildStatement(scope, con ?? connection, transaction);
            if (con != null)
            {
                statement = new DbStatementWrapper(this, null, con, null);
            }

            return statement;
        }

        public override IDbTransaction OpenTransaction(IDbConnection connection)
        {
            if (AmbientContext.CurrentConnection != null)
                return null;
            return base.OpenTransaction(connection);
        }
    }

    public class DbStatementWrapper : CommonDbStatement
    {
        private readonly IDbConnection _connection;

        public DbStatementWrapper(ISqlDialect dialect, TransactionScope scope, IDbConnection connection,
            IDbTransaction transaction) : base(dialect, scope, connection, transaction)
        {
            _connection = connection;
        }

        protected override IDbCommand BuildCommand(string statement)
        {
            IDbCommand command = _connection.CreateCommand();
            command.CommandText = statement;
            BuildParameters(command);
            return command;
        }

        protected override void Dispose(bool disposing)
        {
            
        }
    }
}