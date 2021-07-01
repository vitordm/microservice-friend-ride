namespace FriendRide.Core.Domain.Events
{
    public abstract class Message
    {
        public string MessageType { get; protected set; }

        public Message()
        {
            MessageType = GetType().Name;
        }
    }
}
