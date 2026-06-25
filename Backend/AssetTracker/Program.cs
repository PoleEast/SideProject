using AssetTracker;
using AssetTracker.ApiClients;
using AssetTracker.Middleware;
using AssetTracker.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Project.Core.Auth;
using Project.Data;
using Scalar.AspNetCore;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // 讓Enum以字串形式接收和回傳
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: false));
    });

// ModelState 驗證失敗時，改回與全域 Handler 一致的 ProblemDetails 格式
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var problemDetails = new ValidationProblemDetails(context.ModelState)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "請求參數驗證失敗",
            Instance = context.HttpContext.Request.Path
        };
        problemDetails.Extensions["timestamp"] = DateTime.UtcNow;

        return new BadRequestObjectResult(problemDetails);
    };
});

builder.Services.AddHttpClient<IStockApiClients, FinMindApiClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["FinMindApi:BaseApi"]!);
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("Authorization", builder.Configuration["FinMindApi:Key"]);
});

builder.Services.AddHttpClient<IExchangeRateApiClient, ExchangeRateApiClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ExchangeRateApi:BaseApi"]! + builder.Configuration["ExchangeRateApi:Key"] + "/");
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddKeyedSingleton("ApiResponse", new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    PropertyNameCaseInsensitive = true,
});

builder.Services.AddScoped<TransactionService>();
builder.Services.AddScoped<StockService>();
builder.Services.AddScoped<ExchangeRateService>();
builder.Services.AddScoped<PositionService>();

builder.Services.AddSharedAuth(builder.Configuration);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure()));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();

    //將enum型別更改成string，並顯示可選值
    options.AddSchemaTransformer((schema, context, cancellationToken) =>
    {
        if (context.JsonTypeInfo.Type.IsEnum)
        {
            schema.Type = JsonSchemaType.String;
            schema.Enum = Enum.GetNames(context.JsonTypeInfo.Type)
                .Select(name => (JsonNode)name)
                .ToList();
        }

        return Task.CompletedTask;
    });
});
builder.Services.AddMemoryCache();

MapsterConfig.SettingGlobalConfig();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
