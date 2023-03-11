using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TestRabbitMQ.EventBus.Events;

namespace TestRabbitMQ.EventBus.Abstractions
{
    public interface IIntegrationEventHandler
    {
    }
    public interface IIntergrationEventHandler<in TIntegrationEvent> : IIntegrationEventHandler where TIntegrationEvent : IntegrationEvent 
    {
        Task Handle(TIntegrationEvent @event);
    }
}
