using FriendRide.Core.Domain.Events;
using System;

namespace FriendRide.Core.Domain.Commands
{
    public abstract class Command : Message
    {
        public DateTimeOffset Timestamp { get; protected set; }

        public Command()
        {
            Timestamp = DateTimeOffset.Now;
        }
    }
}
