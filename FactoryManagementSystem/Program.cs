using Microsoft.EntityFrameworkCore;
using FactoryManagementSystem.Models;
using FactoryManagementSystem.Services;
using FactoryManagementSystem.Interfaces;
using FactoryManagementSystem.Cache;
using StackExchange.Redis;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:5173") // Vite default port
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Database context
builder.Services.AddDbContext<IgsmasanDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Business Services (Scoped)
builder.Services.AddScoped<IProductionOrdersService, ProductionOrdersService>();
builder.Services.AddScoped<IMaterialsService, MaterialsService>();
builder.Services.AddScoped<IProductsService, ProductsService>();
builder.Services.AddScoped<IRecipesService, RecipesService>();
builder.Services.AddScoped<IProductionOrderDetailsService, ProductionOrderDetailsService>();
builder.Services.AddScoped<ISuggestionsService, SuggestionsService>();
builder.Services.AddScoped<IMESCompleteBatchService, MESCompleteBatchService>();

// Redis Configuration
builder.Services.AddSingleton<IConnectionMultiplexer>(sp => 
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379"));
builder.Services.AddScoped<IRedisCacheService, RedisCacheService>();

// Swagger/OpenAPI Configuration
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "Factory Management System API",
        Description = "Hệ thống quản lý nhà máy",
        Contact = new OpenApiContact
        {
            Name = "Expert Architect Team",
            Email = "support@tantien.com"
        }
    });

    // Tích hợp XML Comments để hiển thị mô tả API từ code
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || true) // Enable always for now to facilitate checking
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
        options.DocumentTitle = "Factory Management API Documentation";
    });
}

app.UseCors("AllowReactApp");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
