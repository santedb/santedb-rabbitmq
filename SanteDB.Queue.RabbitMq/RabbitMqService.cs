/*
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Model.Serialization;
using SanteDB.Core.Services;

namespace SanteDB.Queue.RabbitMq
{
    /// <summary>
    /// An implementation of the <see cref="IDispatcherQueueManagerService"/> which uses RabbitMQ as the queue managing service
    /// </summary>
    /// <remarks>
    /// <para>This queue service uses RabbitMQ to manage queues and loading/unloading of items to queues.</para>
    /// </remarks>
    public class RabbitMqService : IDispatcherQueueManagerService, IDisposable
    {
        // MSMQ Persistence queue service
        private Tracer m_tracer = Tracer.GetTracer(typeof(RabbitMqService));

        // Configuration
        private readonly RabbitMqConfigurationSection m_configuration;

        //for creating connection
        private ConnectionFactory m_connectionFactory;

        //connection
        private IConnection m_connection;

        //channel
        private IModel m_channel;

        // PEP service
        private readonly IPolicyEnforcementService m_pepService;

        // Consumer tag
        private string m_consumerTag;

        //delivery tag
        private ulong deliveryTag;

        //received messages
        private readonly ConcurrentDictionary<ulong, DispatcherQueueEntry> receivedMessages = new ConcurrentDictionary<ulong, DispatcherQueueEntry>();

        //http client
        private static readonly HttpClient client = new HttpClient();

        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => "RabbitMQ Exchange";


        /// <summary>
        /// DI constructor for RabbitMQ
        /// </summary>
        public RabbitMqService(IConfigurationManager configurationManager, IPolicyEnforcementService pepService)
        {
            this.m_configuration = configurationManager.GetSection<RabbitMqConfigurationSection>();
            this.m_pepService = pepService;
        }

        /// <summary>
        /// Opens the specified queue name and enables subscriptions
        /// </summary>
        public void Open(string queueName)
        {
            if (this.m_channel == null)
            {
                this.SetUp();
            }

            this.OpenQueue(queueName);
        }

        /// <summary>
        /// Sets up connection, exchange and channels
        /// </summary>
        private void SetUp()
        {
            //set up connection, exchange and channel
            this.m_connectionFactory = new ConnectionFactory()
            {
                HostName = this.m_configuration.Hostname, 
                VirtualHost = this.m_configuration.VirtualHost,
                UserName = this.m_configuration.Username,
                Password = this.m_configuration.Password
            };
            this.m_connection = this.m_connectionFactory.CreateConnection();
            this.m_channel = this.m_connection.CreateModel();
            this.m_channel.ExchangeDeclare(this.m_configuration.ExchangeName, "direct");
            //set up prefetch count (max number of unacknowledged message per consumer)
            this.m_channel.BasicQos(0, this.m_configuration.MaxUnackedMessages, false);
        }

        /// <summary>
        /// Sets up a queue
        /// </summary>
        /// <param name="queueName"></param>
        private void OpenQueue(string queueName)
        {
            Dictionary<string, object> args = this.m_configuration.LazyQueue ? new Dictionary<string, object>(){{"x-queue-mode", "lazy"}} : null;
            //creates queue if it doesn't exist
            try
            {
                this.m_channel.QueueDeclare(queueName, this.m_configuration.QueueDurable, false, false, args);
            }
            catch (Exception e)
            {
                throw new Exception($"Error opening queue {queueName}", e);
            }
            
        }

        /// <summary>
        /// Subscribes to <paramref name="queueName"/> using <paramref name="callback"/>
        /// </summary>
        public void SubscribeTo(string queueName, DispatcherQueueCallback callback)
        {
            //subscribe to queues here
            //note: as soon as this happens, this consumer will start getting messages if any, since this is a "push" API
            this.m_channel.QueueBind(queueName, this.m_configuration.ExchangeName, queueName);

            //establish consumer
            var consumer = new EventingBasicConsumer(this.m_channel);

            //on receive 
            consumer.Received += (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    using (var ms = new MemoryStream(body))
                    {
                        var type = Type.GetType(ea.BasicProperties.ContentType);
                        var xmlSerializer = XmlModelSerializerFactory.Current.CreateSerializer(type);
                        //add received message to thread safe dictionary where the key is the delivery tag
                        //the delivery tag will be needed in dequeue to acknowledge the message 
                        this.receivedMessages.TryAdd(ea.DeliveryTag, new DispatcherQueueEntry(null, queueName, DateTime.Now, ea.BasicProperties.ContentType, xmlSerializer.Deserialize(ms)));
                    }
                    this.deliveryTag = ea.DeliveryTag;
                    callback(new DispatcherMessageEnqueuedInfo(queueName, null));
                }
                catch (Exception ex)
                {
                    this.m_tracer.TraceError("Error performing callback - {0}", ex);
                }
            };

            this.m_consumerTag = this.m_channel.BasicConsume(queueName, false, consumer);
        }

        /// <summary>
        /// Unsubscribes the consumer
        /// </summary>
        public void UnSubscribe(string queueName, DispatcherQueueCallback callback)
        {
            //this may still cause some messages to flow through if in progress
            try
            {
                this.m_channel.BasicCancel(this.m_consumerTag);
            }
            catch (Exception e)
            {
                this.m_tracer.TraceError($"Error unsubscribing from {queueName}", e);
            }
        
        }

        /// <summary>
        /// Enqueue the specified data to the persistent queue
        /// </summary>
        public void Enqueue(string queueName, object data)
        {
            this.Open(queueName);
            try
            {
                using (var ms = new MemoryStream())
                {
                    //additional props
                    var props = this.m_channel.CreateBasicProperties();
                    props.Persistent = this.m_configuration.MessagePersistent; //for persistent settings
                    props.ContentType = data.GetType().AssemblyQualifiedName;
                    XmlModelSerializerFactory.Current.CreateSerializer(data.GetType()).Serialize(ms, data);

                    //publish message as byte array
                    this.m_channel.BasicPublish(this.m_configuration.ExchangeName, queueName, props, ms.GetBuffer());
                }
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
            try
            {
                //take an item out of the dictionary
                if (this.receivedMessages.TryRemove(this.deliveryTag, out var queueEntry))
                {
                    //acknowledge so that the message gets deleted from queue
                    this.m_channel.BasicAck(this.deliveryTag, false);
                    return queueEntry;
                }
                return null;
            }
            catch (Exception e)
            {
                throw new DataPersistenceException($"Error de-queueing message from {queueName}", e);
            }

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
            try
            {
                this.m_pepService.Demand(PermissionPolicyIdentifiers.ManageDispatcherQueues);
                this.m_channel.QueueDelete(queueName);
            }
            catch (Exception e)
            {
                this.m_tracer.TraceError($"Error purging queue {queueName}", e);
            }
            
        }

        /// <summary>
        /// Move an entry from one queue to another
        /// </summary>
        public DispatcherQueueEntry Move(DispatcherQueueEntry entry, string toQueue)
        {
            this.Enqueue(toQueue, entry.Body);
            return entry;
        }

        /// <summary>
        /// Get the specified queue entry
        /// </summary>
        public DispatcherQueueEntry GetQueueEntry(string queueName, string correlationId)
        {
            throw new NotSupportedException("This operation is not supported in RabbitMQ");
        }

        /// <summary>
        /// Gets the queues for this system
        /// </summary>
        public IEnumerable<DispatcherQueueInfo> GetQueues()
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{this.m_configuration.Username}:{this.m_configuration.Password}")));
            
            //request all queues from management api
            var response = new HttpResponseMessage();
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(this.m_configuration.ManagementApiTimeout); 
            using (var requestTask = Task.Run(async () => { return await client.GetAsync($"{this.m_configuration.ManagementUri}api/queues"); }, cancellationTokenSource.Token))
            {
                try
                {
                    response = requestTask.Result;
                }
                catch (AggregateException e)
                {
                    client.CancelPendingRequests();
                    this.m_tracer.TraceError($"Error connecting to {this.m_configuration.ManagementUri}api/queues", e);
                }
                catch (TaskCanceledException e)
                {
                    client.CancelPendingRequests();
                    this.m_tracer.TraceError($"Task was cancelled while connecting to {this.m_configuration.ManagementUri}api/queues", e);
                }
            }

            //read response content
            cancellationTokenSource = new CancellationTokenSource();
            string json = String.Empty;
            cancellationTokenSource.CancelAfter(1000);
            using (var requestTask = Task.Run(async () => { return await response.Content.ReadAsStringAsync(); }, cancellationTokenSource.Token))
            {
                try
                {
                    json = requestTask.Result;
                }
                catch (AggregateException e)
                {
                   this.m_tracer.TraceError($"Error reading json content {json}", e);
                }
            }

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
            throw new NotSupportedException("This operation is not supported in RabbitMQ");
        }

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        public void Dispose()
        {
            //disposing channels and connection is not enough, they must be closed 
            this.m_channel?.Close();
            this.m_channel?.Dispose();

            this.m_connection?.Close();
            this.m_connection?.Dispose();

        }
    }
}
