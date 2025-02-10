using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Project231.Models;
using OpenTelemetry.Metrics;
using Microsoft.AspNetCore.Http.Features;
using System.Diagnostics.Metrics;


var builder = WebApplication.CreateBuilder(args);
var meter = new Meter("CustomMetrics", "1.0.0");
var requestCounter = meter.CreateCounter<int>("http_requests_total", "requests", "Count of HTTP requests");
var requestDuration = meter.CreateHistogram<double>("http_request_duration_seconds", "seconds", "Duration of HTTP requests");
var dbQueryDuration = meter.CreateHistogram<double>("database_query_duration_seconds", "seconds", "Duration of database queries");
var activeRequests = meter.CreateUpDownCounter<int>("active_requests", "requests", "Number of active requests");

builder.Services.AddSingleton<ContosoMetrics>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<ProjectPrn231Context>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("MyDb")));
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
builder.Services.AddCors(opts =>
{
    opts.AddPolicy("CORSPolicy", builder =>
    builder.AllowAnyHeader().AllowAnyMethod().
    AllowCredentials().SetIsOriginAllowed((host) => true));
});
builder.Services.AddOpenTelemetry()
    .WithMetrics(builder =>
    {
        builder.AddPrometheusExporter();  // Thêm Prometheus exporter

        // Thêm các Meter mặc định cho Kestrel và HTTP server
        builder.AddMeter("Microsoft.AspNetCore.Hosting",
                         "Microsoft.AspNetCore.Server.Kestrel");

        builder.AddView("http.server.request.duration",
            new ExplicitBucketHistogramConfiguration
            {
                Boundaries = new double[] { 0, 0.005, 0.01, 0.025, 0.05,
                                           0.075, 0.1, 0.25, 0.5, 0.75, 1, 2.5, 5, 7.5, 10 }
            });

        // Thêm metrics tùy chỉnh
        var meter = new Meter("CustomMetrics", "1.0.0");

        // 1. Counter để đếm số request HTTP
        var requestCounter = meter.CreateCounter<int>("http_requests_total", "requests", "Total HTTP requests processed");

        // 2. Histogram để đo thời gian xử lý request
        var requestDuration = meter.CreateHistogram<double>("http_request_duration_seconds", "seconds", "Duration of HTTP requests");

        // 3. Gauge để theo dõi số lượng request đang xử lý
        var activeRequests = meter.CreateUpDownCounter<int>("http_server_active_requests", "requests", "Number of active requests");

        // 4. Histogram để đo thời gian truy vấn database
        var dbQueryDuration = meter.CreateHistogram<double>("database_query_duration_seconds", "seconds", "Duration of database queries");

        builder.AddMeter("CustomMetrics");  // Đảm bảo meter này được sử dụng
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseOpenTelemetryPrometheusScrapingEndpoint("/api/backend/metrics");



app.Use(async (context, next) =>
{
    var tagsFeature = context.Features.Get<IHttpMetricsTagsFeature>();
    if (tagsFeature != null)
    {
        var source = context.Request.Query["utm_medium"].ToString() switch
        {
            "" => "none",
            "social" => "social",
            "email" => "email",
            "organic" => "organic",
            _ => "other"
        };
        tagsFeature.Tags.Add(new KeyValuePair<string, object?>("mkt_medium", source));
    }

    await next.Invoke();
});



// Middleware để theo dõi số request đang xử lý
// Middleware để theo dõi số lượng request HTTP
app.Use(async (context, next) =>
{
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    
    // Tăng số lượng request đang xử lý
    activeRequests.Add(1);
    
    await next();
    
    stopwatch.Stop();
    
    // Giảm số lượng request đang xử lý
    activeRequests.Add(-1);
    
    // Ghi lại thời gian xử lý request
    requestDuration.Record(stopwatch.Elapsed.TotalSeconds);
    
    int statusCode = context.Response.StatusCode;

    // Ghi lại tổng số request theo mã trạng thái HTTP
    if (statusCode >= 400 && statusCode < 500)
    {
        requestCounter.Add(1, new KeyValuePair<string, object>("status", "4xx"));
    }
    else if (statusCode >= 500)
    {
        requestCounter.Add(1, new KeyValuePair<string, object>("status", "5xx"));
    }
    else if (statusCode >= 300 && statusCode < 400)
    {
        requestCounter.Add(1, new KeyValuePair<string, object>("status", "3xx"));
    }
    else
    {
        requestCounter.Add(1, new KeyValuePair<string, object>("status", "2xx"));
    }
});

// Middleware để theo dõi thời gian truy vấn database
app.Use(async (context, next) =>
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ProjectPrn231Context>();
    
    var dbStopwatch = System.Diagnostics.Stopwatch.StartNew();
    await next();
    dbStopwatch.Stop();
    
    // Ghi lại thời gian truy vấn database
    dbQueryDuration.Record(dbStopwatch.Elapsed.TotalSeconds);
});

app.MapGet("/", () => "Hello OpenTelemetry! ticks:"
                     + DateTime.Now.Ticks.ToString()[^3..]);
app.UseHttpsRedirection();
app.UseCors("CORSPolicy");

app.UseAuthorization();

app.MapControllers();

app.Run();
