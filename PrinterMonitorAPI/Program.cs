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

builder.Services.AddSingleton<SNMPService>();
builder.Services.AddScoped<PrinterService>();

builder.Services.AddControllers();

// ====================
// 🔹 Configura CORS para a rede local
// ====================
builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactPolicy", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// ====================
// 🔹 Configuração de Uploads
// ====================
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 10 * 1024 * 1024; // 10MB
});

var app = builder.Build();

// ====================
// 🔹 Middleware
// ====================
app.UseCors("ReactPolicy");
app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();
app.UseStaticFiles();

// ====================
// 🔹 Servir arquivos estáticos (uploads)
// ====================
var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
if (!Directory.Exists(uploadsPath))
    Directory.CreateDirectory(uploadsPath);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});

// ====================
// 🔹 Mapear Controllers
// ====================
app.MapControllers();

// ====================
// 🔹 Configura servidor para rede local
// ====================
app.Urls.Clear();
app.Urls.Add("http://0.0.0.0:5000"); // todos os IPs da máquina

// ====================
// 🔹 Aplica migrations
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
