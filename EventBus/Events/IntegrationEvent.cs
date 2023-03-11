using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TestRabbitMQ.EventBus.Events
{
    public class IntegrationEvent
    {
        public IntegrationEvent() {
            Id = Guid.NewGuid();
            CreateDate = DateTime.Now;
        }
        [JsonConstructor]
        public IntegrationEvent(Guid id,DateTime createDate) {
            Id = id;
            CreateDate = createDate;
        }
        [JsonInclude]
        public Guid Id { get; private set; }
        [JsonInclude]
        public DateTime CreateDate { get; set; }
    }
}
