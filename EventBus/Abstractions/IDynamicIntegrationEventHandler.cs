using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TestRabbitMQ.EventBus.Abstractions
{
    public interface IDynamicIntegrationEventHandler
    {
        Task Handle(dynamic eventData);
    }
}
