﻿/*
 * Copyright (C) 2021 - 2022, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
 * Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors
 * Portions Copyright (C) 2015-2018 Mohawk College of Applied Arts and Technology
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); you 
 * may not use this file except in compliance with the License. You may 
 * obtain a copy of the License at 
 * 
 * http://www.apache.org/licenses/LICENSE-2.0 
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the 
 * License for the specific language governing permissions and limitations under 
 * the License.
 * 
 * User: Shihab Khan
 * Date: 2022-03-17
 */

using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SanteDB.Core.Exceptions;
using SanteDB.Core.Queue;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;
using SanteDB.Queue.RabbitMq.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;

namespace SanteDB.Queue.RabbitMq
{
    public class RabbitMqService : IDispatcherQueueManagerService, IDisposable
    {
        // Configuration
        private readonly RabbitMqConfigurationSection m_configuration;

        //for creating connection
        private  ConnectionFactory m_connectionFactory;

        //connection
        private  IConnection m_connection;

        //channel
        private IModel m_channel;

        //routing key
        private string m_routingKey = "t1";

        // PEP service
        private readonly IPolicyEnforcementService m_pepService;

        // Consumer tag
        private string m_consumerTag;

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
            this.m_consumerTag = this.m_channel.BasicConsume(queue: queueName,
                autoAck: true,
                consumer: consumer);

        }

        /// <summary>
        /// Remove the callback registration
        /// </summary>
        public void UnSubscribe(string queueName, DispatcherQueueCallback callback)
        {
            this.m_channel.BasicCancel(this.m_consumerTag);
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
                    Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data)));
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
            //we can implement a basic get although this is not recommended
            var result = this.m_channel.BasicGet(queueName, autoAck:true);

            //need to find out how this is being used
            return new DispatcherQueueEntry()
            {
                Body = result?.Body,
                CorrelationId = null
            };
        }

        /// <summary>
        /// De-queue a specific message
        /// </summary>
        public DispatcherQueueEntry DequeueById(string queueName, string correlationId)
        {
            //RabbitMQ doesn't support this
            throw new NotSupportedException();
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
            throw new NotSupportedException();
        }

        /// <summary>
        /// Get the specified queue entry
        /// </summary>
        public DispatcherQueueEntry GetQueueEntry(string queueName, string correlationId)
        {
            //not supported in RabbitMQ
            throw new NotSupportedException();
        }

        /// <summary>
        /// Gets the queues for this system
        /// </summary>
        public IEnumerable<DispatcherQueueInfo> GetQueues()
        {
            var clientHandler = new HttpClientHandler { Credentials = this.m_configuration.RabbitMQCredential };
            var client = new HttpClient(clientHandler);
            var response = client.GetAsync($"{this.m_configuration.ManagementUri}api/queues").GetAwaiter().GetResult();
            var json = response.Content.ReadAsStringAsync().Result;
            var deserializedObjects = JsonConvert.DeserializeAnonymousType(json, new[] { new { Name = "", Messages = 0 } });
            return deserializedObjects.Select(r => new DispatcherQueueInfo()
            {
                Name = r.Name,
                QueueSize = r.Messages
            });
            
        }

        /// <summary>
        /// Get all queue entries
        /// </summary>
        public IEnumerable<DispatcherQueueEntry> GetQueueEntries(string queueName)
        {
            //not supported in RabbitMQ
            throw new NotSupportedException();
        }

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        public void Dispose()
        {
            //disposing channels and connection is not enough, they must be closed 
            if (this.m_channel != null)
            {
                this.m_channel.Close();
                this.m_channel.Dispose();
            }
            if (this.m_connection != null)
            {
                this.m_connection.Close();
                this.m_connection.Dispose();
            }
        }
    }
}

//note for channels and connections
//see https://www.cloudamqp.com/blog/part1-rabbitmq-best-practice.html Connections/Channels