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

        // ======================================================
        // GET api/printer  -> lista TUDO + SNMP
        // ======================================================
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> Get()
        {
            var printers = await _db.Impressoras.ToListAsync();

            var tasks = printers.Select(async printer =>
            {
                try { await _snmpService.AtualizarPrinter(printer); }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro SNMP em {printer.Ip}: {ex.Message}");
                }
            });

            await Task.WhenAll(tasks);
            await _db.SaveChangesAsync();

            return Ok(FormatarRetorno(printers));
        }

        // ======================================================
        // GET api/printer/snmp/{id}
        // ======================================================
        [HttpGet("snmp/{id:guid}")]
        public async Task<ActionResult<object>> GetSnmpData(Guid id)
        {
            var printer = await _db.Impressoras.FindAsync(id);
            if (printer == null) return NotFound("Impressora não encontrada.");

            try
            {
                await _snmpService.AtualizarPrinter(printer);
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao atualizar SNMP: {ex.Message}");
            }

            return Ok(FormatarRetorno(printer));
        }

        // ======================================================
        // POST api/printer
        // ======================================================
        [HttpPost]
        public async Task<IActionResult> Adicionar([FromBody] Printer printer)
        {
            if (printer == null)
                return BadRequest("Dados inválidos.");

            printer.Id = Guid.NewGuid();
            printer.imagemUrl = printer.imagemUrl ?? "";

            try
            {
                await _snmpService.AtualizarPrinter(printer);
            }
            catch { }

            _db.Impressoras.Add(printer);
            await _db.SaveChangesAsync();

            return Created($"api/printer/{printer.Id}", FormatarRetorno(printer));
        }

        // ======================================================
        // PATCH api/printer/{id}
        // ======================================================
        [HttpPatch("{id:guid}")]
        public async Task<IActionResult> AtualizarInfo(Guid id, [FromBody] Printer updated)
        {
            var printer = await _db.Impressoras.FindAsync(id);
            if (printer == null)
                return NotFound("Impressora não encontrada.");

            printer.NomeCustomizado = updated.NomeCustomizado ?? printer.NomeCustomizado;
            printer.Descricao = updated.Descricao ?? printer.Descricao;
            printer.imagemUrl = updated.imagemUrl ?? printer.imagemUrl;

            await _db.SaveChangesAsync();

            return Ok(FormatarRetorno(printer));
        }

        // ======================================================
        // DELETE api/printer/{id}
        // ======================================================
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Remover(Guid id)
        {
            var printer = await _db.Impressoras.FindAsync(id);
            if (printer == null)
                return NotFound("Impressora não encontrada.");

            if (!string.IsNullOrEmpty(printer.imagemUrl))
            {
                try
                {
                    var fileName = Path.GetFileName(printer.imagemUrl);
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", fileName);

                    if (System.IO.File.Exists(filePath))
                        System.IO.File.Delete(filePath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro ao remover imagem: {ex.Message}");
                }
            }

            _db.Impressoras.Remove(printer);
            await _db.SaveChangesAsync();

            return NoContent();
        }

        // ======================================================
        // POST api/printer/atualizar
        // ======================================================
        [HttpPost("atualizar")]
        public async Task<ActionResult<IEnumerable<object>>> AtualizarTodas()
        {
            var printers = await _db.Impressoras.ToListAsync();

            var tasks = printers.Select(async printer =>
            {
                try { await _snmpService.AtualizarPrinter(printer); }
                catch (Exception ex) { Console.WriteLine($"Erro SNMP: {ex.Message}"); }
            });

            await Task.WhenAll(tasks);
            await _db.SaveChangesAsync();

            return Ok(FormatarRetorno(printers));
        }

        // ======================================================
        // POST api/printer/upload  -> upload ajustado
        // ======================================================
        [HttpPost("upload")]
        public async Task<IActionResult> UploadImage([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { error = "Nenhum arquivo enviado." });

            var allowed = new[] { "image/jpeg", "image/png", "image/jpg", "image/gif", "image/webp" };
            if (!allowed.Contains(file.ContentType.ToLower()))
                return BadRequest(new { error = "Tipo de arquivo não permitido." });

            var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            Directory.CreateDirectory(uploadDir);

            var ext = Path.GetExtension(file.FileName);
            var fileName = $"{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(uploadDir, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
                await file.CopyToAsync(stream);

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var fileUrl = $"{baseUrl}/uploads/{fileName}";

            return Ok(new { url = fileUrl });
        }

        // ======================================================
        // MÉTODOS AUXILIARES
        // ======================================================
        private object FormatarRetorno(Printer p) =>
            new
            {
                p.Id,
                p.NomeCustomizado,
                p.Ip,
                NumeroSerie = p.NumeroSerie ?? "N/A",
                ContadorTotal = p.ContadorTotal,
                Alertas = p.Alertas ?? new List<string>(),
                Status = p.Status ?? "N/A",
                EColorida = p.EColorida,
                imagemUrl = p.imagemUrl ?? "",
                Descricao = p.Descricao ?? "",
                Toners = p.Toners ?? new Dictionary<string, int>()
            };

        private IEnumerable<object> FormatarRetorno(IEnumerable<Printer> printers) =>
            printers.Select(FormatarRetorno);
    }
}
