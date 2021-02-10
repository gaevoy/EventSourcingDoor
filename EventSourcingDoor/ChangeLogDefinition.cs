using System;
using System.Collections.Generic;
using IEvent = System.Object;

namespace EventSourcingDoor
{
    public class ChangeLogDefinition<TState> where TState : IHaveStreamId
    {
        private readonly Dictionary<Type, Action<TState, IEvent>> _handle =
            new Dictionary<Type, Action<TState, object>>();

        public ChangeLogDefinition<TState> On<TEvent>(Action<TState, TEvent> handler)
        {
            _handle[typeof(TEvent)] = (state, evt) => handler(state, (TEvent) evt);
            return this;
        }
        // .UseAllMethodsWithName("When")

        public ChangeLog<TState> New(TState state)
            => new ChangeLog<TState>(state, this);

        public ChangeLog<TState, TEventBase> New<TEventBase>(TState state)
            => new ChangeLog<TState, TEventBase>(state, this);

        public void ApplyChange(TState state, IEvent evt)
        {
            if (!_handle.TryGetValue(evt.GetType(), out var handle))
                throw new HandlerIsNotDefinedException(evt.GetType());
            handle(state, evt);
        }
    }
}