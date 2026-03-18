using Business.AutoMapper;
using CloudinaryDotNet;
using Data.SeedData.SeederHelpers;
using StackExchange.Redis;
using Web.Extensions;
using Web.Hubs;


var builder = WebApplication.CreateBuilder(args);

// =============== [ SERVICES ] ===============

// Middleware
builder.Services.AddHttpContextAccessor(); 

// DbContext
builder.Services.AddPersistence(builder.Configuration); // Extension

// DataINIT
builder.Services.AddDataSeeders(); // Extension

// Controllers
builder.Services.AddControllers();

// AutoMapper
builder.Services.AddAutoMapper(typeof(GeneralMapper));

// Swagger 
builder.Services.AddSwaggerDocumentation();// Extension

builder.Services.AddMemoryCache();

// SignalR coon Microsoft SignalR
//var azureSignalRConnection = builder.Configuration["Azure:SignalR:ConnectionString"];
//if (!string.IsNullOrEmpty(azureSignalRConnection))
//{
//    builder.Services.AddSignalR()
//                    .AddAzureSignalR(azureSignalRConnection);
//}
//else
//{
//    // Si no hay cadena de Azure (Desarrollo Local sin internet o pruebas), usa el estándar.
//    builder.Services.AddSignalR();
//}

// SignalR con Redis Backplane
var signalRBuilder = builder.Services.AddSignalR();
var redisConnectionString = builder.Configuration.GetConnectionString("Redis");

if (!string.IsNullOrEmpty(redisConnectionString))
{
    signalRBuilder.AddStackExchangeRedis(redisConnectionString, options =>
    {
        options.Configuration.ChannelPrefix = RedisChannel.Literal("MyApp_SignalR");
    });
}



// JWT 
builder.Services.AddJwtAuthentication(builder.Configuration); // Extension

// CORS 
builder.Services.AddCustomCors(builder.Configuration); // Extension

// Entities
builder.Services.AddEntitiesServices(); //Extension

// BUSINESS + SERVICES EXTRAS
builder.Services.AddBusinessServices(builder.Configuration); // Extension


// =============== [ Cloudinary Config ] ===============
var account = new Account(
    builder.Configuration["Cloudinary:CloudName"],
    builder.Configuration["Cloudinary:ApiKey"],
    builder.Configuration["Cloudinary:ApiSecret"]
);

var cloudinary = new Cloudinary(account);
builder.Services.AddSingleton(cloudinary);

// =============== [ Build App ] ===============
var app = builder.Build();

// Swagger en desarrollo
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Mi API v1");
        c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
        // None -> todo colapsado
        // List -> lista de endpoints visible, pero sin detalles
        // Full -> todo expandido
    });

    using var scope = app.Services.CreateScope();
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    await SeederExecutor.SeedAllAsync(scope.ServiceProvider, config);
}

if (app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<AppHub>("/appHub");
app.Run();