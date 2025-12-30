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

                // 🔥 TONER COM SUPORTE ESPECÍFICO RICOH 5200
                printer.Toners = await ObterTonersComSuporteRicoh5200(endpoint, printer.Modelo);

                printer.ContadorTotal = await ObterContadorComSuporteRicoh5200(endpoint, printer.Modelo);

                printer.EColorida =
                    printer.Toners.ContainsKey("Cyan") ||
                    printer.Toners.ContainsKey("Magenta") ||
                    printer.Toners.ContainsKey("Yellow");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ SNMP erro {printer.Ip}: {ex.Message}");
                printer.Status = "Offline";
            }
        }

        // =====================================================
        // TONERS COM SUPORTE RICOH 5200
        // =====================================================
        private async Task<Dictionary<string, int>> ObterTonersComSuporteRicoh5200(
            IPEndPoint ep, string modelo)
        {
            // Só ativa lógica especial se for Ricoh 5200
            if (!string.IsNullOrEmpty(modelo) &&
                modelo.ToUpper().Contains("RICOH") &&
                modelo.Contains("5200"))
            {
                return await ObterTonerRicoh5200(ep);
            }

            // Fallback seguro
            return await ObterTonersGenericoByDescriptionAsync(ep);
        }

        private async Task<Dictionary<string, int>> ObterTonerRicoh5200(IPEndPoint ep)
        {
            var toners = new Dictionary<string, int>();

            var descricoes = await WalkWithVersions(ep, "1.3.6.1.2.1.43.12.1.1.4");
            var niveis = await WalkWithVersions(ep, "1.3.6.1.2.1.43.11.1.1.9");
            var capacidades = await WalkWithVersions(ep, "1.3.6.1.2.1.43.11.1.1.8");

            int total = Math.Min(descricoes.Count,
                        Math.Min(niveis.Count, capacidades.Count));

            for (int i = 0; i < total; i++)
            {
                string desc = descricoes[i].Data.ToString().ToLower();

                int nivel = int.TryParse(niveis[i].Data.ToString(), out int n) ? n : -1;
                int capacidade = int.TryParse(capacidades[i].Data.ToString(), out int c) ? c : -1;

                int percentual = CalcularPercentualRicoh(nivel, capacidade);

                if (desc.Contains("black") || desc.Contains("preto"))
                    toners["Black"] = percentual;
                else if (desc.Contains("cyan"))
                    toners["Cyan"] = percentual;
                else if (desc.Contains("magenta"))
                    toners["Magenta"] = percentual;
                else if (desc.Contains("yellow"))
                    toners["Yellow"] = percentual;
            }

            return toners;
        }

        private int CalcularPercentualRicoh(int nivel, int capacidade)
        {
            if (nivel < 0 || capacidade <= 0)
                return 0;

            int pct = (int)Math.Round((nivel / (double)capacidade) * 100);

            if (pct < 0) pct = 0;
            if (pct > 100) pct = 100;

            return pct;
        }

        // =====================================================
        // CONTADOR COM SUPORTE RICOH
        // =====================================================
        private async Task<int> ObterContadorComSuporteRicoh5200(IPEndPoint ep, string modelo)
        {
            if (!string.IsNullOrEmpty(modelo) && modelo.ToUpper().Contains("RICOH"))
            {
                string raw = await QueryOidWithFallback(
                    ep, "1.3.6.1.4.1.367.3.2.1.2.19.5.1.6.1");

                if (int.TryParse(raw, out int val) && val > 0)
                    return val;
            }

            return await ObterContadorGenericoAsync(ep);
        }

        // =====================================================
        // STATUS / ALERTAS / BANDEJAS
        // =====================================================
        private async Task<string> ObterStatusGeralAsync(IPEndPoint ep)
        {
            string raw = await QueryOidWithFallback(ep, "1.3.6.1.2.1.25.3.2.1.5.1");

            return raw switch
            {
                "2" => "Rodando",
                "3" => "Atenção",
                "4" => "Erro",
                _ => "Indisponível"
            };
        }

        private async Task<List<string>> ObterAlertasUniversalAsync(IPEndPoint ep)
        {
            var alertas = new List<string>();

            var descricoes = await WalkWithVersions(ep, "1.3.6.1.2.1.43.18.1.1.8");
            var estados = await WalkWithVersions(ep, "1.3.6.1.2.1.43.18.1.1.7");

            int total = Math.Min(descricoes.Count, estados.Count);

            for (int i = 0; i < total; i++)
                if (estados[i].Data.ToString() != "0")
                    alertas.Add(descricoes[i].Data.ToString());

            return alertas;
        }

        private async Task<Dictionary<string, string>> ObterBandejasAsync(IPEndPoint ep)
        {
            var bandejas = new Dictionary<string, string>();

            var nomes = await WalkWithVersions(ep, "1.3.6.1.2.1.43.8.2.1.13");
            var status = await WalkWithVersions(ep, "1.3.6.1.2.1.43.8.2.1.10");

            int total = Math.Min(nomes.Count, status.Count);

            for (int i = 0; i < total; i++)
                bandejas[nomes[i].Data.ToString()] =
                    status[i].Data.ToString() == "0" ? "OK" : "Problema";

            return bandejas;
        }

        // =====================================================
        // GENÉRICOS
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
                else if (d.Contains("yellow"))
                    toners["Yellow"] = Normalize(val);
            }

            return toners;
        }

        private async Task<int> ObterContadorGenericoAsync(IPEndPoint ep)
        {
            string raw = await QueryOidWithFallback(ep, "1.3.6.1.2.1.43.10.2.1.4.1.1");
            return int.TryParse(raw, out int v) ? v : 0;
        }

        // =====================================================
        // SNMP UTIL
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
