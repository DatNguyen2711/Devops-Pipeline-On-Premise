using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Metrics;
using Project231.Models;
using System.Diagnostics.Metrics;

var builder = WebApplication.CreateBuilder(args);

// Tạo Meter để theo dõi metrics
var meter = new Meter("CustomMetrics", "1.0.0");
var requestCounter = meter.CreateCounter<int>("http_requests_total", "requests", "Count of HTTP requests");
var requestDuration = meter.CreateHistogram<double>("http_request_duration_seconds", "seconds", "Duration of HTTP requests");
var dbQueryDuration = meter.CreateHistogram<double>("database_query_duration_seconds", "seconds", "Duration of database queries");
var activeRequests = meter.CreateUpDownCounter<int>("active_requests", "requests", "Number of active requests");

builder.Services.AddSingleton(meter);

// Thêm các dịch vụ cho ứng dụng
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<ProjectPrn231Context>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("MyDb"))
);

// Cấu hình Swagger với JWT
builder.Services.AddSwaggerGen(s =>
{
    s.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme (Example: 'Bearer 12345abcdef')",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    s.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// CORS Policy
builder.Services.AddCors(opts =>
{
    opts.AddPolicy("CORSPolicy", policy =>
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()
              .SetIsOriginAllowed(_ => true)
    );
});

// OpenTelemetry - Metrics
builder.Services.AddOpenTelemetry()
    .WithMetrics(metricsBuilder =>
    {
        metricsBuilder.AddPrometheusExporter();
        metricsBuilder.AddMeter("Microsoft.AspNetCore.Hosting", "Microsoft.AspNetCore.Server.Kestrel");
        metricsBuilder.AddView("http.server.request.duration", new ExplicitBucketHistogramConfiguration
        {
            Boundaries = new double[] { 0, 0.005, 0.01, 0.025, 0.05, 0.075, 0.1, 0.25, 0.5, 0.75, 1, 2.5, 5, 7.5, 10 }
        });
        metricsBuilder.AddMeter("CustomMetrics", "OrderMetrics");
    });

var app = builder.Build();

// Middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRouting(); // 🔹 Đảm bảo routing hoạt động đúng
app.UseCors("CORSPolicy");

// 🔹 Bổ sung Authentication nếu dùng JWT
app.UseAuthentication();
app.UseAuthorization();

// Expose Prometheus metrics
app.UseOpenTelemetryPrometheusScrapingEndpoint("/api/backend/metrics");

// Middleware tracking request
app.Use(async (context, next) =>
{
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    activeRequests.Add(1); // Tăng số lượng request đang xử lý

    await next();

    stopwatch.Stop();
    activeRequests.Add(-1); // Giảm số lượng request đang xử lý
    requestDuration.Record(stopwatch.Elapsed.TotalSeconds);

    // Ghi lại tổng số request theo mã trạng thái HTTP
    string statusLabel = context.Response.StatusCode switch
    {
        >= 400 and < 500 => "4xx",
        >= 500 => "5xx",
        >= 300 and < 400 => "3xx",
        _ => "2xx"
    };
    requestCounter.Add(1, new KeyValuePair<string, object>("status", statusLabel));
});

// Middleware theo dõi database queries (chỉnh lại)
app.Use(async (context, next) =>
{
    await next(); // Chạy request trước khi đo
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ProjectPrn231Context>();

    var dbStopwatch = System.Diagnostics.Stopwatch.StartNew();
    await dbContext.Database.ExecuteSqlRawAsync("SELECT 1"); // Query test nhanh
    dbStopwatch.Stop();

    dbQueryDuration.Record(dbStopwatch.Elapsed.TotalSeconds);
});

// API endpoint test
app.MapGet("/", () => "Hello OpenTelemetry! ticks:" + DateTime.Now.Ticks.ToString()[^3..]);

app.MapControllers();

app.Run();
