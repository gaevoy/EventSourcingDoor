using System.Linq;

namespace EventSourcingDoor.Tests.Utils
{
    public static class DbContextExt
    {
        public static void DetachAll(this System.Data.Entity.DbContext db)
        {
            foreach (var dbEntityEntry in db.ChangeTracker.Entries().ToList())
            {
                if (dbEntityEntry.Entity != null)
                {
                    dbEntityEntry.State = System.Data.Entity.EntityState.Detached;
                }
            }
        }
        public static void DetachAll(this Microsoft.EntityFrameworkCore.DbContext db)
        {
            foreach (var dbEntityEntry in db.ChangeTracker.Entries().ToList())
            {
                if (dbEntityEntry.Entity != null)
                {
                    dbEntityEntry.State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                }
            }
        }
    }
}