using AssetTracker;
using AssetTracker.ApiClients;
using AssetTracker.Middleware;
using AssetTracker.Options;
using AssetTracker.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Project.Core.Auth;
using Project.Data;
using Scalar.AspNetCore;
using System.Net.Http.Headers;
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

// 設定一律經由 Options 綁定並於啟動階段驗證
builder.Services.AddOptions<FinMindOptions>()
    .Bind(builder.Configuration.GetSection(FinMindOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<ExchangeRateOptions>()
    .Bind(builder.Configuration.GetSection(ExchangeRateOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<FrontendCorsOptions>()
    .Bind(builder.Configuration.GetSection(FrontendCorsOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// 設定資料庫連線字串，若未設定則直接在啟動階段拋出例外
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "缺少設定 'ConnectionStrings:DefaultConnection'，請參考 secrets.sample.json 設定 user secrets。");
}

builder.Services.AddHttpClient<IStockApiClients, FinMindApiClient>((serviceProvider, client) =>
{
    var finMind = serviceProvider.GetRequiredService<IOptions<FinMindOptions>>().Value;

    client.BaseAddress = new Uri(finMind.NormalizedBaseApi);
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", finMind.Key);
});

builder.Services.AddHttpClient<IExchangeRateApiClient, ExchangeRateApiClient>((serviceProvider, client) =>
{
    var exchangeRate = serviceProvider.GetRequiredService<IOptions<ExchangeRateOptions>>().Value;

    client.BaseAddress = new Uri(exchangeRate.BaseAddressWithKey);
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
    options.UseSqlServer(connectionString, sqlOptions => sqlOptions.EnableRetryOnFailure()));

var frontendCors = builder.Configuration
    .GetSection(FrontendCorsOptions.SectionName)
    .Get<FrontendCorsOptions>() ?? new FrontendCorsOptions();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(frontendCors.AllowedOrigins)
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
