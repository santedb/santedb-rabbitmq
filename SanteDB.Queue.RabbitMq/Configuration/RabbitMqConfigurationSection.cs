/*
 * Copyright (C) 2021 - 2024, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * User: fyfej
 * Date: 2023-6-21
 */
using SanteDB.Core.Configuration;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Serialization;

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
        [XmlAttribute("hostname")]
        [DisplayName("Host Name")]
        [Description("Name of host where RabbitMQ resides")]
        public string Hostname { get; set; } = "localhost";

        /// <summary>
        /// Exchange name
        /// </summary>
        [XmlAttribute("exchangeName")]
        [DisplayName("Exchange Name")]
        [Description("Name of Exchange")]
        public string ExchangeName { get; set; } = "SanteExc01";


        /// <summary>
        /// Virtual host
        /// </summary>
        [XmlAttribute("virtualHost")]
        [DisplayName("virtualHost")]
        [Description("Virtual Host")]
        public string VirtualHost { get; set; } = "/";

        /// <summary>
        /// Enables Queues to persist and survive exchange server restart
        /// </summary>
        [XmlAttribute("durable")]
        [DisplayName("Queue Durable")]
        [Description("Sets Queue Durability")]
        public bool QueueDurable { get; set; } = true;

        /// <summary>
        /// Enables persistence of messages
        /// </summary>
        [XmlAttribute("messagePersistence")]
        [DisplayName("Message Persistent")]
        [Description("Sets message persistence")]
        public bool MessagePersistent { get; set; } = true;

        /// <summary>
        /// Enables Lazy Queue and moves content of queue to disk as soon as practically possible
        /// </summary>
        [XmlAttribute("lazy")]
        [DisplayName("Lazy Queue")]
        [Description("Sets the lazy queue setting")]
        public bool LazyQueue { get; set; } = true;

        /// <summary>
        /// Sets maximum number of unacknowledged messages per channel
        /// </summary>
        [XmlAttribute("maxUnackedMessageLimit")]
        [DisplayName("Max Unacked Messages")]
        [Description("The maximum number of unacknowledged messsages")]
        public ushort MaxUnackedMessages { get; set; } = 1;

        /// <summary>
        /// RabbitMQ server network username
        /// </summary>
        [XmlAttribute("username")]
        public string Username { get; set; } = "guest";

        /// <summary>
        /// RabbitMQ server network password
        /// </summary>
        [XmlAttribute("password")]
        public string Password { get; set; } = "guest";

        /// <summary>
        /// RabbitMQ Management URI
        /// </summary>
        [XmlAttribute("managementUri")]
        [DisplayName("Management Uri")]
        [Description("Management API Uri")]
        public string ManagementUri { get; set; } = "http://localhost:15672/";

        /// <summary>
        /// Sets timeout value in  milliseconds for when requesting response from management api
        /// </summary>
        [XmlAttribute("managementApiTimeout")]
        [DisplayName("Management Api Timeout")]
        [Description("Timeout value in milliseconds for when requesting response from management api")]
        public int ManagementApiTimeout { get; set; } = 500;
    }
}
