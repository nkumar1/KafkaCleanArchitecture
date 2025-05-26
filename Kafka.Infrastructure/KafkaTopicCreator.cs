using Confluent.Kafka;
using Confluent.Kafka.Admin;

namespace Kafka.Infrastructure
{
    public class KafkaTopicCreator
    {
        private readonly string _bootstrapServers;

        public KafkaTopicCreator(string bootstrapServers)
        {
            _bootstrapServers = bootstrapServers;
        }
        
        public async Task CreateTopicIfNotExistsAsync(string topicName, int numPartitions = 3, short replicationFactor = 1)
        {
            using var adminClient = new AdminClientBuilder(new AdminClientConfig
            {
                BootstrapServers = _bootstrapServers
            }).Build();

            try
            {
                await adminClient.CreateTopicsAsync(new[]
                {
                    new TopicSpecification
                    {
                        Name = topicName,
                        NumPartitions = numPartitions,
                        ReplicationFactor = replicationFactor
                    }
                });
                Console.WriteLine($"Topic {topicName} created successfully");
            }
            catch (CreateTopicsException e) when (e.Results[0].Error.Code == ErrorCode.TopicAlreadyExists)
            {
                Console.WriteLine($"Topic {topicName} already exists");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred creating topic {topicName}: {ex.Message}");
                throw;
            }
        }
    }
}
