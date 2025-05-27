using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kafka.Infrastructure
{
    public class KafkaOptions
    {
        public string BootstrapServers { get; set; }
        public string GroupId { get; set; }
        public string TopicName { get; set; }
        public bool AllowAutoCreateTopics { get; set; } = false;
    }
}
