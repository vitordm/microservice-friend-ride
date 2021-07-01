using FriendRide.Core.Domain.Commands;
using FriendRide.Core.Domain.Events;
using System.Threading.Tasks;

namespace FriendRide.Core.Domain.Bus
{
    public interface IEventBus
    {
        Task SendCommand<T>(T command) where T : Command;
        void Publish<T>(T @event) where T : Event;

        void Subscribe<T, TH>()
            where T : Event
            where TH : IEventHandler<T>;

    }
}
