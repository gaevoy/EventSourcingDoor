using System;
using IEvent = System.Object;

namespace EventSourcingDoor
{
    public class ChangeLogDefinition<TState> where TState : IHaveStreamId
    {
        private Action<TState, IEvent> _handle = (state, evt) => throw new HandlerIsNotDefinedException(evt.GetType());

        public ChangeLogDefinition<TState> On<TEvent>(Action<TState, TEvent> handler)
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

        public ChangeLog<TState> New(TState state)
            => new ChangeLog<TState>(state, this);

        public ChangeLog<TState, TEventBase> New<TEventBase>(TState state)
            => new ChangeLog<TState, TEventBase>(state, this);

        public void ApplyChange(TState state, IEvent evt)
            => _handle(state, evt);
    }
}