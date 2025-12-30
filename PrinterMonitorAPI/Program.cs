using PrinterMonitorAPI.Data;
using PrinterMonitorAPI.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.FileProviders;
using Serilog;
using System.IO;


// ======================================================
// 🔹 CAMINHOS BASE (AppData)
// ======================================================
var appDataPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "PMS"
);

Directory.CreateDirectory(appDataPath);

var logsPath = Path.Combine(appDataPath, "logs");
Directory.CreateDirectory(logsPath);

var dbPath = Path.Combine(appDataPath, "impressoras.db");

// ======================================================
// 🔹 SERILOG (LOG EM ARQUIVO)
// ======================================================
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.File(
        Path.Combine(logsPath, "pms-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 15
    )
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// ======================================================
// 🔹 RODAR COMO SERVIÇO DO WINDOWS
// ======================================================
builder.Host.UseWindowsService();

// ======================================================
// 🔹 LOGGING
// ======================================================
builder.Host.UseSerilog();

// ======================================================
// 🔹 BANCO DE DADOS (SQLite)
// ======================================================
var connectionString = $"Data Source={dbPath};Cache=Shared";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlite(connectionString);
});

// ======================================================
// 🔹 SERVIÇOS
// ======================================================
builder.Services.AddSingleton<SNMPService>();
builder.Services.AddScoped<PrinterService>();

// ======================================================
// 🔹 CONTROLLERS
// ======================================================
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy =
            System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// ======================================================
// 🔹 CORS
// ======================================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactPolicy", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// ======================================================
// 🔹 UPLOAD
// ======================================================
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 10 * 1024 * 1024;
});

var app = builder.Build();

// ======================================================
// 🔹 GARANTIR BANCO
// ======================================================
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.EnsureCreated();
}

// ======================================================
// 🔹 MIDDLEWARE
// ======================================================
app.UseCors("ReactPolicy");
app.UseRouting();
app.UseAuthorization();

// ======================================================
// 🔹 FRONTEND (VUE)
// ======================================================
app.UseDefaultFiles();
app.UseStaticFiles();

// uploads
var uploadsPath = Path.Combine(
    app.Environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"),
    "uploads"
);

Directory.CreateDirectory(uploadsPath);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});

// ======================================================
// 🔹 ROTAS
// ======================================================
app.MapControllers();
app.MapFallbackToFile("index.html");

// ======================================================
// 🔹 PORTA CONFIGURÁVEL
// ======================================================
var port = builder.Configuration.GetValue<int>("Port", 5000);

app.Urls.Clear();
app.Urls.Add($"http://0.0.0.0:{port}");

// ======================================================
// 🔹 START
// ======================================================
try
{
    Log.Information("PMS iniciado com sucesso na porta {Port}", port);
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Falha crítica ao iniciar o PMS");
}
finally
{
    Log.CloseAndFlush();
}
