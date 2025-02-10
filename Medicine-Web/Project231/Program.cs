using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Project231.Models;
using Project231.Services;
using OpenTelemetry.Metrics;
using Microsoft.AspNetCore.Http.Features;
using OpenTelemetry.Metrics;
using System.Diagnostics.Metrics;

var builder = WebApplication.CreateBuilder(args);

// Khai báo OpenTelemetry và Meter cho custom metrics
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
        builder.AddPrometheusExporter();

        builder.AddMeter("Microsoft.AspNetCore.Hosting",
                         "Microsoft.AspNetCore.Server.Kestrel");
        builder.AddView("http.server.request.duration",
            new ExplicitBucketHistogramConfiguration
            {
                Boundaries = new double[] { 0, 0.005, 0.01, 0.025, 0.05,
                       0.075, 0.1, 0.25, 0.5, 0.75, 1, 2.5, 5, 7.5, 10 }
            });
    });
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapPrometheusScrapingEndpoint("/api/backend/metrics");

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
app.Use(async (context, next) =>
{
    activeRequests.Add(1); // Tăng số lượng request đang xử lý

    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    await next(); // Tiếp tục xử lý request
    stopwatch.Stop();

    activeRequests.Add(-1); // Giảm số lượng request sau khi hoàn thành

    int statusCode = context.Response.StatusCode;

    // Ghi lại tổng số request theo mã trạng thái
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

    // Ghi lại thời gian xử lý request
    requestDuration.Record(stopwatch.Elapsed.TotalSeconds, new KeyValuePair<string, object>("status", statusCode.ToString()));
});

// Middleware để theo dõi thời gian query database
app.Use(async (context, next) =>
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ProjectPrn231Context>();

    var dbStopwatch = System.Diagnostics.Stopwatch.StartNew();
    await next();
    dbStopwatch.Stop();

    dbQueryDuration.Record(dbStopwatch.Elapsed.TotalSeconds);
});

app.MapGet("/", () => "Hello OpenTelemetry! ticks:" + DateTime.Now.Ticks.ToString()[^3..]);

app.UseHttpsRedirection();
app.UseCors("CORSPolicy");

app.UseAuthorization();

app.MapControllers();
