using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using NUnit.Framework;
using SqlStreamStore;

namespace EventSourcingDoor.Tests
{
    public class PlaygroundTests
    {
        [Test]
        public void Test1()
        {
            var user = new User(Guid.NewGuid(), "Bond");
            user.Rename("James Bond");
            var events = user.Changes.GetUncommittedChanges();

            var user2 = new User();
            user2.Changes.LoadFromHistory(user.Changes.GetUncommittedChanges());
        }

        [Test]
        public void Test2()
        {
            var user = new UserAggregate(Guid.NewGuid(), "Bond");
            user.Rename("James Bond");
            var events = user.Changes.GetUncommittedChanges();

            var user2 = new UserAggregate();
            user2.Changes.LoadFromHistory(user.Changes.GetUncommittedChanges());
        }

        public string ConnectionString => "server=localhost;database=EventSourcingDoor;UID=sa;PWD=sa123";
        private IStreamStore _streamStore;

        [SetUp]
        public async Task InitDatabase()
        {
            var streamStore = new MsSqlStreamStoreV3(new MsSqlStreamStoreV3Settings(ConnectionString));
            _streamStore = streamStore;
            var db = new Db(ConnectionString, _streamStore);
            //await streamStore.CreateSchemaIfNotExists();
            //db.Database.CreateIfNotExists();
        }

        [Test]
        public async Task SaveAsync()
        {
            var newUser = new UserAggregate(Guid.NewGuid(), "Bond");
            newUser.Rename("James Bond");

            using (var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            using (var db = new Db(ConnectionString, _streamStore))
            {
                // db.Users.Add(newUser);
                // await db.SaveChangesAsync();

                var users = await db.Users.ToListAsync();
                foreach (var user in users)
                {
                    user.Rename(user.Name + "6");
                }

                await db.SaveChangesAsync();
                transaction.Complete();
            }
        }

        [Test]
        public void SaveSync()
        {
            using (var transaction = new TransactionScope())
            using (var db = new Db(ConnectionString, _streamStore))
            {
                // db.Users.Add(newUser);
                // await db.SaveChangesAsync();

                var users = db.Users.ToList();
                foreach (var user in users)
                {
                    user.Rename(user.Name + "7");
                }

                db.SaveChanges();
                transaction.Complete();
            }
        }

        public class Db : EventSourcedDbContext
        {
            public Db(string connectionString, IStreamStore streamStore) : base(connectionString, streamStore)
            {
            }

            public DbSet<UserAggregate> Users { get; set; }
        }
    }
}