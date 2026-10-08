using System.Text.Json.Serialization;
using AutoFno.Api.Middleware;
using AutoFno.Infrastructure;
using AutoFno.Infrastructure.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Add Controllers with String Enum Serializer
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// Add SignalR
builder.Services.AddSignalR();

// Swagger Documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "AutoFno API - NIFTY 50 & SENSEX Algorithmic Engine", Version = "v1" });
});

// Register Domain & Infrastructure Services
builder.Services.AddInfrastructureServices(builder.Configuration);

// CORS for Vite dev server and web dashboard
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:5174", "https://localhost:5174", "http://localhost:5173", "http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Global Exception Handler
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "AutoFno API v1"));
}

app.UseCors("CorsPolicy");

app.MapControllers();
app.MapHub<TradingHub>("/hubs/trading");

app.Run();
