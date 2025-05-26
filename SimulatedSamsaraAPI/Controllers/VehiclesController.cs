using Confluent.Kafka;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace SimulatedSamsaraAPI.Controllers
{
    [ApiController]
    [Route("api/vehicles")]
    public class VehiclesController : ControllerBase
    {
        private readonly IProducer<string, string> _producer;
        private readonly ILogger<VehiclesController> _logger;
        private const string TopicName = "vehicle-locations";

        public VehiclesController(ILogger<VehiclesController> logger)
        {
            var config = new ProducerConfig { BootstrapServers = "localhost:9092" };
            _producer = new ProducerBuilder<string, string>(config).Build();
            _logger = logger;
        }

        [HttpPost("locations")]
        public async Task<IActionResult> ReportLocation([FromBody] VehicleLocation location)
        {
            try
            {
                var message = new Message<string, string>
                {
                    Key = location.VehicleId,
                    Value = JsonSerializer.Serialize(location)
                };

                await _producer.ProduceAsync(TopicName, message);
                _logger.LogInformation($"Produced message for {location.VehicleId}");

                return Accepted();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to produce message");
                return StatusCode(500);
            }
        }
    }

    public record VehicleLocation(
        string VehicleId,
        DateTime Timestamp,
        double Latitude,
        double Longitude,
        int Speed,
        int FuelLevel);
}
