using FriendRide.Core.Domain.Bus;
using FriendRide.Core.Domain.Commands;
using FriendRide.Core.Domain.Events;
using FriendRide.Core.Infra.Bus.Settings;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FriendRide.Core.Infra.Bus
{
    public class RabbitMQBus : IEventBus
    {
        private readonly IMediator mediator;
        private readonly IServiceScopeFactory serviceScopeFactory;
        private readonly RabbitMQBusSettings settings;
        private readonly Dictionary<string, List<Type>> handlers = new Dictionary<string, List<Type>>();
        private readonly List<Type> eventTypes = new List<Type>();


        public RabbitMQBus(
            IMediator mediator,
            IServiceScopeFactory serviceScopeFactory,
            RabbitMQBusSettings settings
        )
        {
            this.mediator = mediator;
            this.serviceScopeFactory = serviceScopeFactory;
            this.settings = settings;
        }

        public Task SendCommand<T>(T command) where T : Command
        {
            mediator.Send(command);
            return Task.CompletedTask;
        }


        public void Publish<T>(T @event) where T : Event
        {
            var factory = new ConnectionFactory
            {
                HostName = settings.HostName
            };

            using (IConnection connection = factory.CreateConnection())
            using (IModel channel = connection.CreateModel())
            {
                string eventName = @event.GetType().Name;
                channel.QueueDeclare(eventName, false, false, false, null);

                string message = JsonConvert.SerializeObject(@event);
                byte[] body = Encoding.UTF8.GetBytes(message);

                channel.BasicPublish(string.Empty, eventName, null, body);
            }

        }

        public void Subscribe<T, TH>()
            where T : Event
            where TH : IEventHandler<T>
        {
            string eventName = typeof(T).Name;
            Type handlerType = typeof(TH);

            if (!eventTypes.Contains(handlerType))
                eventTypes.Add(handlerType);

            if (!handlers.ContainsKey(eventName))
                handlers.Add(eventName, new());

            if (handlers[eventName].Any(t => t.GetType() == handlerType))
                throw new ArgumentException(
                    $"Handler Type {handlerType.Name} already is registered for '{eventName}'", nameof(handlerType)
                );

            handlers[eventName].Add(handlerType);

            StartBasicConsume<T>();
        }

        private void StartBasicConsume<T>() where T : Event
        {
            var factory = new ConnectionFactory
            {
                HostName = settings.HostName,
                DispatchConsumersAsync = true
            };

            IConnection connection = factory.CreateConnection();
            IModel channel = connection.CreateModel();

            string eventName = typeof(T).Name;

            channel.QueueDeclare(eventName, false, false, false, null);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.Received += Consumer_Received;

            channel.BasicConsume(eventName, false, consumer);
        }

        private async Task Consumer_Received(object sender, BasicDeliverEventArgs @event)
        {
            string eventName = @event.RoutingKey;
            string message = Encoding.UTF8.GetString(@event.Body.ToArray());

            try
            {
                await ProcessEvent(eventName, message).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                //TODO: Apply loggin here
                throw ex;
            }
        }

        private async Task ProcessEvent(string eventName, string message)
        {
            if (!handlers.ContainsKey(eventName))
                return;

            using (var scope = serviceScopeFactory.CreateScope())
            {
                List<Type> subscriptions = handlers[eventName];

                foreach (var subscription in subscriptions)
                {
                    object handler = scope.ServiceProvider.GetService(subscription);
                    if (handler == null)
                        return;
                    Type eventType = eventTypes.SingleOrDefault(t => t.Name == eventName);
                    object @event = JsonConvert.DeserializeObject(message, eventType);

                    Type concreteType = typeof(IEventHandler<>).MakeGenericType(eventType);
                    await (Task)concreteType.GetMethod("Handle")
                            .Invoke(handler, new object[] { @event });
                }
            }
        }
    }
}
