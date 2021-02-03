using System;

namespace EventSourcingDoor
{
    public static class StreamDefinition
    {
        public static StreamDefinition<TState, TEventBase> For<TState, TEventBase>()
            => new StreamDefinition<TState, TEventBase>();
    }

    public class StreamDefinition<TState, TEventBase>
    {
        private Action<TState, TEventBase> _handle = (state, evt) => { };

        public StreamDefinition<TState, TEventBase> On<TEvent>(Action<TState, TEvent> handler)
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

        public EventStream<TState, TEventBase> New(TState state)
            => new EventStream<TState, TEventBase>(state, this);

        public void ApplyChange(TState state, TEventBase evt)
            => _handle(state, evt);
    }
}