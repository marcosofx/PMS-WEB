using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PrinterMonitorAPI.Models;
using PrinterMonitorAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace PrinterMonitorAPI.Services
{
    public class PrinterService
    {
        private readonly SNMPService _snmpService;
        private readonly ApplicationDbContext _db;

        public PrinterService(SNMPService snmpService, ApplicationDbContext db)
        {
            _snmpService = snmpService;
            _db = db;
        }

        // -------------------------------------------------------------
        // LISTAR
        // -------------------------------------------------------------
        public async Task<List<Printer>> ListarImpressorasAsync()
        {
            return await _db.Impressoras.AsNoTracking().ToListAsync();
        }

        // -------------------------------------------------------------
        // BUSCAR POR ID
        // -------------------------------------------------------------
        public async Task<Printer?> BuscarPorIdAsync(Guid id)
        {
            return await _db.Impressoras.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        }

        // -------------------------------------------------------------
        // ADICIONAR IMPRESSORA
        // -------------------------------------------------------------
        public async Task<Printer> AdicionarImpressoraAsync(Printer printer)
        {
            printer.Id = Guid.NewGuid();

            try
            {
                await _snmpService.AtualizarPrinter(printer);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PrinterService] Falha ao atualizar SNMP ao adicionar {printer.Nome}: {ex.Message}");
            }

            _db.Impressoras.Add(printer);
            await _db.SaveChangesAsync();

            return printer;
        }

        // -------------------------------------------------------------
        // REMOVER IMPRESSORA
        // -------------------------------------------------------------
        public async Task<bool> RemoverImpressoraAsync(Guid id)
        {
            var printer = await _db.Impressoras.FindAsync(id);
            if (printer == null) return false;

            _db.Impressoras.Remove(printer);
            await _db.SaveChangesAsync();
            return true;
        }

        // -------------------------------------------------------------
        // ATUALIZAR NOME E DESCRIÇÃO
        // -------------------------------------------------------------
        public async Task<Printer?> AtualizarInfoAsync(Guid id, string? nomeCustomizado, string? descricao)
        {
            var printer = await _db.Impressoras.FindAsync(id);
            if (printer == null) return null;

            if (!string.IsNullOrWhiteSpace(nomeCustomizado))
                printer.NomeCustomizado = nomeCustomizado;

            if (!string.IsNullOrWhiteSpace(descricao))
                printer.Descricao = descricao;

            await _db.SaveChangesAsync();
            return printer;
        }

        // -------------------------------------------------------------
        // ATUALIZAR TODAS AS IMPRESSORAS VIA SNMP
        // -------------------------------------------------------------
        public async Task<List<Printer>> AtualizarTodasAsync()
        {
            var printers = await _db.Impressoras.ToListAsync();

            // Atualiza cada impressora em paralelo, mas usando um contexto separado
            var tasks = printers.Select(async printer =>
            {
                try
                {
                    await _snmpService.AtualizarPrinter(printer);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[PrinterService] Erro atualizando {printer.Nome} ({printer.Ip}): {ex.Message}");
                }
            });

            await Task.WhenAll(tasks);

            await _db.SaveChangesAsync();
            return printers;
        }
    }
}