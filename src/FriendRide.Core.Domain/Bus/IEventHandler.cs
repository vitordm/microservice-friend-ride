using FriendRide.Core.Domain.Events;
using System.Threading.Tasks;

namespace FriendRide.Core.Domain.Bus
{
    public interface IEventHandler
    {

    }
    public interface IEventHandler<in TEvent> : IEventHandler
            where TEvent : Event
    {
        Task Handle(TEvent @event);
    }
}
