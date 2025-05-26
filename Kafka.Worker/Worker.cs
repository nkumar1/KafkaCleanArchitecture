using Confluent.Kafka;
using Domain.Entities;
using Domain.Interfaces;
using System.Text.Json;

namespace Kafka.Worker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;

        public Worker(ILogger<Worker> logger, IServiceScopeFactory scopeFactory, IConfiguration configuration)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Kafka Worker started at: {time}", DateTimeOffset.Now);
            var topicName = _configuration["Kafka:TopicName"];

            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var repository = scope.ServiceProvider.GetRequiredService<IRepository<VehicleLocation>>();

                    var consumer = scope.ServiceProvider.GetRequiredService<IConsumer<string, string>>();

                    try
                    {
                        consumer.Subscribe(topicName); //Subscribe Topic

                        var consumeResult = consumer.Consume(stoppingToken);
                        if (consumeResult == null) continue;

                        var vehicleLocation = JsonSerializer.Deserialize<VehicleLocation>(consumeResult.Message.Value);
                        await repository.AddAsync(vehicleLocation);
                        await repository.SaveChangesAsync();

                        // ✅ Commit offset manually, Avoid triggering re-consumption
                        consumer.StoreOffset(consumeResult);
                        consumer.Commit(consumeResult);

                        _logger.LogInformation("Processed vehicle {VehicleId}", vehicleLocation.VehicleId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing message");
                    }
                }

                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
