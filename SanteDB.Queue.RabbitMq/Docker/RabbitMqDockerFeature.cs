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
 * Date: 2022-05-16
 */

using SanteDB.Docker.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using SanteDB.Core.Configuration;
using SanteDB.Core.Exceptions;
using SanteDB.Queue.RabbitMq.Configuration;

namespace SanteDB.Queue.RabbitMq.Docker
{
    public class RabbitMqDockerFeature : IDockerFeature
    {
        public const string Hostname = "hostname";
        public const string Username = "username";
        public const string Password = "password";
        public const string ExchangeName = "exchangeName";
        public const string QueueDurable = "durable";
        public const string MessagePersistent = "messagePersistence";
        public const string LazyQueue = "lazy";
        public const string MaxUnackedMessages = "maxUnackedMessageLimit";
        public const string VirtualHost = "virtualHost";
        public const string ManagementApiTimeout = "mgmtApiTimeout";

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
        ///     <item><term>Password</term><description>Passwprd</description></item>
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
            Hostname, Username, Password, ExchangeName, QueueDurable, MessagePersistent, LazyQueue, MaxUnackedMessages, VirtualHost, ManagementApiTimeout
        };

        /// <summary>
        /// Configure the feature for execution in Docker
        /// </summary>
        /// <remarks>Allows the feature to be configured against the provided <paramref name="configuration"/></remarks>
        /// <param name="configuration">The configuration into which the feature should be configured</param>
        /// <param name="settings">Settings which were parsed from the environment in SDB_{this.Id}_{Key}={Value}</param>
        public void Configure(SanteDBConfiguration configuration, IDictionary<string, string> settings)
        {
            // The type of service to add
            Type[] serviceTypes = new Type[] {
                typeof(RabbitMqService),
            };

            // Try to get value of settings
            if (!settings.TryGetValue(Hostname, out string hostname))
            {
                hostname = "localhost";
            }

            if (!settings.TryGetValue(Username, out string username))
            {
                username = "admin";
            }

            if (!settings.TryGetValue(Password, out string password))
            {
                password = "admin";
            }

            if (!settings.TryGetValue(QueueDurable, out string durable))
            {
                durable = "true";
            }

            if (!settings.TryGetValue(ExchangeName, out string exchangeName))
            {
                exchangeName = "SanteExc01";
            }

            if (!settings.TryGetValue(MessagePersistent, out string messagePersistence))
            {
                messagePersistence = "true";
            }

            if (!settings.TryGetValue(LazyQueue, out string lazy))
            {
                lazy = "false";
            }

            if (!settings.TryGetValue(MaxUnackedMessages, out string maxUnackedMessageLimit))
            {
                maxUnackedMessageLimit = "1";
            }

            if (!settings.TryGetValue(VirtualHost, out string virtualHost))
            {
                virtualHost = "/";
            }

            if (!settings.TryGetValue(ManagementApiTimeout, out string mgmtApiTimeout))
            {
                mgmtApiTimeout = "500";
            }

            // Parse to make sure appropriate values were provided
            if(!Boolean.TryParse(durable, out bool validatedDurableSetting))
            {
                throw new ConfigurationException($"durable = {durable} is not understood as a boolean value", configuration);
            }
            if(!Boolean.TryParse(messagePersistence, out bool validatedMessagePersistence))
            {
                throw new ConfigurationException($"messagePersistence = {messagePersistence} is not understood as a boolean value", configuration);
            }
            if(!Boolean.TryParse(lazy, out bool validatedLazySetting))
            {
                throw new ConfigurationException($"lazy = {lazy} is not understood as a boolean value", configuration);
            }
            if(!ushort.TryParse(maxUnackedMessageLimit, out ushort validatedMaxUnackedMessageLimit))
            {
                throw new ConfigurationException($"maxUnackedMessageLimit = {maxUnackedMessageLimit} is not understood as a unsigned short value", configuration);
            }
            if(!int.TryParse(mgmtApiTimeout, out int validatedMgmtApiTimeout))
            {
                throw new ConfigurationException($"mgmtApiTimeout = {validatedMgmtApiTimeout} is not understood as a integer value", configuration);
            }

            // Rabbitmq Config
            var rabbitMqSetting = configuration.GetSection<RabbitMqConfigurationSection>();

            if (rabbitMqSetting == null)
            {
                rabbitMqSetting = new RabbitMqConfigurationSection()
                {
                };
                configuration.AddSection(rabbitMqSetting);
            }

            rabbitMqSetting.Hostname = hostname;
            rabbitMqSetting.VirtualHost = virtualHost;
            rabbitMqSetting.Username = username;
            rabbitMqSetting.Password = password;
            rabbitMqSetting.ExchangeName = exchangeName;
            rabbitMqSetting.QueueDurable = validatedDurableSetting;
            rabbitMqSetting.MaxUnackedMessages = validatedMaxUnackedMessageLimit;
            rabbitMqSetting.MessagePersistent = validatedMessagePersistence;
            rabbitMqSetting.LazyQueue = validatedLazySetting;
            rabbitMqSetting.ManagementApiTimeout = validatedMgmtApiTimeout;
            
            // Add services
            var serviceConfiguration = configuration.GetSection<ApplicationServiceContextConfigurationSection>().ServiceProviders;
            serviceConfiguration.AddRange(serviceTypes.Where(t => !serviceConfiguration.Any(c => c.Type == t)).Select(t => new TypeReferenceConfiguration(t)));
        }
    }
}