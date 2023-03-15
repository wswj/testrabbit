using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TestRabbitMQ.EventBus.Abstractions;
using TestRabbitMQ.Events;

namespace TestRabbitMQ.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private readonly IEventBus eventBus;
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger, IEventBus eventBus)
        {
            _logger = logger;
            this.eventBus = eventBus;
        }

        [HttpGet]
        public IEnumerable<WeatherForecast> Get()
        {
            TestEvents testEvents = new TestEvents("消息");
            
            eventBus.Publish(testEvents);
            return null;
        }
    }
}
