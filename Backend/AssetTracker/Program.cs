using AssetTracker;
using AssetTracker.ApiClients;
using AssetTracker.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Project.Data;
using Scalar.AspNetCore;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

//TODO: 了解有哪些方式或工具可以記錄log

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // 讓Enum以字串形式接收和回傳
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: false));
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

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<TransactionService>();
builder.Services.AddScoped<StockService>();
builder.Services.AddScoped<ExchangeRateService>();
builder.Services.AddScoped<PositionService>();

builder.Services.AddAuthentication(option =>
{
    option.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    option.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(option =>
{
    option.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidateAudience = true,
        ValidAudiences = [builder.Configuration["Jwt:Audience"]],
        ValidateLifetime = true,
    };
});
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

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

if (app.Environment.IsProduction())
{
    var db = app.Services.CreateScope().ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
