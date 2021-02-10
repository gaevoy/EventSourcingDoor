using System.Data.Entity;

namespace EventSourcingDoor.Tests.Utils
{
    public static class DbContextExt
    {
        public static void DetachAll(this DbContext db) {

            foreach (var dbEntityEntry in db.ChangeTracker.Entries()) {

                if (dbEntityEntry.Entity != null) {
                    dbEntityEntry.State = EntityState.Detached;
                }
            }
        }
    }
}