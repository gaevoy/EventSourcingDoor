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
        private readonly IHaveVersion _versionState;

        public ChangeLog(TState state, ChangeLogDefinition<TState> definition)
        {
            _state = state;
            _definition = definition;
            _versionState = state is IHaveVersion versionState ? versionState : null;
        }

        public IEnumerable<IEvent> GetUncommittedChanges() => _changes;

        public void MarkChangesAsCommitted() => _changes.Clear();

        public void LoadFromHistory(IEnumerable<IEvent> history)
        {
            foreach (var evt in history)
                _definition.ApplyChange(_state, evt);
        }

        public void ApplyChange(IEvent evt)
        {
            _definition.ApplyChange(_state, evt);
            _changes.Add(evt);
            if (_versionState == null) return;
            _versionState.Version++;
            foreach (var change in _changes)
                if (change is IHaveVersion eventWithVersion)
                    eventWithVersion.Version = _versionState.Version;
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

        public void ApplyChange(TEventBase evt)
            => base.ApplyChange(evt);
    }
}