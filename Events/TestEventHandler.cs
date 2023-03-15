using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TestRabbitMQ.EventBus.Abstractions;

namespace TestRabbitMQ.Events
{
    public class TestEventHandler : IIntegrationEventHandler<TestEvents>
    {
        public async Task Handle(TestEvents @event)
        {
            Console.WriteLine(@event.Message);
        }
    }
}
