using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TestRabbitMQ.EventBus;
using TestRabbitMQ.EventBus.Abstractions;
using TestRabbitMQ.EventBus.Events;

namespace TestRabbitMQ.EventBusRabbitMQ
{
    public class EventBusRabbitMQ : IEventBus, IDisposable
    {
        const string BROKER_NAME = "test_message";

        private readonly IRabbitMQPersistentConnection _persistentConnection;
        private readonly IEventBusSubscriptionsManager _subsManager;
        private readonly ServiceLifetime serviceLifetime;
        private readonly int _retryCount;
        private readonly IServiceScopeFactory serviceScopeFactory;

        private IModel _consumerChannel;
        private string _queueName;
        public void Dispose()
        {
            if (_consumerChannel != null)
            {
                _consumerChannel.Dispose();
            }

            _subsManager.Clear();
        }
        public EventBusRabbitMQ(IRabbitMQPersistentConnection persistentConnection, IEventBusSubscriptionsManager subscriptionsManager, string queueName = null, int retryCount = 5, IServiceScopeFactory serviceScopeFactory = null)
        {
            _persistentConnection = persistentConnection ?? throw new Exception("消息队列连接信息为空！");
            _subsManager = _subsManager?? new InMemoryEventBusSubscriptionsManager();
            _queueName = queueName;
            _consumerChannel = CreateConsumerChannel();
            this.serviceScopeFactory = serviceScopeFactory;
        }
        private IModel CreateConsumerChannel() { 
            if (!_persistentConnection.IsConnected) {
                _persistentConnection.TryConnect();
            }
            var channel = _persistentConnection.CreateModel();
            channel.ExchangeDeclare(exchange: BROKER_NAME, type: "direct");
            channel.QueueDeclare(queue: _queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
            channel.CallbackException += (sender, ea) => {
                _consumerChannel.Dispose();
                _consumerChannel = CreateConsumerChannel();
                StartBasicConsume();
            };
            return channel;
        }
        private void StartBasicConsume() {
            if (_consumerChannel != null)
            {
                var consumer = new AsyncEventingBasicConsumer(_consumerChannel);
                consumer.Received += Consumer_Received;
                _consumerChannel.BasicConsume(
                    queue: _queueName,
                    autoAck: false,
                    consumer: consumer);
            }
            else { 
                
            }
        }
        private async Task Consumer_Received(object sender,BasicDeliverEventArgs eventArgs) {
            var eventName = eventArgs.RoutingKey;
            var message = Encoding.UTF8.GetString(eventArgs.Body.Span);
            try {
                if (message.ToLowerInvariant().Contains("throw-fake-exception")) {
                    throw new Exception($"非法消息:{message}！");
                }
                await ProcessEvent(eventName, message);
            }
            catch (Exception ex) {
                
            }
        }

        private async Task ProcessEvent(string eventName,string message) {
            if (_subsManager.HasSubscriptionsForEvent(eventName)) {
                using var scope= serviceScopeFactory.CreateScope();
                var subscriptions = _subsManager.GetHandlersForEvent(eventName);
                foreach (var subscription in subscriptions)
                {
                    if (subscription.IsDynamic)
                    {
                        if (scope.ServiceProvider.GetService(subscription.HandlerType) is not IDynamicIntegrationEventHandler handler) continue;
                        using dynamic eventData = JsonDocument.Parse(message);
                        await Task.Yield();
                        await handler.Handle(eventData);
                    }
                    else {
                        var handler = scope.ServiceProvider.GetService(subscription.HandlerType);
                        if (handler == null) continue;
                        var eventType = _subsManager.GetEventTypeByName(eventName);
                        var integrationEvent = JsonSerializer.Deserialize(message,eventType, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                        var concreteType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
                        await Task.Yield();
                        await (Task)concreteType.GetMethod("handle").Invoke(handler, new object[] { integrationEvent });
                    }
                }
            }
        }

        public void Publish(IntegrationEvent @event)
        {
            if (!_persistentConnection.IsConnected) {
                _persistentConnection.TryConnect();
            }
            var policy = RetryPolicy.Handle<BrokerUnreachableException>()
                .Or<SocketException>()
                .WaitAndRetry(_retryCount,retryAttempt=>TimeSpan.FromSeconds(Math.Pow(2,retryAttempt)),(ex,time)=> {
                    
                });
            var eventName = @event.GetType().Name;
            using var channel = _persistentConnection.CreateModel();
            channel.ExchangeDeclare(exchange: BROKER_NAME, type: "direct");
            var body = JsonSerializer.SerializeToUtf8Bytes(@event,@event.GetType(), new JsonSerializerOptions
            {
                WriteIndented = true
            });
            policy.Execute(()=> {
                var properties = channel.CreateBasicProperties();
                properties.DeliveryMode = 2;
                channel.BasicPublish(exchange: BROKER_NAME, eventName, mandatory: true, properties,body);
            });

        }

        public void Subscribe<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>
        {
            var eventName = _subsManager.GetEventKey<T>();
            DoInternalSubscription(eventName);
            _subsManager.AddSubscription<T,TH>();
            StartBasicConsume();
        }

        private void DoInternalSubscription(string eventName) {
            var containsKey = _subsManager.HasSubscriptionsForEvent(eventName);
            if (!containsKey) {
                if (!_persistentConnection.IsConnected) {
                    _persistentConnection.TryConnect();
                }
                _consumerChannel.QueueBind(queue:_queueName,
                    exchange:BROKER_NAME,
                    routingKey:eventName);
            }
        }

        public void UnsubscribeDynamic<TH>(string eventName) where TH : IDynamicIntegrationEventHandler
        {
            _subsManager.RemoveDynamicSubscription<TH>(eventName);
        }

        public void Unsubscribe<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>
        {
            var eventName = _subsManager.GetEventKey<T>();
            _subsManager.RemoveSubscription<T,TH>();
        }

        public void SubscribeDynamic<TH>(string eventName) where TH : IDynamicIntegrationEventHandler
        {
            DoInternalSubscription(eventName);
            _subsManager.AddDynamicSubscription<TH>(eventName);
            StartBasicConsume();
        }
    }
}
