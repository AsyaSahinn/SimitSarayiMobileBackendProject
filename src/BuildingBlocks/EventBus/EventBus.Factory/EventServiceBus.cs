using EventBus.Base;
using EventBus.RabbitMQ;

namespace EventBus.Factory
{
    internal class EventServiceBus : EventBusRabbitMQ
    {
        public EventServiceBus(EventBusConfig config, IServiceProvider serviceProvider) : base(config, serviceProvider)
        {
        }
    }
}