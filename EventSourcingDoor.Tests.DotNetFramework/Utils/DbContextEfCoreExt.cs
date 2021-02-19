using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace EventSourcingDoor.Tests.Utils
{
    public static class DbContextEfCoreExt
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