using Confluent.Kafka;
using Infrastructure.DependencyInjection;
using Infrastructure.Persistence;
using Kafka.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Kafka.Worker
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();

            // Initialize database
            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var context = services.GetRequiredService<AppDbContext>();
                    await context.Database.MigrateAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred while migrating the database: {ex.Message}");
                }
            }

            // Initialize Kafka topic
            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var topicCreator = services.GetRequiredService<KafkaTopicCreator>();
                    await topicCreator.CreateTopicIfNotExistsAsync("vehicle-locations");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred while creating Kafka topic: {ex.Message}");
                }
            }

            await host.RunAsync();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddInfrastructure(hostContext.Configuration);

                    // Kafka consumer configuration
                    services.AddSingleton<IConsumer<string, string>>(sp =>
                    {
                        var config = new ConsumerConfig
                        {
                            BootstrapServers = hostContext.Configuration["Kafka:BootstrapServers"],
                            GroupId = hostContext.Configuration["Kafka:GroupId"],
                            AutoOffsetReset = AutoOffsetReset.Earliest,
                            EnableAutoCommit = false,
                            EnableAutoOffsetStore = false
                        };
                        return new ConsumerBuilder<string, string>(config).Build();
                    });

                    services.AddSingleton<KafkaTopicCreator>(sp =>
                        new KafkaTopicCreator(hostContext.Configuration["Kafka:BootstrapServers"]));

                    services.AddHostedService<Worker>();
                })
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                    config.AddEnvironmentVariables();
                });
    }
}