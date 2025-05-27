using Confluent.Kafka;
using Domain.Entities;
using Domain.Interfaces;
using Kafka.Infrastructure;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Kafka.Worker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly KafkaOptions _kafkaOptions;

        public Worker(ILogger<Worker> logger, IServiceScopeFactory scopeFactory, IOptions<KafkaOptions> kafkaOptions)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _kafkaOptions = kafkaOptions.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Kafka Worker started at: {time}", DateTimeOffset.Now);
            var topicName = _kafkaOptions.TopicName;

            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<VehicleLocation>>();

            //Reuse the same consumer for the whole service lifetime
            var consumer = scope.ServiceProvider.GetRequiredService<IConsumer<string, string>>();
            consumer.Subscribe(topicName); //Subscribe ONCE, outside loop

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var consumeResult = consumer.Consume(stoppingToken);
                        if (consumeResult == null || consumeResult.Message == null) continue;

                        var vehicleLocation = JsonSerializer.Deserialize<VehicleLocation>(consumeResult.Message.Value);

                        _logger.LogInformation("Received message from Partition: {Partition}, Offset: {Offset}",
                           consumeResult.Partition.Value,
                           consumeResult.Offset.Value);

                        // Check if record already exists to prevent duplicate insert
                        var exists = await repository.ExistsAsync(vehicleLocation.VehicleId, vehicleLocation.Timestamp);

                        if (!exists)
                        {
                            await repository.AddAsync(vehicleLocation);
                            await repository.SaveChangesAsync();

                            _logger.LogInformation("Inserted vehicle {VehicleId} at {Timestamp}", vehicleLocation.VehicleId, vehicleLocation.Timestamp);
                        }
                        else
                        {
                            _logger.LogInformation("Skipped duplicate vehicle {VehicleId} at {Timestamp}", vehicleLocation.VehicleId, vehicleLocation.Timestamp);
                        }

                        consumer.StoreOffset(consumeResult);
                        consumer.Commit(consumeResult);

                        _logger.LogInformation("Processed vehicle {VehicleId}", vehicleLocation.VehicleId);
                    }
                    catch (ConsumeException cex)
                    {
                        _logger.LogError(cex, "Kafka consume error");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing message");
                    }

                    await Task.Delay(200, stoppingToken); // Reduce tight loop CPU pressure
                }
            }
            finally
            {
                consumer.Close(); // Important for clean shutdown and offset commit
            }
        }


        //protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        //{
        //    _logger.LogInformation("Kafka Worker started at: {time}", DateTimeOffset.Now);
        //    var topicName = _kafkaOptions.TopicName; //_configuration["Kafka:TopicName"];

        //    while (!stoppingToken.IsCancellationRequested)
        //    {
        //        using (var scope = _scopeFactory.CreateScope())
        //        {
        //            var repository = scope.ServiceProvider.GetRequiredService<IRepository<VehicleLocation>>();

        //            var consumer = scope.ServiceProvider.GetRequiredService<IConsumer<string, string>>();

        //            try
        //            {
        //                consumer.Subscribe(topicName); //Subscribe Topic

        //                var consumeResult = consumer.Consume(stoppingToken);
        //                if (consumeResult == null) continue;

        //                if (consumeResult.Message == null) continue;


        //                var vehicleLocation = JsonSerializer.Deserialize<VehicleLocation>(consumeResult.Message.Value);
        //                await repository.AddAsync(vehicleLocation);
        //                await repository.SaveChangesAsync();

        //                // ✅ Commit offset manually, Avoid triggering re-consumption
        //                consumer.StoreOffset(consumeResult);
        //                consumer.Commit(consumeResult);

        //                _logger.LogInformation("Processed vehicle {VehicleId}", vehicleLocation.VehicleId);
        //            }
        //            catch (Exception ex)
        //            {
        //                _logger.LogError(ex, "Error processing message");
        //            }
        //        }

        //        await Task.Delay(1000, stoppingToken);
        //    }
        //}

    }
}
