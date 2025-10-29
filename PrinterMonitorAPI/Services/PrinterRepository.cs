using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using PrinterMonitorAPI.Models;

namespace PrinterMonitorAPI.Services
{
    public class PrinterRepository
    {
        private readonly ConcurrentDictionary<Guid, Printer> _printers = new();
        private readonly SNMPService _snmpService;

        public PrinterRepository(SNMPService snmpService)
        {
            _snmpService = snmpService;
        }

        public IEnumerable<Printer> GetAll() => _printers.Values;

        public Printer? GetById(Guid id) => _printers.TryGetValue(id, out var p) ? p : null;

        public async Task<Printer> AddAsync(string ip, string foto = "")
        {
            var printer = new Printer
            {
                Id = Guid.NewGuid(),
                Ip = ip,
                Foto = foto
            };

            await _snmpService.AtualizarPrinter(printer);
            _printers[printer.Id] = printer;

            return printer;
        }

        public bool Remove(Guid id) => _printers.TryRemove(id, out _);

        public async Task AtualizarTodosAsync()
        {
            var tasks = new List<Task>();
            foreach (var printer in _printers.Values)
                tasks.Add(_snmpService.AtualizarPrinter(printer));

            await Task.WhenAll(tasks);
        }
    }
}
