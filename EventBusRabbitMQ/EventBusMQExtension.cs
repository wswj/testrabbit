﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TestRabbitMQ.EventBus;
using TestRabbitMQ.EventBus.Abstractions;

namespace TestRabbitMQ.EventBusRabbitMQ
{
    public static class EventBusMQExtension
    {
        public static IServiceCollection AddIntegrationServices(this IServiceCollection services,IConfiguration configuration) {
            services.AddSingleton<IRabbitMQPersistentConnection>(sp=> {
                var factory = new ConnectionFactory() {
                    HostName = configuration["EventBusConnection"],
                    DispatchConsumersAsync=true
                };
                if (!string.IsNullOrEmpty(configuration["EventBusUserName"])) {
                    factory.UserName = configuration["EventBusUserName"];
                }
                if (!string.IsNullOrEmpty(configuration["EventBusPassword"]))
                {
                    factory.Password = configuration["EventBusPassword"];
                }
                var retryCount = 5;
                if (!string.IsNullOrEmpty(configuration["EventBusRetryCount"])) {
                    retryCount = int.Parse(configuration["EventBusRetryCount"]);
                }
                return new DefaultRabbitMQPersistentConnection(factory,retryCount);
            });
            return services;
        }


        public static IServiceCollection AddEventBus(this IServiceCollection service, IConfiguration configuration)
        {
            service.AddSingleton<IEventBus, EventBusRabbitMQ>(sp=> {
                var subscriptionClientName = configuration["SubscriptionClientName"];
                var rabbitMQPersistentConnection = sp.GetRequiredService<IRabbitMQPersistentConnection>();
                var eventBusSubcriptionsManager = sp.GetRequiredService<IEventBusSubscriptionsManager>();
            });
        }
    }
}
