using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EventSourcingDoor
{
    public interface IOutbox
    {
        void SaveChanges(IEnumerable<IChangeLog> changes);
        Task SaveChangesAsync(IEnumerable<IChangeLog> changes, CancellationToken cancellation);
    }
}