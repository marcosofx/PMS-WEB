using Microsoft.AspNetCore.Mvc;
using PrinterMonitorAPI.Models;
using PrinterMonitorAPI.Data;
using PrinterMonitorAPI.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PrinterMonitorAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PrinterController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly SNMPService _snmpService;

        public PrinterController(ApplicationDbContext db, SNMPService snmpService)
        {
            _db = db;
            _snmpService = snmpService;
        }

        // GET api/printer
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> Get()
        {
            var printers = await _db.Impressoras.ToListAsync();

            // Atualiza dados SNMP de todas as impressoras
            var tasks = printers.Select(printer => _snmpService.AtualizarPrinter(printer));
            await Task.WhenAll(tasks);
            await _db.SaveChangesAsync();

            // Retorno para o front
            var results = printers.Select(printer => new
            {
                printer.Id,
                printer.NomeCustomizado,
                printer.Ip,
                NumeroSerie = printer.NumeroSerie ?? "N/A",
                ContadorTotal = printer.ContadorTotal,
                Alertas = printer.Alertas != null && printer.Alertas.Count > 0 ? printer.Alertas : new List<string>(),
                Status = printer.Status ?? "N/A",
                EColorida = printer.EColorida,
                Foto = printer.ImagemUrl,
                Descricao = printer.Descricao,
                Toners = printer.Toners
            }).ToList();

            return Ok(results);
        }

        // GET api/printer/snmp/{id}
        [HttpGet("snmp/{id:guid}")]
        public async Task<ActionResult<object>> GetSnmpData(Guid id)
        {
            var printer = await _db.Impressoras.FindAsync(id);
            if (printer == null)
                return NotFound("Impressora não encontrada.");

            try
            {
                await _snmpService.AtualizarPrinter(printer);
                await _db.SaveChangesAsync();
            }
            catch
            {
                Console.WriteLine($"Erro ao atualizar {printer.Nome} ({printer.Ip}) via SNMP.");
            }

            return Ok(new
            {
                printer.Id,
                printer.NomeCustomizado,
                printer.Ip,
                NumeroSerie = printer.NumeroSerie ?? "N/A",
                ContadorTotal = printer.ContadorTotal,
                Alertas = printer.Alertas != null && printer.Alertas.Count > 0 ? printer.Alertas : new List<string>(),
                Status = printer.Status ?? "N/A",
                EColorida = printer.EColorida,
                Toners = printer.Toners
            });
        }

        // POST api/printer
        [HttpPost]
        public async Task<IActionResult> Adicionar([FromBody] Printer printer)
        {
            if (printer == null)
                return BadRequest("Dados da impressora inválidos.");

            printer.Id = Guid.NewGuid();

            try
            {
                await _snmpService.AtualizarPrinter(printer);
            }
            catch
            {
                // ignora falhas SNMP no cadastro
            }

            _db.Impressoras.Add(printer);
            await _db.SaveChangesAsync();

            return Created($"api/printer/{printer.Id}", printer);
        }

        // PATCH api/printer/{id}
        [HttpPatch("{id:guid}")]
        public async Task<IActionResult> AtualizarInfo(Guid id, [FromBody] Printer updated)
        {
            var printer = await _db.Impressoras.FindAsync(id);
            if (printer == null)
                return NotFound("Impressora não encontrada.");

            printer.NomeCustomizado = updated.NomeCustomizado ?? printer.NomeCustomizado;
            printer.Descricao = updated.Descricao ?? printer.Descricao;
            printer.ImagemUrl = updated.ImagemUrl ?? printer.ImagemUrl;

            await _db.SaveChangesAsync();
            return Ok(printer);
        }

        // DELETE api/printer/{id}
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Remover(Guid id)
        {
            var printer = await _db.Impressoras.FindAsync(id);
            if (printer == null)
                return NotFound("Impressora não encontrada.");

            if (!string.IsNullOrEmpty(printer.ImagemUrl))
            {
                var fileName = Path.GetFileName(printer.ImagemUrl);
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", fileName);
                if (System.IO.File.Exists(filePath))
                    System.IO.File.Delete(filePath);
            }

            _db.Impressoras.Remove(printer);
            await _db.SaveChangesAsync();

            return NoContent();
        }

        // POST api/printer/atualizar
        [HttpPost("atualizar")]
        public async Task<ActionResult<IEnumerable<object>>> AtualizarTodas()
        {
            var printers = await _db.Impressoras.ToListAsync();

            var tasks = printers.Select(printer => _snmpService.AtualizarPrinter(printer));
            await Task.WhenAll(tasks);
            await _db.SaveChangesAsync();

            var results = printers.Select(printer => new
            {
                printer.Id,
                printer.NomeCustomizado,
                printer.Ip,
                NumeroSerie = printer.NumeroSerie ?? "N/A",
                ContadorTotal = printer.ContadorTotal,
                Alertas = printer.Alertas != null && printer.Alertas.Count > 0 ? printer.Alertas : new List<string>(),
                Status = printer.Status ?? "N/A",
                EColorida = printer.EColorida,
                Foto = printer.ImagemUrl,
                Descricao = printer.Descricao,
                Toners = printer.Toners
            }).ToList();

            return Ok(results);
        }

        // POST api/printer/upload
        [HttpPost("upload")]
        public async Task<IActionResult> UploadImage([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { error = "Nenhum arquivo enviado." });

            var allowedTypes = new[] { "image/jpeg", "image/png", "image/jpg", "image/gif", "image/webp" };
            if (!allowedTypes.Contains(file.ContentType.ToLower()))
                return BadRequest(new { error = "Tipo de arquivo não permitido. Envie JPEG, PNG, GIF ou WEBP." });

            var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploadDir))
                Directory.CreateDirectory(uploadDir);

            var ext = Path.GetExtension(file.FileName);
            var fileName = $"{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(uploadDir, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var fileUrl = $"{baseUrl}/uploads/{fileName}";

            return Ok(new { url = fileUrl });
        }
    }
}
