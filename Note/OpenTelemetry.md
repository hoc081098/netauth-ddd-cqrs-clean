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