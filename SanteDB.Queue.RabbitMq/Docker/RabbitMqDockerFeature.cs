/*
 * Copyright (C) 2021 - 2023, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2023-3-10
 */
using SanteDB.Core.Configuration;
using SanteDB.Core.Exceptions;
using SanteDB.Docker.Core;
using SanteDB.Queue.RabbitMq.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SanteDB.Queue.RabbitMq.Docker
{
    public class RabbitMqDockerFeature : IDockerFeature
    {
        /// <summary>
        ///  Name of the device hosting RabbitMQ
        /// </summary>
        public const string Hostname = "hostname";

        /// <summary>
        /// Username for  authenticating to the RabbitMQ server
        /// </summary>
        public const string Username = "username";

        /// <summary>
        /// Password for  authenticating to the RabbitMQ server
        /// </summary>
        public const string Password = "password";

        /// <summary>
        /// Name of RabbitMQ exchange
        /// </summary>
        public const string ExchangeName = "exchangeName";

        /// <summary>
        /// The queue durability setting
        /// </summary>
        public const string QueueDurable = "durable";

        /// <summary>
        /// The message persistence setting
        /// </summary>
        public const string MessagePersistent = "messagePersistence";

        /// <summary>
        /// The lazy queue setting
        /// </summary>
        public const string LazyQueue = "lazy";

        /// <summary>
        /// The setting for limiting number of unacknowledged message
        /// </summary>
        public const string MaxUnackedMessages = "maxUnackedMessageLimit";

        /// <summary>
        /// The virtual host
        /// </summary>
        public const string VirtualHost = "virtualHost";

        /// <summary>
        /// The timeout setting for request to management API
        /// </summary>
        public const string ManagementApiTimeout = "managementApiTimeout";

        /// <summary>
        /// The URI to management API
        /// </summary>
        public const string ManagementUri = "managementUri";

        /// <summary>
        /// Gets the identifier of the feature
        /// </summary>
        /// <remarks>This is the identifier that appears in the <c>SDB_FEATURE</c></remarks>
        public string Id => "RABBIT_MQ";

        /// <summary>
        /// Settings for the RabbitMq queue management feature
        /// </summary>
        /// <remarks>
        /// <list type="table">
        ///     <item><term>Hostname</term><description>Name of host where RabbitMQ resides</description></item>
        ///     <item><term>Username</term><description>Username</description></item>
        ///     <item><term>Password</term><description>Password</description></item>
        ///     <item><term>ExchangeName</term><description>Name of RabbitMq Exchange</description></item>
        ///     <item><term>QueueDurable</term><description>Defines whether queue should be durable</description></item>
        ///     <item><term>MessagePersistent</term><description>Defines whether messages should be marked as persistent</description></item>
        ///     <item><term>LazyQueue</term><description>Defines whether queue should be marked as lazy</description></item>
        ///     <item><term>MaxUnackedMessages</term><description>The Maximum number of unacknowledged messages allowed</description></item>
        ///     <item><term>VirtualHost</term><description>The virtual host</description></item>
        ///     <item><term>ManagementApiTimeout</term><description>Timeout value for when requesting response from RabbitMq Management Api</description></item>
        /// </list>
        /// </remarks>
        public IEnumerable<string> Settings => new String[]
        {
            Hostname, Username, Password, ExchangeName, QueueDurable, MessagePersistent, LazyQueue, MaxUnackedMessages, VirtualHost, ManagementApiTimeout, ManagementUri
        };

        /// <summary>
        /// Configure the feature for execution in Docker
        /// </summary>
        /// <remarks>Allows the feature to be configured against the provided <paramref name="configuration"/></remarks>
        /// <param name="configuration">The configuration into which the feature should be configured</param>
        /// <param name="settings">Settings which were parsed from the environment in SDB_{this.Id}_{Key}={Value}</param>
        public void Configure(SanteDBConfiguration configuration, IDictionary<string, string> settings)
        {
            var hostname = "localhost";
            var username = "guest";
            var password = "guest";
            var durable = "true";
            var exchangeName = "SanteExc01";
            var messagePersistence = "true";
            var lazy = "true";
            var maxUnackedMessageLimit = "1";
            var virtualHost = "/";
            var managementApiTimeout = "500";
            var managementUri = "http://localhost:15672/";

            // The type of service to add
            Type[] serviceTypes = new Type[] { typeof(RabbitMqService) };

            // Try to get value of settings
            if (settings.TryGetValue(Hostname, out var foundHostname))
            {
                hostname = foundHostname;
            }

            if (settings.TryGetValue(Username, out var foundUsername))
            {
                username = foundUsername;
            }

            if (settings.TryGetValue(Username, out var foundPassword))
            {
                password = foundPassword;
            }

            if (settings.TryGetValue(QueueDurable, out var foundDurable))
            {
                durable = foundDurable;
            }

            if (settings.TryGetValue(ExchangeName, out var foundExchangeName))
            {
                exchangeName = foundExchangeName;
            }

            if (settings.TryGetValue(MessagePersistent, out var foundMessagePersistence))
            {
                messagePersistence = foundMessagePersistence;
            }

            if (settings.TryGetValue(LazyQueue, out var foundLazy))
            {
                lazy = foundLazy;
            }

            if (settings.TryGetValue(MaxUnackedMessages, out var foundMaxUnackedMessages))
            {
                maxUnackedMessageLimit = foundMaxUnackedMessages;
            }

            if (settings.TryGetValue(VirtualHost, out var foundVirtualHost))
            {
                virtualHost = foundVirtualHost;
            }

            if (settings.TryGetValue(ManagementApiTimeout, out var foundManagementApiTimeout))
            {
                managementApiTimeout = foundManagementApiTimeout;
            }

            if (settings.TryGetValue(ManagementUri, out var foundManagementURI))
            {
                managementUri = foundManagementURI;
            }

            // Parse to make sure appropriate values were provided
            if (!bool.TryParse(durable, out var validatedDurableSetting))
            {
                throw new ConfigurationException($"{QueueDurable} = {durable} is not understood as a boolean value", configuration);
            }

            if (!bool.TryParse(messagePersistence, out var validatedMessagePersistence))
            {
                throw new ConfigurationException($"{MessagePersistent} = {messagePersistence} is not understood as a boolean value", configuration);
            }

            if (!bool.TryParse(lazy, out var validatedLazySetting))
            {
                throw new ConfigurationException($"{LazyQueue} = {lazy} is not understood as a boolean value", configuration);
            }

            if (!ushort.TryParse(maxUnackedMessageLimit, out var validatedMaxUnackedMessageLimit))
            {
                throw new ConfigurationException($"{MaxUnackedMessages} = {maxUnackedMessageLimit} is not understood as a unsigned short value", configuration);
            }

            if (!int.TryParse(managementApiTimeout, out var validatedManagementApiTimeout))
            {
                throw new ConfigurationException($"{ManagementApiTimeout} = {validatedManagementApiTimeout} is not understood as a integer value", configuration);
            }

            if (!Uri.IsWellFormedUriString(managementUri, UriKind.Absolute))
            {
                throw new ConfigurationException($"{ManagementUri} = {managementUri} is not understood as a well formed Uri value", configuration);
            }

            // RabbitMQ Config
            var rabbitMqConfigurationSection = configuration.GetSection<RabbitMqConfigurationSection>() ?? new RabbitMqConfigurationSection();

            rabbitMqConfigurationSection.Hostname = hostname;
            rabbitMqConfigurationSection.VirtualHost = virtualHost;
            rabbitMqConfigurationSection.Username = username;
            rabbitMqConfigurationSection.Password = password;
            rabbitMqConfigurationSection.ExchangeName = exchangeName;
            rabbitMqConfigurationSection.QueueDurable = validatedDurableSetting;
            rabbitMqConfigurationSection.MaxUnackedMessages = validatedMaxUnackedMessageLimit;
            rabbitMqConfigurationSection.MessagePersistent = validatedMessagePersistence;
            rabbitMqConfigurationSection.LazyQueue = validatedLazySetting;
            rabbitMqConfigurationSection.ManagementApiTimeout = validatedManagementApiTimeout;
            rabbitMqConfigurationSection.ManagementUri = managementUri;

            // Add services
            var serviceConfiguration = configuration.GetSection<ApplicationServiceContextConfigurationSection>().ServiceProviders;
            serviceConfiguration.AddRange(serviceTypes.Where(t => !serviceConfiguration.Any(c => c.Type == t)).Select(t => new TypeReferenceConfiguration(t)));
        }
    }
}