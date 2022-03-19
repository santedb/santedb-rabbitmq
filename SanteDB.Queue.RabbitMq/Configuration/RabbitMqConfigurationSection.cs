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

using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Xml.Serialization;
using SanteDB.Core.Configuration;

namespace SanteDB.Queue.RabbitMq.Configuration
{
    /// <summary>
    /// RabbitMQ Exchange Configuration
    /// </summary>
    [XmlType(nameof(RabbitMqConfigurationSection), Namespace = "http://santedb.org/configuration")]
    [ExcludeFromCodeCoverage]
    public class RabbitMqConfigurationSection : IConfigurationSection
    {
        /// <summary>
        /// Host to connect to 
        /// </summary>
        public string Hostname { get; set; }

        /// <summary>
        /// Exchange name
        /// </summary>
        public string ExchangeName { get; set; }

        /// <summary>
        /// Enables Queues to persist and survive exchange server restart
        /// </summary>
        public bool QueueDurable { get; set; }

        /// <summary>
        /// Enables persistence of messages
        /// </summary>
        public bool MessagePersistent { get; set; }

        /// <summary>
        /// Enables Lazy Queue and moves content of queue to disk as soon as practically possible
        /// </summary>
        public bool LazyQueue { get; set; }

        /// <summary>
        /// Sets maximum number of messages per queue
        /// </summary>
        public int MaxMessagesPerQueue { get; set; }

        /// <summary>
        /// Sets maximum number of queues 
        /// </summary>
        public int MaxQueues { get; set; }

        //RabbitMQ server network credentials
        public NetworkCredential RabbitMQCredential { get; set; }

        //RabbitMQ Management URI
        public Uri ManagementUri { get; set; }

    }
}
