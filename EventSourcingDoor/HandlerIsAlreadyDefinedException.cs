using System;

namespace EventSourcingDoor
{
    public class HandlerIsAlreadyDefinedException : Exception
    {
        public readonly Type EventType;

        public HandlerIsAlreadyDefinedException(Type eventType) : base($"Handler for {eventType.Name} is already defined")
        {
            EventType = eventType;
        }
    }
}