using System.Data;
using System.Threading;

namespace EventSourcingDoor
{
    public class AmbientContext
    {
        private static readonly AsyncLocal<IDbConnection> Connection = new AsyncLocal<IDbConnection>();
        private static readonly AsyncLocal<IDbTransaction> Transaction = new AsyncLocal<IDbTransaction>();

        public static IDbConnection CurrentConnection
        {
            get => Connection.Value;
            set => Connection.Value = value;
        }
        public static IDbTransaction CurrentTransaction
        {
            get => Transaction.Value;
            set => Transaction.Value = value;
        }
    }
}