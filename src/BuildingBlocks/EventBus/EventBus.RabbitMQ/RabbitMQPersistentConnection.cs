using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace EventBus.RabbitMQ
{
    public class RabbitMQPersistentConnection : IDisposable
    {
        private readonly IConnectionFactory _connectionFactory;
        private readonly int _retryCount;
        private IConnection connection;
        private object lock_object = new object();
        private bool _disposed = false; 
        public RabbitMQPersistentConnection(IConnectionFactory connectionFactory , int retryCount=5)
        {
            _connectionFactory = connectionFactory;
            _retryCount = retryCount;

        }

        public bool IsConnection => connection != null && connection.IsOpen; 

        public IModel CreateModel()
        {
            return connection.CreateModel();    
        }


        public void Dispose()
        {
            /*Örneğin, RabbitMQ .NET istemcisi kullanıyorsanız, bağlantıları ve kanalları kapatmak için IDisposable arabirimini uygulayan nesneleri kullanarak "dispose" işlevselliğini kullanabilirsiniz. Bu, RabbitMQ kaynaklarını doğru bir şekilde temizlemenize ve yönetmenize yardımcı olur.*/
            connection?.Dispose();  
            _disposed = true;
        }

        public bool TryConnect()
        { 
            lock(lock_object)
            {
                var policy = Policy.Handle<SocketException>()
                    .Or<BrokerUnreachableException>()
                    .WaitAndRetry(_retryCount,retryAttempt => TimeSpan.FromSeconds(Math.Pow(2,retryAttempt)),(ex,time) =>
                    {
                    }
                );

                policy.Execute(()=>
                {
                    connection = _connectionFactory.CreateConnection();  
                });

                if(IsConnection)
                {
                    //log
                    connection.ConnectionShutdown += Connection_ConnectionShutdown;
                    connection.CallbackException += Connection_CallbackException;
                    connection.ConnectionBlocked += Connection_ConnectionBlocked;
                    return true;
                }
                return false;
            }

        }

        private void Connection_ConnectionBlocked(object? sender, global::RabbitMQ.Client.Events.ConnectionBlockedEventArgs e)
        {
            if(_disposed) return;
            TryConnect();
        }

        private void Connection_CallbackException(object? sender, global::RabbitMQ.Client.Events.CallbackExceptionEventArgs e)
        {
            if (_disposed) return;
            TryConnect();
        }

        private void Connection_ConnectionShutdown(object? sender, ShutdownEventArgs e)
        {
            //log
            if (_disposed) return;
            TryConnect();
        }
    }
}
