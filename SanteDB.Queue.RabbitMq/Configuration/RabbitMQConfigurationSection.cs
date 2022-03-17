using System.Diagnostics.CodeAnalysis;
using System.Xml.Serialization;
using SanteDB.Core.Configuration;

namespace SanteDB.Queue.RabbitMq.Configuration
{
    /// <summary>
    /// RabbitMQ Exchange Configuration
    /// </summary>
    [XmlType(nameof(RabbitMQConfigurationSection), Namespace = "http://santedb.org/configuration")]
    [ExcludeFromCodeCoverage]
    public class RabbitMQConfigurationSection : IConfigurationSection
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

        
    }
}
