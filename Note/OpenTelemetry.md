# OpenTelemetry Note

## Jaeger All-in-One
```
        [ App ]
          |
          |  AddOtlpExporter()
          |  OTEL_EXPORTER_OTLP_ENDPOINT=http://jaeger:4317
          |
          v
[ Jaeger all-in-one ]
  - OTLP receiver (4317)
  - Trace backend
  - UI (16686)
```

## Collector-first

```
App (OTLP exporter)
        ↓
OTEL Collector
  (receiver → processor → exporter)
        ↓
    Backend
        ↓
        UI
```

## I. Install Packages

```
# Automatic tracing, metrics
Install-Package OpenTelemetry.Extensions.Hosting

# Telemetry data exporter
Install-Package OpenTelemetry.Exporter.OpenTelemetryProtocol

# Instrumentation packages
Install-Package OpenTelemetry.Instrumentation.Http
Install-Package OpenTelemetry.Instrumentation.AspNetCore
Install-Package OpenTelemetry.Instrumentation.EntityFrameworkCore
Install-Package OpenTelemetry.Instrumentation.StackExchangeRedis
Install-Package Npgsql.OpenTelemetry
```

## II. Configure OpenTelemetry in ASP.NET Core

```csharp
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

public static class InfrastructureDiModule
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration) 
    {
        [...]
        services.AddOpenTelemetryConfiguration();
    }
    
    extension(IServiceCollection services)
    {
        private IServiceCollection AddOpenTelemetryConfiguration()
        {
            services.AddOpenTelemetry()
                .ConfigureResource(resource => resource.AddService("NetAuth.Api"))
                .WithTracing(tracing =>
                {
                    // Add instrumentations.
                    tracing
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddEntityFrameworkCoreInstrumentation()
                        .AddRedisInstrumentation()
                        .AddNpgsql();

                    // Add OTLP exporter.
                    // We also need to configure an environment variable for the exporter added with AddOtlpExporter to work correctly.
                    // We can set OTEL_EXPORTER_OTLP_ENDPOINT through application settings.
                    // The address specified here will point to a local Jaeger instance.
                    tracing.AddOtlpExporter();
                });

            return services;
        }
    }
}
```

## III. Add Jeager Docker Container and add `OTEL_EXPORTER_OTLP_ENDPOINT` environment variable

##### compose.yaml

```yaml
services:
  [...]

  # Jaeger all-in-one = OTLP receiver (Collector-lite) + Observability backend (Trace storage + query) + UI.
  netauth.jaeger:
    image: jaegertracing/all-in-one:1.76.0
    container_name: netauth.jaeger
    restart: unless-stopped
    ports:
      - "4317:4317" # gRPC - OTLP (OpenTelemetry Protocol) - Primary endpoint for receiving gRPC-based trace data from OpenTelemetry instrumented applications.
      - "16686:16686" # TCP - Jaeger UI - The web interface for viewing and searching traces.
```

##### appsettings.Development.json

```json
{
  [...],
  "OTEL_EXPORTER_OTLP_ENDPOINT": "http://localhost:4317"
}
```