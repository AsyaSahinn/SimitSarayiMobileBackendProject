using EventBus.AzureServiceBus;
using EventBus.Base;
using EventBus.Base.Abstraction;
using EventBus.Factory;
using EventBus.RabbitMQ;
using EventBus.UnitTests.Events.EventHandlers;
using EventBus.UnitTests.Events.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace EventBus.UnitTests
{
    public class EventBusTests
    {
        private ServiceCollection _services;
        public EventBusTests()
        {
            _services = new ServiceCollection();
            _services.AddLogging(configure => configure.AddConsole());
        }

   
        public void subscribe_event_on_rabbitmq_test()
        {
            _services.AddSingleton<IEventBus>(sp =>
            {
                EventBusConfig config = new()
                {
                    ConnectionRetryCount = 3,
                    SubscriberClientAppName = "EventBus.UnitTest",
                    DefaultTopicName = "SimSaMobileAppEventBusTopicName",
                    EventBusType = EventBusType.RabbitMQ,
                    EventNameSuffix = "IntegrationEvent",
                    //Connection = new ConnectionFactory() //default ayarlar
                    //{
                    //    HostName = "localhost",
                    //    Port = 5672,
                    //    UserName = "guest",  
                    //    Password = "guest",
                    //}

                };
                return EventBusFactory.Create(config, sp);
            });
            var sp = _services.BuildServiceProvider();

            var eventBus = sp.GetRequiredService<IEventBus>();

            eventBus.Subscribe<OrderCreatedIntegrationEvent,OrderCreatedIntegrationEventHandler>();    
            eventBus.UnSubscribe<OrderCreatedIntegrationEvent,OrderCreatedIntegrationEventHandler>();    
        }

    }
}