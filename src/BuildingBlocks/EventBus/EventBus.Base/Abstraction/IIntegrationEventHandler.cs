using EventBus.Base.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventBus.Base.Abstraction
{
    //where ile TIntegrationEvent classı IntegrationEvent'den türemek zorundadır koşulunu ekliyoruz.
    public interface IIntegrationEventHandler<TIntegrationEvent>: IntegrationEventHandler where TIntegrationEvent:IntegrationEvent
    {
        Task Handle(TIntegrationEvent @event);
    }

    public interface IntegrationEventHandler
    {

    }
}
