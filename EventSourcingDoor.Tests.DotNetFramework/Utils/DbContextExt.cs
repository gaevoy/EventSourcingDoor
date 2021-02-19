using System.Data.Entity;
using System.Linq;

namespace EventSourcingDoor.Tests.Utils
{
    public static class DbContextExt
    {
        public static void DetachAll(this DbContext db)
        {
            foreach (var dbEntityEntry in db.ChangeTracker.Entries().ToList())
            {
                if (dbEntityEntry.Entity != null)
                {
                    dbEntityEntry.State = EntityState.Detached;
                }
            }
        }
    }
}