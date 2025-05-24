using Confluent.Kafka;
using Domain.Entities;
using Domain.Interfaces;
using System.Text.Json;

namespace Kafka.Worker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IRepository<VehicleLocation> _repository;
        private readonly IConsumer<string, string> _consumer;

        public Worker(ILogger<Worker> logger,
            IRepository<VehicleLocation> repository,
            IConsumer<string, string> consumer
            )
        {
            _logger = logger;
            _repository = repository;
            _consumer = consumer;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Worker started at: {time}", DateTimeOffset.Now);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = _consumer.Consume(stoppingToken);
                    if (consumeResult == null) continue;

                    _logger.LogInformation("Received message at: {time}", DateTimeOffset.Now);

                    var vehicleLocation = JsonSerializer.Deserialize<VehicleLocation>(consumeResult.Message.Value);
                    await _repository.AddAsync(vehicleLocation);
                    await _repository.SaveChangesAsync();

                    _logger.LogInformation("Processed vehicle {VehicleId} location", vehicleLocation.VehicleId);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Worker cancellation requested");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message");
                }
            }

            _consumer.Close();
        }
    }
}
