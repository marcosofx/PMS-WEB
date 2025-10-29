using System;
using System.Collections.Generic;
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

        public async Task AtualizarPrinter(Printer printer)
        {
            if (printer == null || string.IsNullOrWhiteSpace(printer.Ip))
                return;

            try
            {
                var endpoint = new IPEndPoint(IPAddress.Parse(printer.Ip), 161);

                printer.Modelo = await GetOidValueAsync(endpoint, "1.3.6.1.2.1.1.1.0"); // sysDescr
                printer.Nome = await GetOidValueAsync(endpoint, ".1.3.6.1.4.1.367.3.2.1.6.1.1.7.1"); // Nome fabricante
                printer.NumeroSerie = await GetOidValueAsync(endpoint, "1.3.6.1.2.1.43.5.1.1.17.1"); // Serial
                printer.Status = await ObterStatusGeralAsync(endpoint);
                printer.Foto = "N/A";

                // Obtém toners
                printer.Toners = await ObterTonersUniversalAsync(endpoint);

                // Detecta colorida: se algum dos toners coloridos tiver nível > 0
                printer.EColorida =
                    (printer.Toners.ContainsKey("Cyan") && printer.Toners["Cyan"] > 0) ||
                    (printer.Toners.ContainsKey("Magenta") && printer.Toners["Magenta"] > 0) ||
                    (printer.Toners.ContainsKey("Yellow") && printer.Toners["Yellow"] > 0);

                printer.ContadorTotal = await ObterContadorTotalAsync(endpoint);
                printer.Bandejas = await ObterBandejasAsync(endpoint);
                printer.Alertas = await ObterAlertasUniversalAsync(endpoint);
            }
            catch
            {
                printer.Modelo ??= "Não disponível";
                printer.Nome ??= "Não disponível";
                printer.NumeroSerie ??= "Não disponível";
                printer.Status ??= "Desconhecido";
                printer.Foto ??= "N/A";
                printer.Toners ??= new Dictionary<string, int> { { "Black", 0 }, { "Cyan", 0 }, { "Magenta", 0 }, { "Yellow", 0 } };
                printer.ContadorTotal = 0;
                printer.Bandejas ??= new Dictionary<string, string>();
                printer.Alertas ??= new List<string>();
                printer.EColorida = false;
            }
        }

        private async Task<string> GetOidValueAsync(IPEndPoint endpoint, string oid)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var result = Messenger.Get(
                        VersionCode.V2,
                        endpoint,
                        new OctetString(_community),
                        new List<Variable> { new Variable(new ObjectIdentifier(oid)) },
                        3000
                    );

                    if (result.Count > 0)
                        return result[0].Data.ToString();
                }
                catch { }

                return "Não disponível";
            });
        }

        private async Task<string> ObterStatusGeralAsync(IPEndPoint endpoint)
        {
            string status = await GetOidValueAsync(endpoint, "1.3.6.1.2.1.25.3.5.1.1.1");
            return status.Trim() switch
            {
                "1" => "Em operação",
                "2" => "Desligada",
                "3" => "Aguardando",
                "4" => "Imprimindo",
                "5" => "Em aquecimento",
                _ => "Desconhecido",
            };
        }

        private async Task<Dictionary<string, int>> ObterTonersUniversalAsync(IPEndPoint endpoint)
        {
            var toners = new Dictionary<string, int>
            {
                { "Black", 0 },
                { "Cyan", 0 },
                { "Magenta", 0 },
                { "Yellow", 0 }
            };

            var descricoes = new List<Variable>();
            var niveis = new List<Variable>();

            // OIDs universais
            string oidDescricao = "1.3.6.1.2.1.43.12.1.1.4";
            string oidNivel = "1.3.6.1.2.1.43.11.1.1.9";

            // OIDs Ricoh específicos
            string oidDescricaoColor = "1.3.6.1.4.1.367.3.2.1.2.24.1.1.2";
            string oidNivelColor = "1.3.6.1.4.1.367.3.2.1.2.24.1.1.3";

            // OID PB
            string oidPB = "1.3.6.1.2.1.43.11.1.1.6.1.1";

            try
            {
                // Tenta OIDs universais
                Messenger.Walk(VersionCode.V2, endpoint, new OctetString(_community),
                    new ObjectIdentifier(oidDescricao), descricoes, 5000, WalkMode.WithinSubtree);
                Messenger.Walk(VersionCode.V2, endpoint, new OctetString(_community),
                    new ObjectIdentifier(oidNivel), niveis, 5000, WalkMode.WithinSubtree);

                // Se nada retornou, tenta OIDs Ricoh
                if (descricoes.Count == 0 || niveis.Count == 0)
                {
                    descricoes.Clear();
                    niveis.Clear();

                    Messenger.Walk(VersionCode.V2, endpoint, new OctetString(_community),
                        new ObjectIdentifier(oidDescricaoColor), descricoes, 5000, WalkMode.WithinSubtree);
                    Messenger.Walk(VersionCode.V2, endpoint, new OctetString(_community),
                        new ObjectIdentifier(oidNivelColor), niveis, 5000, WalkMode.WithinSubtree);
                }

                // Preenche o dicionário
                for (int i = 0; i < descricoes.Count && i < niveis.Count; i++)
                {
                    string cor = descricoes[i].Data.ToString().ToLower();
                    int nivel = 0;
                    int.TryParse(niveis[i].Data.ToString(), out nivel);

                    // Normaliza valores
                    if (nivel > 100 && nivel <= 255) nivel = (int)((nivel / 255.0) * 100);
                    else if (nivel > 255 && nivel <= 10000) nivel = (int)((nivel / 10000.0) * 100);
                    nivel = Math.Clamp(nivel, 0, 100);

                    if (cor.Contains("black") || cor.Contains("preto") || cor.Contains("k")) toners["Black"] = nivel;
                    else if (cor.Contains("cyan") || cor.Contains("c")) toners["Cyan"] = nivel;
                    else if (cor.Contains("magenta") || cor.Contains("m")) toners["Magenta"] = nivel;
                    else if (cor.Contains("yellow") || cor.Contains("amarelo") || cor.Contains("y")) toners["Yellow"] = nivel;
                }

                // Caso PB sem cores
                if (toners["Black"] == 0)
                {
                    var pbVars = new List<Variable>();
                    Messenger.Walk(VersionCode.V2, endpoint, new OctetString(_community),
                        new ObjectIdentifier(oidPB), pbVars, 5000, WalkMode.WithinSubtree);

                    if (pbVars.Count > 0)
                    {
                        int nivelPB = 0;
                        int.TryParse(pbVars[0].Data.ToString(), out nivelPB);

                        if (nivelPB > 100 && nivelPB <= 255) nivelPB = (int)((nivelPB / 255.0) * 100);
                        else if (nivelPB > 255 && nivelPB <= 10000) nivelPB = (int)((nivelPB / 10000.0) * 100);

                        toners["Black"] = Math.Clamp(nivelPB, 0, 100);
                    }
                }
            }
            catch
            {
                // retorna toners zerados em caso de erro
            }

            return toners;
        }

        private async Task<List<string>> ObterAlertasUniversalAsync(IPEndPoint endpoint)
        {
            var alertas = new List<string>();
            var vars = new List<Variable>();

            string[] alertOids = {
                "1.3.6.1.2.1.43.18.1.1.8",
                "1.3.6.1.4.1.367.3.2.1.3.8.1.1.8",
                "1.3.6.1.4.1.23.2.2.140",
                "1.3.6.1.4.1.1248.1.2.2.1.1.8"
            };

            foreach (var oid in alertOids)
            {
                try
                {
                    Messenger.Walk(VersionCode.V2, endpoint, new OctetString(_community),
                        new ObjectIdentifier(oid), vars, 5000, WalkMode.WithinSubtree);
                    if (vars.Count > 0) break;
                }
                catch { }
            }

            foreach (var v in vars)
            {
                var texto = v.Data.ToString().Trim();
                if (!string.IsNullOrWhiteSpace(texto))
                    alertas.Add(texto);
            }

            return alertas;
        }

        private async Task<int> ObterContadorTotalAsync(IPEndPoint endpoint)
        {
            string[] oidsPossiveis = {
                "1.3.6.1.2.1.43.10.2.1.4.1.1",
                "1.3.6.1.4.1.367.3.2.1.1.4.1.8.0",
                "1.3.6.1.4.1.23.2.2.133.1.1.4.1",
                "1.3.6.1.4.1.1248.1.2.2.1.1.4"
            };

            int total = 0;
            foreach (var oid in oidsPossiveis)
            {
                try
                {
                    var result = Messenger.Get(
                        VersionCode.V2,
                        endpoint,
                        new OctetString(_community),
                        new List<Variable> { new Variable(new ObjectIdentifier(oid)) },
                        3000
                    );

                    if (result.Count > 0 && int.TryParse(result[0].Data.ToString(), out int val))
                        total += val;
                }
                catch { }
            }

            return total;
        }

        private async Task<Dictionary<string, string>> ObterBandejasAsync(IPEndPoint endpoint)
        {
            var bandejas = new Dictionary<string, string>();
            var vars = new List<Variable>();

            try
            {
                Messenger.Walk(VersionCode.V2, endpoint, new OctetString(_community),
                    new ObjectIdentifier("1.3.6.1.2.1.43.8.2.1.18"), vars, 5000, WalkMode.WithinSubtree);

                for (int i = 0; i < vars.Count; i++)
                {
                    string status = vars[i].Data.ToString();
                    bandejas[$"Bandeja {i + 1}"] = status switch
                    {
                        "1" => "Pronta",
                        "2" => "Vazia",
                        "3" => "Erro",
                        "4" => "Fora de serviço",
                        _ => "Desconhecido"
                    };
                }
            }
            catch { }

            return bandejas;
        }
    }
}
