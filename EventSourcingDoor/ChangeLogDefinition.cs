using System;

namespace EventSourcingDoor
{
    public class ChangeLogDefinition<TState, TEventBase>
    {
        private Action<TState, TEventBase> _handle = (state, evt) => { };

        public ChangeLogDefinition<TState, TEventBase> On<TEvent>(Action<TState, TEvent> handler)
        {
            var next = _handle;
            _handle = (state, evt) =>
            {
                if (evt is TEvent typedEvt)
                    handler(state, typedEvt);
                else
                    next(state, evt);
            };
            return this;
        }
        // .UseAllMethodsWithName("When")

        public ChangeLog<TState, TEventBase> New(TState state)
            => new ChangeLog<TState, TEventBase>(state, this);

        public void ApplyChange(TState state, TEventBase evt)
            => _handle(state, evt);
    }
}