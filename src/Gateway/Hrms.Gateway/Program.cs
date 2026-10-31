var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

// Liveness only. Readiness checks (DB, RabbitMQ, Redis, S3) are added with Hrms.Web in prompt 02.
app.MapGet("/health/live", () => Results.Ok(new { status = "Healthy" }));

app.Run();

/// <summary>Exposed for WebApplicationFactory in integration tests.</summary>
public partial class Program;
