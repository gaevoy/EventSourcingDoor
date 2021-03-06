using System.Collections.Generic;
using System.Linq;
using IEvent = System.Object;

namespace EventSourcingDoor
{
    public static class ChangeLog
    {
        public static ChangeLogDefinition<TState> For<TState>()
            => new ChangeLogDefinition<TState>();
    }

    public class ChangeLog<TState> : IChangeLog
    {
        private readonly TState _state;
        private readonly ChangeLogDefinition<TState> _definition;
        private readonly List<IEvent> _changes = new List<IEvent>();
        private readonly IHaveVersion _versionOwner;
        private readonly IHaveStreamId _streamIdOwner;

        public ChangeLog(TState state, ChangeLogDefinition<TState> definition)
        {
            _state = state;
            _definition = definition;
            _versionOwner = state is IHaveVersion versionState ? versionState : null;
            _streamIdOwner = state is IHaveStreamId streamIdState ? streamIdState : null;
        }

        public string StreamId => _streamIdOwner?.StreamId;

        public IEnumerable<IEvent> GetUncommittedChanges() => _changes;

        public void MarkChangesAsCommitted() => _changes.Clear();

        public void LoadFromHistory(IEnumerable<IEvent> history)
        {
            long version = 0;
            foreach (var evt in history)
            {
                version++;
                _definition.ApplyChange(_state, evt);
            }

            if (_versionOwner != null)
                _versionOwner.Version = version;
        }

        public void Apply(IEvent evt)
        {
            _definition.ApplyChange(_state, evt);
            _changes.Add(evt);
            if (_versionOwner == null) return;
            _versionOwner.Version++;
            foreach (var change in _changes)
                if (change is IHaveVersion eventWithVersion)
                    eventWithVersion.Version = _versionOwner.Version;
        }
    }

    public class ChangeLog<TState, TEventBase> : ChangeLog<TState>
    {
        public ChangeLog(TState state, ChangeLogDefinition<TState> definition) : base(state, definition)
        {
        }

        public new IEnumerable<TEventBase> GetUncommittedChanges()
            => base.GetUncommittedChanges().Cast<TEventBase>();

        public void LoadFromHistory(IEnumerable<TEventBase> history)
            => base.LoadFromHistory(history.Cast<IEvent>());

        public void Apply(TEventBase evt)
            => base.Apply(evt);
    }
}