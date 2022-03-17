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
using SanteDB.Core.Configuration;
using SanteDB.Core.Configuration.Features;

namespace SanteDB.Queue.RabbitMq.Configuration
{
    [ExcludeFromCodeCoverage]
    public class RabbitMqFeature : GenericServiceFeature<RabbitMqService>
    {
        /// <summary>
        /// Gets the name of the queue
        /// </summary>
        public override string Name => "RabbmitMQ1";
        
        /// <summary>
        /// Gets the group 
        /// </summary>
        public override string Group => FeatureGroup.System;

        /// <summary>
        /// Gets the configuration service
        /// </summary>
        public override Type ConfigurationType => typeof(RabbitMqConfigurationSection2);

        /// <summary>
        /// Get the default configuration
        /// </summary>
        protected override object GetDefaultConfiguration()
        {
            return new RabbitMqConfigurationSection2()
            {
                Hostname = "localhost",
                ExchangeName = "TestExchange",
                QueueDurable = true,
                MessagePersistent = true,
                LazyQueue = false,
                MaxMessagesPerQueue = 50_000,
                MaxQueues = 10
            };
        }
    }
}
