Kafka Vehicle Location Processing Solution

This solution provides an end-to-end pipeline for ingesting, processing, and persisting vehicle location data using Apache Kafka, a .NET 8 Worker Service, and a simulated Web API. It is designed for reliability, scalability, and ease of local development using Docker.

📂 Table of Contents

Overview
Prerequisites
Running Kafka and Zookeeper with Docker Compose
Configuration
Build and Run
Health Checks
Stopping Services
Project Structure

🦩 Overview

This solution consists of the following components:
Kafka.WorkerA .NET 8 Worker Service that consumes vehicle location messages from a Kafka topic and persists them to a SQL Server database.
SimulatedSamsaraAPIA .NET 8 Web API for simulating vehicle location data and publishing it to Kafka.
InfrastructureImplements data access logic and sets up dependency injection.
DomainContains core business entities and interfaces.

🛠 Prerequisites

.NET 8 SDK

Docker Desktop

(Optional) SQL Server locally or in Docker

🐳 Running Kafka and Zookeeper with Docker Compose

Use the provided docker-compose.yml to start Kafka and Zookeeper locally:

docker-compose up -d
Kafka available at: localhost:9092

Zookeeper available at: localhost:2181

To stop the containers:
docker-compose down

⚙ Configuration

Kafka Settings

In appsettings.json for both Worker and API:

"Kafka": {
  "BootstrapServers": "localhost:9092",
  "TopicName": "vehicle-locations",
  "GroupId": "vehicle-location-consumer-group"
}

Database Connection

"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=VehicleDb;User Id=sa;Password=Your_password123;"
}

Adjust this based on your SQL Server setup.

🚀 Build and Run

Restore and Build:

dotnet build

Apply EF Core Migrations:
dotnet ef database update --project Infrastructure.Persistence

Run the Kafka Worker:
dotnet run --project Kafka.Worker

Run the Simulated API:
dotnet run --project SimulatedSamsaraAPI

Send Test Data:
Use Postman or Swagger UI to POST to:

POST /api/vehicles/locations

Example payload:
{
  "vehicleId": "V100",
  "timestamp": "2025-05-26T10:00:00",
  "latitude": 22.5,
  "longitude": 78.9,
  "speed": 60,
  "fuelLevel": 80
}

💡 Health Checks

Both API and Worker services expose health endpoints (if enabled):
/health/ready – readiness probe
/health/live – liveness probe

Example configuration in Program.cs:

builder.Services.AddHealthChecks()
    .AddKafka(new ProducerConfig { BootstrapServers = kafkaBootstrapServers })
    .AddSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));

🚤 Stopping Services

To stop Kafka and Zookeeper:
docker-compose down

To stop the Worker or API:
Press Ctrl+C in each respective terminal

📂 Project Structure

/
├── docker-compose.yml
├── README.md
├── Kafka.Worker/                 # Kafka consumer (background service)
├── SimulatedSamsaraAPI/          # Web API producer
├── Domain/                       # Domain entities and interfaces
├── Infrastructure.DependencyInjection/
├── Infrastructure.Persistence/   # EF Core DbContext, Repositories
├── Kafka.Infrastructure/         # Kafka topic management

📨 Contributing

PRs and suggestions are welcome. Please open an issue first for significant changes.
