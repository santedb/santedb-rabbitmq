using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SanteDB.Core.Exceptions;
using SanteDB.Core.Queue;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;
using SanteDB.Queue.RabbitMq.Configuration;

namespace SanteDB.Queue.RabbitMq
{
    public class RabbitMQService : IDispatcherQueueManagerService, IDisposable
    {
        // Configuration
        private readonly RabbitMQConfigurationSection m_configuration;

        //need to use this to make request to management interface 
        private readonly HttpClient client = new HttpClient();

        //for creating connection
        private  ConnectionFactory m_connectionFactory;
        //connection
        private  IConnection m_connection;
        //channel
        private IModel m_channel;

        private string m_routingKey = "t1";

        // PEP service
        private readonly IPolicyEnforcementService m_pepService;

        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => "RabbitMQ Exchange";



        /// <summary>
        /// Opens the specified queue name and enables subscriptions
        /// </summary>
        public void Open(string queueName)
        {   
            this.SetUp();
            //set up queue
            this.m_channel.QueueDeclare(
                queue: queueName, durable: this.m_configuration.QueueDurable,
                exclusive: false, autoDelete: false);
        }

        /// <summary>
        /// Sets up connection and channels
        /// </summary>
        private void SetUp()
        {
            //set up connection, exchange and channel
            this.m_connectionFactory = new ConnectionFactory() { HostName = this.m_configuration.Hostname };
            this.m_connection = this.m_connectionFactory.CreateConnection();
            this.m_channel = this.m_connection.CreateModel();
            this.m_channel.ExchangeDeclare(exchange: this.m_configuration.ExchangeName,
                type: "direct");
        }

        /// <summary>
        /// Subscribes to <paramref name="queueName"/> using <paramref name="callback"/>
        /// </summary>
        public void SubscribeTo(string queueName, DispatcherQueueCallback callback)
        {
            //subscribe to queues here
            //note: as soon as this happens, this consumer will start getting messages.
            this.m_channel.QueueBind(queue: queueName,
                exchange: this.m_configuration.ExchangeName, routingKey: this.m_routingKey);

            //establish consumer
            var consumer = new EventingBasicConsumer(this.m_channel);

            //for testing only - remove later
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var routingKey = ea.RoutingKey;
                Console.WriteLine(" [x] Received '{0}':'{1}'",
                    routingKey, message);

            };
            this.m_channel.BasicConsume(queue: queueName,
                autoAck: true,
                consumer: consumer);

        }

        /// <summary>
        /// Remove the callback registration
        /// </summary>
        public void UnSubscribe(string queueName, DispatcherQueueCallback callback)
        {
            //possibly could use basic cancel for this 
            //need to set up a consumer to be able to handle cancel
            throw new NotImplementedException();

        }

        /// <summary>
        /// Enqueue the specified data to the persistent queue
        /// </summary>
        public void Enqueue(string queueName, object data)
        {   
            //if queue doesn't exist, it will get created
            this.m_channel.QueueDeclare(
                queue: queueName, durable: this.m_configuration.QueueDurable,
                exclusive: false, autoDelete: false);

            try
            {
                this.m_channel.BasicPublish(exchange: this.m_configuration.ExchangeName,
                    routingKey: "t1",
                    basicProperties: null,
                    null);
                //body: JsonSerializer(data));
            }
            catch (Exception ex)
            {
                throw new DataPersistenceException($"Error enqueueing message to {queueName}", ex);

            }

        }

        /// <summary>
        /// Dequeues the last added item from the persistent queue
        /// </summary>
        public DispatcherQueueEntry Dequeue(string queueName)
        {
            //assuming fifo is ok here
            //we can implement a basic get although this is not recommended at all as per rabbitmq docs

            var result = this.m_channel.BasicGet(queueName, autoAck:true);

            //need to find out how this is being used
            return new DispatcherQueueEntry()
            {
                Body = result.Body,
                //probably wrong
                CorrelationId = result.DeliveryTag.ToString()
            };

        }

        /// <summary>
        /// De-queue a specific message
        /// </summary>
        public DispatcherQueueEntry DequeueById(string queueName, string correlationId)
        {
            //RabbitMQ doesn't support this
            throw new NotImplementedException();
        }

        /// <summary>
        /// Purge the queue
        /// </summary>
        public void Purge(string queueName)
        {
            this.m_pepService.Demand(PermissionPolicyIdentifiers.ManageDispatcherQueues);
            this.m_channel.QueueDelete(queueName);
        }

        /// <summary>
        /// Move an entry from one queue to another
        /// </summary>
        public DispatcherQueueEntry Move(DispatcherQueueEntry entry, string toQueue)
        {
            //RabbitMQ doesn't support this
            //could look into doing this in hacky way later and find out a way to mark it as unsafe
            throw new NotImplementedException();

        }

        /// <summary>
        /// Get the specified queue entry
        /// </summary>
        public DispatcherQueueEntry GetQueueEntry(string queueName, string correlationId)
        {
            //no such thing in rabbit mq
            throw new NotSupportedException();
        }

        /// <summary>
        /// Gets the queues for this system
        /// </summary>
        public IEnumerable<DispatcherQueueInfo> GetQueues()
        {
            //need to make http request to management api

            return null;

        }

        /// <summary>
        /// Get all queue entries
        /// </summary>
        public IEnumerable<DispatcherQueueEntry> GetQueueEntries(string queueName)
        {
            throw new NotImplementedException();
        }

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        public void Dispose()
        {
            //this should dispose all virtual channels within it as well
            //need to investigate this further
            this.m_connection.Dispose();
        }

    }
}

//note for channels and connections
//see https://www.cloudamqp.com/blog/part1-rabbitmq-best-practice.html Connections/Channels