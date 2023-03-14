using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TestRabbitMQ.EventBus.Events;

namespace TestRabbitMQ.EventBus.Abstractions
{
    public interface IEventBus
    {
        void Publish(IntegrationEvent @event);
        void Subscribe<T, TH>()
            where T : IntegrationEvent
            where TH : IIntergrationEventHandler<T>;
        void SubscribeDynamic<TH>(string eventName)
        where TH : IDynamicIntegrationEventHandler;
        void UnsubscribeDynamic<TH>(string eventName)
        where TH : IDynamicIntegrationEventHandler;

        void Unsubscribe<T, TH>()
            where TH : IIntergrationEventHandler<T>
            where T : IntegrationEvent;
    }
}
