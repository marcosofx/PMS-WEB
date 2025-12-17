using PrinterMonitorAPI.Data;
using PrinterMonitorAPI.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.FileProviders;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

// ====================
// 🔹 Configurações Gerais
// ====================
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite("Data Source=impressoras.db"));

// SNMP deve ser singleton
builder.Services.AddSingleton<SNMPService>();

// Serviços de negócio devem ser Scoped
builder.Services.AddScoped<PrinterService>();

// ====================
// 🔹 Controllers + JSON camelCase
// ====================
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});

// ====================
// 🔹 CORS
// ====================
builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactPolicy", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// ====================
// 🔹 Configuração de Uploads
// ====================
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 10 * 1024 * 1024; // 10 MB
});

var app = builder.Build();

// ====================
// 🔹 Middleware
// ====================
app.UseCors("ReactPolicy");

// ⚠️ Se não usa HTTPS interno, COMENTE
// app.UseHttpsRedirection();

app.UseRouting();
app.UseAuthorization();

// ====================
// 🔹 Servir arquivos estáticos
// ====================
app.UseStaticFiles();

// Pasta uploads (agora dentro de wwwroot)
var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
if (!Directory.Exists(uploadsPath))
    Directory.CreateDirectory(uploadsPath);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});

// ====================
// 🔹 Rotas da API
// ====================
app.MapControllers();

// ====================
// 🔹 SPA (React)
// ====================
app.MapFallbackToFile("index.html");

// ====================
// 🔹 Aceitar conexões na rede
// ====================
app.Urls.Clear();
app.Urls.Add("http://0.0.0.0:5000");

// ====================
// 🔹 Executar migrations automaticamente
// ====================
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

// ====================
// 🔹 Inicializa
// ====================
app.Run();
