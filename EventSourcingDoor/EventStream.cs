using System.Collections.Generic;

namespace EventSourcingDoor
{
    public class EventStream<TState, TEventBase> : IAmEventStream<TEventBase>
    {
        private readonly TState _state;
        private readonly StreamDefinition<TState, TEventBase> _definition;
        private readonly List<TEventBase> _changes = new List<TEventBase>();

        public EventStream(TState state, StreamDefinition<TState, TEventBase> definition)
        {
            _state = state;
            _definition = definition;
        }

        public IEnumerable<TEventBase> GetUncommittedChanges()
            => _changes;

        public void MarkChangesAsCommitted()
            => _changes.Clear();

        public void LoadFromHistory(IEnumerable<TEventBase> history)
        {
            foreach (var evt in history)
                _definition.ApplyChange(_state, evt);
        }

        public void ApplyChange(TEventBase evt)
        {
            _definition.ApplyChange(_state, evt);
            _changes.Add(evt);
        }
    }
}