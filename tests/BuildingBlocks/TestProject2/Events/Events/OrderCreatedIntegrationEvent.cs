using EventBus.Base.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventBus.UnitTests.Events.Events
{
    public  class OrderCreatedIntegrationEvent : IntegrationEvent
    {
        public OrderCreatedIntegrationEvent(int id)
        {
            Id = id;
        }

        public int Id { get; set; } 
    }
}
