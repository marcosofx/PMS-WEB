using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Lextm.SharpSnmpLib;
using Lextm.SharpSnmpLib.Messaging;
using PrinterMonitorAPI.Models;

namespace PrinterMonitorAPI.Services
{
    public class SNMPService
    {
        private readonly string _community = "public";
        private readonly int _getTimeout = 1500;
        private readonly int _walkTimeout = 2500;

        // =====================================================
        // MÉTODO PRINCIPAL
        // =====================================================
        public async Task AtualizarPrinter(Printer printer)
        {
            if (printer == null || string.IsNullOrWhiteSpace(printer.Ip))
                return;

            try
            {
                var endpoint = new IPEndPoint(IPAddress.Parse(printer.Ip), 161);

                printer.Modelo = await QueryOidWithFallback(endpoint, "1.3.6.1.2.1.1.1.0");
                printer.NumeroSerie = await QueryOidWithFallback(endpoint, "1.3.6.1.2.1.43.5.1.1.17.1");

                printer.Status = await ObterStatusGeralAsync(endpoint);
                printer.Alertas = await ObterAlertasUniversalAsync(endpoint);
                printer.Bandejas = await ObterBandejasAsync(endpoint);

                printer.Toners = await ObterTonersGenericoByDescriptionAsync(endpoint);
                printer.ContadorTotal = await ObterContadorGenericoAsync(endpoint);

                printer.EColorida =
                    printer.Toners.ContainsKey("Cyan") ||
                    printer.Toners.ContainsKey("Magenta") ||
                    printer.Toners.ContainsKey("Yellow");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ SNMP erro {printer.Ip}: {ex.Message}");

                // NÃO zera dados — apenas marca indisponível
                printer.Status = "Offline";
            }
        }

        // =====================================================
        // STATUS REAL DA IMPRESSORA
        // =====================================================
        private async Task<string> ObterStatusGeralAsync(IPEndPoint ep)
        {
            string raw = await QueryOidWithFallback(ep, "1.3.6.1.2.1.25.3.2.1.5.1");

            return raw switch
            {
                "1" => "Desconhecido",
                "2" => "Rodando",
                "3" => "Atenção",
                "4" => "Erro",
                _ => "Indisponível"
            };
        }

        // =====================================================
        // ALERTAS (TONER, PORTA, PAPEL)
        // =====================================================
        private async Task<List<string>> ObterAlertasUniversalAsync(IPEndPoint ep)
        {
            var alertas = new List<string>();

            var descricoes = await WalkWithVersions(ep, "1.3.6.1.2.1.43.18.1.1.8");
            var estados = await WalkWithVersions(ep, "1.3.6.1.2.1.43.18.1.1.7");

            int total = Math.Min(descricoes.Count, estados.Count);

            for (int i = 0; i < total; i++)
            {
                string desc = descricoes[i].Data.ToString();
                string estado = estados[i].Data.ToString();

                if (estado != "0")
                    alertas.Add(desc);
            }

            return alertas;
        }

        // =====================================================
        // BANDEJAS DE PAPEL
        // =====================================================
        private async Task<Dictionary<string, string>> ObterBandejasAsync(IPEndPoint ep)
        {
            var bandejas = new Dictionary<string, string>();

            var nomes = await WalkWithVersions(ep, "1.3.6.1.2.1.43.8.2.1.13");
            var status = await WalkWithVersions(ep, "1.3.6.1.2.1.43.8.2.1.10");

            int total = Math.Min(nomes.Count, status.Count);

            for (int i = 0; i < total; i++)
            {
                bandejas[nomes[i].Data.ToString()] =
                    status[i].Data.ToString() == "0" ? "OK" : "Problema";
            }

            return bandejas;
        }

        // =====================================================
        // TONERS (GENÉRICO)
        // =====================================================
        private async Task<Dictionary<string, int>> ObterTonersGenericoByDescriptionAsync(IPEndPoint ep)
        {
            var toners = new Dictionary<string, int>();

            var descr = await WalkWithVersions(ep, "1.3.6.1.2.1.43.12.1.1.4");
            var niveis = await WalkWithVersions(ep, "1.3.6.1.2.1.43.11.1.1.9");

            int total = Math.Min(descr.Count, niveis.Count);

            for (int i = 0; i < total; i++)
            {
                string d = descr[i].Data.ToString().ToLower();
                int val = int.TryParse(niveis[i].Data.ToString(), out int v) ? v : 0;

                if (d.Contains("black") || d.Contains("preto"))
                    toners["Black"] = Normalize(val);
                else if (d.Contains("cyan"))
                    toners["Cyan"] = Normalize(val);
                else if (d.Contains("magenta"))
                    toners["Magenta"] = Normalize(val);
                else if (d.Contains("yellow") || d.Contains("amarelo"))
                    toners["Yellow"] = Normalize(val);
            }

            return toners;
        }

        // =====================================================
        // CONTADOR DE PÁGINAS
        // =====================================================
        private async Task<int> ObterContadorGenericoAsync(IPEndPoint ep)
        {
            string raw = await QueryOidWithFallback(ep, "1.3.6.1.2.1.43.10.2.1.4.1.1");
            return int.TryParse(raw, out int v) ? v : 0;
        }

        // =====================================================
        // UTILITÁRIOS SNMP
        // =====================================================
        private async Task<string> QueryOidWithFallback(IPEndPoint ep, string oid)
        {
            string v2 = await TryGet(ep, oid, VersionCode.V2);
            return v2 != "N/A" ? v2 : await TryGet(ep, oid, VersionCode.V1);
        }

        private async Task<string> TryGet(IPEndPoint ep, string oid, VersionCode ver)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var res = Messenger.Get(
                        ver, ep, new OctetString(_community),
                        new List<Variable> { new Variable(new ObjectIdentifier(oid)) },
                        _getTimeout);

                    return res.First().Data.ToString();
                }
                catch
                {
                    return "N/A";
                }
            });
        }

        private async Task<List<Variable>> WalkWithVersions(IPEndPoint ep, string oid)
        {
            var list = new List<Variable>();

            try
            {
                Messenger.Walk(VersionCode.V2, ep, new OctetString(_community),
                    new ObjectIdentifier(oid), list, _walkTimeout, WalkMode.WithinSubtree);
            }
            catch { }

            if (!list.Any())
            {
                try
                {
                    Messenger.Walk(VersionCode.V1, ep, new OctetString(_community),
                        new ObjectIdentifier(oid), list, _walkTimeout, WalkMode.WithinSubtree);
                }
                catch { }
            }

            return list;
        }

        private int Normalize(int val)
        {
            if (val > 100 && val <= 255)
                val = (int)(val / 255.0 * 100);

            if (val < 0) val = 0;
            if (val > 100) val = 100;

            return val;
        }
    }
}
//ok
