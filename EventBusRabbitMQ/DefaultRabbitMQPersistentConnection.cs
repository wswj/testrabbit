using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace TestRabbitMQ.EventBusRabbitMQ
{
    public class DefaultRabbitMQPersistentConnection : IRabbitMQPersistentConnection
    {
        private readonly IConnectionFactory _connectionFactory;
        private readonly int _retryCount;
        private IConnection _connection;
        private readonly ILogger<DefaultRabbitMQPersistentConnection> _logger;
        public bool Disposed;
        readonly object _syncRoot = new();
        public DefaultRabbitMQPersistentConnection(IConnectionFactory connectionFactory,int retryCount=5) {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _retryCount = retryCount;
        }
        public bool IsConnected => _connection!=null&&_connection.IsOpen && !Disposed;

        public IModel CreateModel()
        {
            if (!IsConnected) throw new Exception("消息队列连接已关闭！");
            return _connection.CreateModel();
        }

        public void Dispose()
        {
            if (Disposed) return;
            Disposed = true;
            try
            {
                _connection.ConnectionShutdown -= OnConnectionShutdown;
                _connection.CallbackException -= OnCallbackException;
                _connection.ConnectionBlocked -= OnConnectionBlocked;
                _connection.Dispose();
            }
            catch (IOException ex)
            {
                _logger.LogCritical(ex.ToString());
            }
        }

        public bool TryConnect()
        {
            lock (_syncRoot) {
                var policy = RetryPolicy.Handle<SocketException>()
                    .Or<BrokerUnreachableException>()
                    .WaitAndRetry(_retryCount,retryAttemp=>TimeSpan.FromSeconds(Math.Pow(2,retryAttemp)),(ex,time)=> { 
                        
                    });
                policy.Execute(() => {
                    _connection = _connectionFactory.CreateConnection();
                });
                if (IsConnected)
                {
                    _connection.ConnectionShutdown += OnConnectionShutdown;
                    _connection.CallbackException += OnCallbackException;
                    _connection.ConnectionBlocked += OnConnectionBlocked;
                    return true;
                }
                else {
                    return false;
                }
                
            }
        }
        void OnConnectionShutdown(object sender,ShutdownEventArgs reason) {
            if (Disposed) return;
            TryConnect();
        }
        void OnCallbackException(object sender,CallbackExceptionEventArgs e) {
            if (Disposed) return;
            TryConnect();
        }
        private void OnConnectionBlocked(object sender, ConnectionBlockedEventArgs e)
        {
            if (Disposed) return;
            TryConnect();
        }
    }
}
