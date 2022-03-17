using System;
using System.Diagnostics.CodeAnalysis;
using SanteDB.Core.Configuration;
using SanteDB.Core.Configuration.Features;

namespace SanteDB.Queue.RabbitMq.Configuration
{
    [ExcludeFromCodeCoverage]
    public class RabbitMQFeature : GenericServiceFeature<RabbitMQService>
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
        public override Type ConfigurationType => typeof(RabbitMQConfigurationSection);

        /// <summary>
        /// Get the default configuration
        /// </summary>
        protected override object GetDefaultConfiguration()
        {
            return new RabbitMQConfigurationSection()
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
