using EventBus.Base.Abstraction;
using EventBus.UnitTests.Events.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventBus.UnitTests.Events.EventHandlers
{
    internal class OrderCreatedIntegrationEventHandler : IIntegrationEventHandler<OrderCreatedIntegrationEvent>
    {
        public Task Handle(OrderCreatedIntegrationEvent @event)
        {
           //
           return Task.CompletedTask;   
        }
    }
}
