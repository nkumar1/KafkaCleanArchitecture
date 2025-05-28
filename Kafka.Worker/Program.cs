using Confluent.Kafka;
using Domain.Interfaces;
using Infrastructure.DependencyInjection;
using Infrastructure.Persistence;
using Kafka.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static System.Formats.Asn1.AsnWriter;

namespace Kafka.Worker
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();

            // Initialize database
            //Commenting code since DB migration is already completed.

            //using (var scope = host.Services.CreateScope())
            //{
            //    var services = scope.ServiceProvider;
            //    var logger = services.GetRequiredService<ILogger<Program>>();

            //    try
            //    {
            //        var context = services.GetRequiredService<AppDbContext>();
            //        logger.LogInformation("Applying database migrations...");
            //        await context.Database.MigrateAsync();
            //        logger.LogInformation("Migrations applied successfully");
            //    }
            //    catch (Exception ex)
            //    {
            //        logger.LogError(ex, "An error occurred while migrating the database");
            //        throw; // Rethrow to fail fast if migrations fail
            //    }
            //}

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
                    // 1. Binds section "Kafka" to KafkaOptions and registers it for DI
                    services.Configure<KafkaOptions>(
                        hostContext.Configuration.GetSection("Kafka"));

                    // 2. Register Infrastructure (DbContext, Repositories)
                    services.AddInfrastructure(hostContext.Configuration);

                    // 3. Configure Kafka Consumer
                    // Kafka Consumer (Transient - new instance each time)
                    services.AddTransient<IConsumer<string, string>>(sp =>
                    {
                        // Strongly typed config with IOptions<T>
                        var options = sp.GetRequiredService<IOptions<KafkaOptions>>().Value;

                        var config = new ConsumerConfig
                        {
                            BootstrapServers = options.BootstrapServers,
                            GroupId = options.GroupId, //GroupId is used only in Kafka consumers — not in producers.
                            //The consumer only reads new messages
                            AutoOffsetReset = AutoOffsetReset.Latest, //.Earliest, //.Latest
                            EnableAutoCommit = false, //Manually commit offsets (after processing), doing after saving data into DB.
                            EnableAutoOffsetStore = false,
                            AllowAutoCreateTopics = options.AllowAutoCreateTopics, // set to true: Important for local development not for production deployment.
                            SessionTimeoutMs = 10000,  // Time to detect dead consumers
                            MaxPollIntervalMs = 300000, // Adjust if processing takes longer
                            EnablePartitionEof = true // For explicit EOF handling
                        };

                        //Kafka Consumer expects Kafka messages with string keys and string values.

                        //“Hey Kafka, I expect the message Key to be a string, and the Value to also be a string.”
                        //The consumer will deserialize the key and value of the Kafka message as strings.

                        //In  API (Kafka producer), message format is explicitly set to <string, string>. Key is "VehicleId" and Value is
                        //JSON created from VehicleLocation object.

                        return new ConsumerBuilder<string, string>(config).Build();
                    });

                    // 4. Register KafkaTopicCreator (Singleton)
                    // Kafka Topic Creator (Singleton - can be reused)
                    services.AddSingleton<KafkaTopicCreator>(sp =>
                        new KafkaTopicCreator(hostContext.Configuration["Kafka:BootstrapServers"]));

                    // 4.Register Worker as a Singleton Background Service
                    //All BackgroundService or IHostedService instances must be singleton.

                    services.AddSingleton<IHostedService, Worker>();

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