﻿/*
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
 */
using SanteDB.Core.Configuration;
using SanteDB.Core.Configuration.Features;
using System;
using System.Diagnostics.CodeAnalysis;

namespace SanteDB.Queue.RabbitMq.Configuration
{
    /// <summary>
    /// Configuration for using the <see cref="RabbitMqService"/>.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class RabbitMqFeature : GenericServiceFeature<RabbitMqService>
    {
        /// <summary>
        /// Gets the name of the queue
        /// </summary>
        public override string Name => "RabbitMQ";

        /// <summary>
        /// Gets the group 
        /// </summary>
        public override string Group => FeatureGroup.System;

        /// <summary>
        /// Gets the configuration service
        /// </summary>
        public override Type ConfigurationType => typeof(RabbitMqConfigurationSection);

        /// <summary>
        /// Get the default configuration
        /// </summary>
        protected override object GetDefaultConfiguration() => new RabbitMqConfigurationSection()
        {
            Hostname = "localhost",
            Username = "guest",
            Password = "guest",
            ExchangeName = "SanteExc01",
            QueueDurable = true,
            MessagePersistent = true,
            LazyQueue = true,
            MaxUnackedMessages = 1,
            VirtualHost = "/",
            ManagementApiTimeout = 500,
            ManagementUri = "http://localhost:15672/"
        };
    }
}
