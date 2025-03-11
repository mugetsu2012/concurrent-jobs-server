# Concurrent Jobs Server

A .NET 8 asynchronous concurrent jobs server that provides HTTP endpoints for starting, monitoring, and cancelling long-running jobs. The server is designed to handle concurrent job processing with limits per job type.

## Features

- **Asynchronous Job Processing**: Jobs run asynchronously in the background
- **Concurrency Control**: Maximum of 5 concurrent jobs per job type
- **Job Lifecycle Management**: Start, query status, and cancel jobs
- **RESTful API**: Clean API design with appropriate status codes
- **Structured Logging**: Comprehensive logging using Serilog
- **Cross-Platform**: Runs on Windows, macOS, and Linux
- **Unit and Integration Testing**: Complete test coverage

## API Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/jobs` | POST | Start a new asynchronous job |
| `/api/jobs/{jobId}` | GET | Get the status of a job by ID |
| `/api/jobs/cancel` | POST | Cancel a running job by ID |

### Start a Job

**Request:**
```json
{
  "jobType": "ReportGeneration",
  "jobName": "Monthly Sales Report"
}
```

**Response:**
```json
{
  "jobId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

### Get Job Status

**Response:**
```json
{
  "jobId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "jobType": "ReportGeneration",
  "jobName": "Monthly Sales Report",
  "status": "Running"
}
```

Status values: "Running", "Completed", "Cancelled", "Failed"

### Cancel Job

**Request:**
```json
{
  "jobId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

## Architecture

The server is built using clean architecture principles:

- **Models**: Data models for jobs and API contracts
- **Services**: Core business logic and concurrent jobs management
- **Controllers**: API endpoints and request handling

Key components:
- `JobService`: Singleton service managing job state and execution
- `CancellationTokenSource`: Per-job cancellation support
- `SemaphoreSlim`: Concurrency control per job type

## Getting Started

### Prerequisites

- .NET 8.0 SDK or later
- Optional: Visual Studio 2022, JetBrains Rider, or VS Code

### Configuration

The application uses `appsettings.json` for configuration:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

You can modify the log configuration and other settings as needed.

### Installation

1. Clone the repository
   ```
   git clone https://github.com/mugetsu2012/concurrent-jobs-server.git
   cd concurrent-jobs-server
   ```

2. Build the application
   ```
   dotnet build
   ```

3. Run the application
   ```
   dotnet run
   ```

4. Access the Swagger UI at `https://localhost:5051/swagger` to explore the API

## Integration Testing

The project includes integration tests to verify the behavior of the API endpoints:

```
dotnet test ConcurrentJobs.Server.TestsIntegration
```

Make sure to add the following to your main project file:

```xml
<PropertyGroup>
  <PreserveCompilationContext>true</PreserveCompilationContext>
</PropertyGroup>
```

And add this line to your `Program.cs`:

```csharp
public partial class Program { }
```

## Unit Testing

Unit tests for the JobService verify the core business logic:

```
dotnet test ConcurrentJobs.Server.Tests
```

## Implementation Details

### Concurrency Model

The server uses a combination of asynchronous programming and concurrency control:

1. Each job runs asynchronously using `Task.Run`
2. A `SemaphoreSlim` per job type controls the maximum concurrent jobs
3. Jobs are tracked in a `ConcurrentDictionary` for thread safety
4. Each job has its own `CancellationTokenSource` for cancellation support

### Job Simulation

Jobs are simulated with a delay:

```csharp
await Task.Delay(TimeSpan.FromSeconds(15), cancellationToken);
```

In a real-world scenario, this would be replaced with actual processing logic.

## Dependencies

- ASP.NET Core 8.0
- Serilog
- NUnit (for testing)