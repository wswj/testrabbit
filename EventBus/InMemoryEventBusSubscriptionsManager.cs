using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TestRabbitMQ.EventBus.Abstractions;
using TestRabbitMQ.EventBus.Events;

namespace TestRabbitMQ.EventBus
{
    public class InMemoryEventBusSubscriptionsManager : IEventBusSubscriptionsManager
    {
        private readonly Dictionary<string, List<SubscriptionInfo>> _handlers;
        private readonly List<Type> _eventTypes;
        public InMemoryEventBusSubscriptionsManager()
        {
            _handlers = new Dictionary<string, List<SubscriptionInfo>>();
            _eventTypes = new List<Type>();
        }
        public bool IsEmpty => _handlers.Count > 0;

        public event EventHandler<string> OnEventRemoved;

        public void AddDynamicSubscription<TH>(string eventName) where TH : IDynamicIntegrationEventHandler
        {
            DoAddSubscription(typeof(TH), eventName, true);
        }

        public void AddSubscription<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>
        {
            var eventName = GetEventKey<T>();
            DoAddSubscription(typeof(TH), eventName, false);
            if (!_eventTypes.Contains(typeof(T)))
            {
                _eventTypes.Add(typeof(T));
            }
        }

        public void Clear()
        {
            _handlers.Clear();
        }

        public string GetEventKey<T>()
        {
            return typeof(T).Name;
        }

        private void RasieOnEventRemoved(string eventName) {
            var handler = OnEventRemoved;
            handler?.Invoke(this,eventName);
        }

        public Type GetEventTypeByName(string eventName)
        {
            return _eventTypes.SingleOrDefault(e=>e.Name==eventName);
        }

        public IEnumerable<SubscriptionInfo> GetHandlersForEvent<T>() where T : IntegrationEvent
        {
            var key = GetEventKey<T>();
            return _handlers[key];

        }

        public IEnumerable<SubscriptionInfo> GetHandlersForEvent(string eventName)
        {
            return _handlers[eventName];
        }

        public bool HasSubscriptionsForEvent<T>() where T : IntegrationEvent
        {
            var key = GetEventKey<T>();
            return HasSubscriptionsForEvent(key);
        }

        public bool HasSubscriptionsForEvent(string eventName)
        {
            return _handlers.ContainsKey(eventName);
        }

        public void RemoveDynamicSubscription<TH>(string eventName) where TH : IDynamicIntegrationEventHandler
        {
            var handleToRemove = FindDynamicSubscriptionToRemove<TH>(eventName);
            DoRemoveHandler(eventName,handleToRemove);
        }

        private SubscriptionInfo FindDynamicSubscriptionToRemove<TH>(string eventName)
            where TH : IDynamicIntegrationEventHandler
        {
            return DoFindSubscriptionToRemove(eventName, typeof(TH));
        }

        private SubscriptionInfo DoFindSubscriptionToRemove(string eventName, Type handlerType)
        {
            if (!HasSubscriptionsForEvent(eventName))
            {
                return null;
            }
            return _handlers[eventName].SingleOrDefault(e => e.HandlerType == handlerType);
        }
        private void DoRemoveHandler(string eventName, SubscriptionInfo subscriptionInfo)
        {
            if (subscriptionInfo != null)
            {
                _handlers[eventName].Remove(subscriptionInfo);
                if (!_handlers.Any())
                {
                    _handlers.Remove(eventName);
                    var eventType = _eventTypes.SingleOrDefault(e => e.Name == eventName);
                    if (eventType != null)
                    {
                        _eventTypes.Remove(eventType);
                    }
                    RasieOnEventRemoved(eventName);
                }
            }
        }

        public void RemoveSubscription<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>
        {
            var eventName = GetEventKey<T>();
            var handleToRemove= DoFindSubscriptionToRemove(eventName,typeof(TH));
            DoRemoveHandler(eventName,handleToRemove);

        }
        private void DoAddSubscription(Type handleType, string eventName, bool isDynamic)
        {
            if (!HasSubscriptionsForEvent(eventName))
            {
                _handlers.Add(eventName, new List<SubscriptionInfo>());
            }

            if (_handlers[eventName].Any(s => s.HandlerType == handleType))
            {
                throw new ArgumentException("已注册过该事件处理类,请勿重复注册");
            }

            if (isDynamic)
            {
                _handlers[eventName].Add(SubscriptionInfo.Dynamic(handleType));
            }
            else
            {
                _handlers[eventName].Add(SubscriptionInfo.Typed(handleType));
            }
        }
    }
}
