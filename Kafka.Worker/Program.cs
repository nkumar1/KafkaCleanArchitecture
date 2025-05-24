using Confluent.Kafka;
using Domain.Interfaces;
using Infrastructure.DependencyInjection;
using Infrastructure.Persistence;
using Kafka.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
                var logger = services.GetRequiredService<ILogger<Program>>();

                try
                {
                    var context = services.GetRequiredService<AppDbContext>();
                    logger.LogInformation("Applying database migrations...");
                    await context.Database.MigrateAsync();
                    logger.LogInformation("Migrations applied successfully");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "An error occurred while migrating the database");
                    throw; // Rethrow to fail fast if migrations fail
                }
            }

            // Initialize Kafka topic
            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var logger = services.GetRequiredService<ILogger<Program>>();

                try
                {
                    //IServiceProvider : This is used to resolve services from the DI container
                    //GetRequiredService: This method retrieves a service of the specified type from the DI container,
                                         //throwing an exception if the service is not registered.

                    var topicCreator = services.GetRequiredService<KafkaTopicCreator>();
                    logger.LogInformation("Creating Kafka topic if not exists...");
                    await topicCreator.CreateTopicIfNotExistsAsync("vehicle-locations");
                    logger.LogInformation("Kafka topic verified/created");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "An error occurred while creating Kafka topic");
                    throw; // Rethrow to fail fast if topic creation fails
                }
            }

            await host.RunAsync();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    // Configuration
                    services.AddInfrastructure(hostContext.Configuration);

                    // Kafka Consumer (Transient - new instance each time)
                    services.AddTransient<IConsumer<string, string>>(sp =>
                    {
                        var config = new ConsumerConfig
                        {
                            BootstrapServers = hostContext.Configuration["Kafka:BootstrapServers"],
                            GroupId = hostContext.Configuration["Kafka:GroupId"],
                            AutoOffsetReset = AutoOffsetReset.Earliest,
                            EnableAutoCommit = false,
                            EnableAutoOffsetStore = false,
                            AllowAutoCreateTopics = true // Important for local development
                        };
                        return new ConsumerBuilder<string, string>(config).Build();
                    });

                    // Kafka Topic Creator (Singleton - can be reused)
                    services.AddSingleton<KafkaTopicCreator>(sp =>
                        new KafkaTopicCreator(hostContext.Configuration["Kafka:BootstrapServers"]));

                    // Worker Service (Scoped)
                    services.AddScoped<IHostedService, Worker>();

                    // Add health checks if needed
                    services.AddHealthChecks()
                        .AddDbContextCheck<AppDbContext>()
                        .AddKafka(new ProducerConfig
                        {
                            BootstrapServers = hostContext.Configuration["Kafka:BootstrapServers"]
                        });
                })
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                    config.AddEnvironmentVariables();
                })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                    logging.AddDebug();
                });
    }
}