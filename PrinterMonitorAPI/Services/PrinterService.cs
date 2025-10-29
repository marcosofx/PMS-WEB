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

        // Listar todas as impressoras
        public async Task<List<Printer>> ListarImpressorasAsync()
        {
            return await _db.Impressoras.ToListAsync();
        }

        // Buscar por ID
        public async Task<Printer?> BuscarPorIdAsync(Guid id)
        {
            return await _db.Impressoras.FindAsync(id);
        }

        // Adicionar impressora
        public async Task<Printer> AdicionarImpressoraAsync(Printer printer)
        {
            printer.Id = Guid.NewGuid();

            try
            {
                await _snmpService.AtualizarPrinter(printer);
            }
            catch
            {
                Console.WriteLine($"Erro ao atualizar impressora {printer.Nome} ({printer.Ip})");
            }

            _db.Impressoras.Add(printer);
            await _db.SaveChangesAsync();

            return printer;
        }

        // Remover impressora
        public async Task<bool> RemoverImpressoraAsync(Guid id)
        {
            var printer = await _db.Impressoras.FindAsync(id);
            if (printer == null) return false;

            _db.Impressoras.Remove(printer);
            await _db.SaveChangesAsync();
            return true;
        }

        // Atualizar NomeCustomizado e Descricao
        public async Task<Printer?> AtualizarInfoAsync(Guid id, string? nomeCustomizado, string? descricao)
        {
            var printer = await _db.Impressoras.FindAsync(id);
            if (printer == null) return null;

            printer.NomeCustomizado = nomeCustomizado ?? printer.NomeCustomizado;
            printer.Descricao = descricao ?? printer.Descricao;

            await _db.SaveChangesAsync();
            return printer;
        }

        // Atualizar todas as impressoras via SNMP
        public async Task<List<Printer>> AtualizarTodasAsync()
        {
            var printers = await _db.Impressoras.ToListAsync();

            var tasks = new List<Task>();
            foreach (var printer in printers)
            {
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        await _snmpService.AtualizarPrinter(printer);
                    }
                    catch
                    {
                        Console.WriteLine($"Erro ao atualizar {printer.Nome} ({printer.Ip})");
                    }
                }));
            }

            await Task.WhenAll(tasks);
            await _db.SaveChangesAsync();

            return printers;
        }
    }
}
