using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TestRabbitMQ.EventBus.Events;

namespace TestRabbitMQ.Events
{
    public class TestEvents: IntegrationEvent
    {
        public string Message { get; set; }
        public TestEvents(string message) {
            Message = message;
        }
    }
}
