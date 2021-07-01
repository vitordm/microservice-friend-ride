using System;

namespace FriendRide.Core.Domain.Events
{
    public abstract class Event
    {
        public DateTimeOffset Timestamp { get; protected set; }

        protected Event()
        {
            Timestamp = DateTimeOffset.Now;
        }
    }
}
