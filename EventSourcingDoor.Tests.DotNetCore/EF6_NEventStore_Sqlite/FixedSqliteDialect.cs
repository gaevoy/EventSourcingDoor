using System;
using System.Globalization;
using NEventStore.Persistence.Sql.SqlDialects;

namespace EventSourcingDoor.Tests.EF6_NEventStore_Sqlite
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
    }
}