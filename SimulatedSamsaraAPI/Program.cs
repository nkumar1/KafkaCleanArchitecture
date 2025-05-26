using Confluent.Kafka;
using Microsoft.Extensions.Diagnostics.HealthChecks; // Ensure this namespace is included
using HealthChecks.Kafka; // Add this namespace if using a Kafka health check library
using HealthChecks.SqlServer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using System.Text.Json;
using Confluent.Kafka.Admin;


var builder = WebApplication.CreateBuilder(args);

// ======== Configuration ========
builder.Configuration
.AddJsonFile("appsettings.json", optional: false)
.AddEnvironmentVariables();

//ToDo: Update kafka bootstrap servers from environment variables or configuration
var kafkaBootstrapServers = builder.Configuration["Kafka:BootstrapServers"] ?? "localhost:9092";

// ======== Services ========
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddSingleton<IProducer<string, string>>(_ =>
    new ProducerBuilder<string, string>(new ProducerConfig
    {
        BootstrapServers = kafkaBootstrapServers,
        Acks = Acks.All,
        EnableIdempotence = true, // Ensure exactly-once delivery, no duplicate messages.
        MessageSendMaxRetries = 3,
        RetryBackoffMs = 100,
        LingerMs = 5,
        CompressionType = CompressionType.Snappy // Use Snappy compression for better performance
    }).Build());


// Kafka AdminClient (Singleton)

//Manages Kafka topics/metadata
//Separate from producer for clean separation of concerns

builder.Services.AddSingleton<IAdminClient>(_ =>
    new AdminClientBuilder(new AdminClientConfig
    {
        BootstrapServers = kafkaBootstrapServers
    }).Build());

// Health Checks: 
// Kafka Health: Verifies broker connectivity
// SQL Health: Checks database connection
// Exposed via /health/ready and /health/live endpoints

builder.Services.AddHealthChecks()
    .AddKafka(new ProducerConfig { BootstrapServers = kafkaBootstrapServers })
    .AddSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));

// ======== App Build ========
var app = builder.Build();

// ======== Middleware Pipeline ========
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Health Check Endpoints
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                duration = e.Value.Duration.TotalMilliseconds
            })
        }));
    }
});

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false // Simple liveness check
});

// ======== Kafka Initialization ========
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var adminClient = scope.ServiceProvider.GetRequiredService<IAdminClient>();

    try
    {
        // Verify Kafka connection
        var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(10));
        logger.LogInformation("Connected to Kafka brokers: {Brokers}",
            string.Join(", ", metadata.Brokers.Select(b => $"{b.Host}:{b.Port}")));

        // Ensure topic exists
        var topicName = "vehicle-locations";
        if (!metadata.Topics.Exists(t => t.Topic == topicName))
        {
            try
            {
                await adminClient.CreateTopicsAsync(new[] {
                new TopicSpecification {
                    Name = topicName,
                    NumPartitions = 3,
                    ReplicationFactor = 1
                }
            });
                logger.LogInformation("Created Kafka topic: {Topic}", topicName);
            }
            catch (CreateTopicsException ex) when (ex.Results.Any(r => r.Error.Code == ErrorCode.TopicAlreadyExists))
            {
                logger.LogInformation("Topic '{Topic}' already exists.", topicName);
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "Kafka initialization failed");
        throw; // Fail fast if Kafka is critical
    }
}

// ======== Startup Complete ========
//logger.LogInformation("Application starting with Kafka at {Brokers}", kafkaBootstrapServers);

app.Run();