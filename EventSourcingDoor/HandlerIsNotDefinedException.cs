using System;

namespace EventSourcingDoor
{
    public class HandlerIsNotDefinedException : Exception
    {
        public readonly Type EventType;

        public HandlerIsNotDefinedException(Type eventType) : base($"Handler for {eventType.Name} is not defined")
        {
            EventType = eventType;
        }
    }
}