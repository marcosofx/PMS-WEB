using System;
using System.Collections.Generic;
using System.Net;
using Lextm.SharpSnmpLib;
using Lextm.SharpSnmpLib.Messaging;

namespace SNMPPrinterMonitor
{
    class Program
    {
        static void Main()
        {
            Console.WriteLine("=== Leitor SNMP de Impressoras ===");
            Console.Write("Digite o IP da impressora: ");
            string ip = Console.ReadLine()?.Trim() ?? "";
            var endpoint = new IPEndPoint(IPAddress.Parse(ip), 161);
            string community = "public";

            Console.WriteLine();

            // 1️⃣ Modelo e número de série
            string modelo = GetOidValue(endpoint, community, "1.3.6.1.2.1.1.1.0"); // sysDescr
            string serial = GetOidValue(endpoint, community, "1.3.6.1.2.1.43.5.1.1.17.1"); // serial number

            Console.WriteLine($"Modelo: {modelo}");
            Console.WriteLine($"Série: {serial}");

            // 2️⃣ Status geral
            Console.WriteLine("\nStatus Geral:");
            string statusGeral = ObterStatusGeral(endpoint, community);
            Console.WriteLine($"  {statusGeral}");

            // 3️⃣ Níveis de toner
            Console.WriteLine("\nNível de Toner:");
            ObterNiveisToner(endpoint, community);

            // 4️⃣ Contador total
            Console.WriteLine("\nContadores de Páginas:");
            ObterContadores(endpoint, community);

            // 5️⃣ Bandejas
            Console.WriteLine("\nBandejas:");
            ObterBandejas(endpoint, community);

            // 6️⃣ Alertas
            Console.WriteLine("\nAlertas Ativos:");
            var alertas = ObterAlertas(endpoint, community);
            if (alertas.Count == 0)
                Console.WriteLine("  Nenhum alerta ativo.");
            else
                foreach (var a in alertas)
                    Console.WriteLine($"  - {a}");

            Console.WriteLine("\nPressione qualquer tecla para sair...");
            Console.ReadKey();
        }

        static string GetOidValue(IPEndPoint endpoint, string community, string oid)
        {
            try
            {
                var result = Messenger.Get(
                    VersionCode.V1,
                    endpoint,
                    new OctetString(community),
                    new List<Variable> { new Variable(new ObjectIdentifier(oid)) },
                    3000
                );

                if (result.Count > 0)
                    return result[0].Data.ToString();
            }
            catch { }

            return "Não disponível";
        }

        static List<string> ObterAlertas(IPEndPoint endpoint, string community)
        {
            var alertasAtivos = new List<string>();
            try
            {
                var alertas = new List<Variable>();
                Messenger.Walk(VersionCode.V1, endpoint, new OctetString(community),
                    new ObjectIdentifier("1.3.6.1.2.1.43.18.1.1.8.1.1"), alertas, 5000, WalkMode.WithinSubtree);

                foreach (var a in alertas)
                {
                    string desc = a.Data.ToString();
                    if (!string.IsNullOrWhiteSpace(desc))
                        alertasAtivos.Add(desc);
                }
            }
            catch { }

            return alertasAtivos;
        }

        static string ObterStatusGeral(IPEndPoint endpoint, string community)
        {
            string codigoStatus = GetOidValue(endpoint, community, "1.3.6.1.2.1.25.3.5.1.1.1");
            var alertas = ObterAlertas(endpoint, community);

            if (alertas.Count > 0)
                return "Com alerta: " + string.Join(", ", alertas);

            switch (codigoStatus.Trim())
            {
                case "1": return "Em operação";
                case "2": return "Desligada";
                case "3": return "Aguardando";
                case "4": return "Imprimindo";
                case "5": return "Em aquecimento";
                default: return "Status desconhecido";
            }
        }

        static void ObterNiveisToner(IPEndPoint endpoint, string community)
        {
            try
            {
                var descricoes = new List<Variable>();
                var niveis = new List<Variable>();

                Messenger.Walk(VersionCode.V1, endpoint, new OctetString(community),
                    new ObjectIdentifier("1.3.6.1.2.1.43.12.1.1.4"), descricoes, 5000, WalkMode.WithinSubtree);

                Messenger.Walk(VersionCode.V1, endpoint, new OctetString(community),
                    new ObjectIdentifier("1.3.6.1.2.1.43.11.1.1.9"), niveis, 5000, WalkMode.WithinSubtree);

                if (descricoes.Count == 0 || niveis.Count == 0)
                {
                    Console.WriteLine("  Não foi possível obter níveis de toner.");
                    return;
                }

                for (int i = 0; i < descricoes.Count && i < niveis.Count; i++)
                {
                    string cor = descricoes[i].Data.ToString().ToLower();
                    if (!(cor.Contains("black") || cor.Contains("cyan") || cor.Contains("magenta") || cor.Contains("yellow")))
                        continue;

                    string nivelStr = niveis[i].Data.ToString();
                    int nivel = 0;
                    if (int.TryParse(nivelStr, out int val))
                    {
                        if (val > 100 && val <= 255)
                            nivel = (int)((val / 255.0) * 100);
                        else if (val > 255 && val <= 10000)
                            nivel = (int)((val / 10000.0) * 100);
                        else
                            nivel = val;

                        if (nivel > 100) nivel = 100;
                        if (nivel < 0) nivel = 0;
                    }

                    Console.WriteLine($"  {PrimeiraMaiuscula(cor)}: {nivel}%");
                }
            }
            catch
            {
                Console.WriteLine("  Erro ao obter níveis de toner.");
            }
        }

        static void ObterContadores(IPEndPoint endpoint, string community)
        {
            string[] oidsPossiveis = new string[]
            {
                "1.3.6.1.2.1.43.10.2.1.4.1.1",
                "1.3.6.1.2.1.43.10.2.1.4.1.2",
                "1.3.6.1.2.1.43.10.2.1.4.1.3",
                "1.3.6.1.4.1.367.3.2.1.1.4.1.8.0"
            };

            int total = 0;
            foreach (var oid in oidsPossiveis)
            {
                try
                {
                    var result = Messenger.Get(
                        VersionCode.V1,
                        endpoint,
                        new OctetString(community),
                        new List<Variable> { new Variable(new ObjectIdentifier(oid)) },
                        3000
                    );

                    if (result.Count > 0 && int.TryParse(result[0].Data.ToString(), out int val))
                        total += val;
                }
                catch { }
            }

            if (total > 0)
                Console.WriteLine($"  Total: {total}");
            else
                Console.WriteLine("  Contador total não disponível.");
        }

        static void ObterBandejas(IPEndPoint endpoint, string community)
        {
            try
            {
                var bandejas = new List<Variable>();
                Messenger.Walk(VersionCode.V1, endpoint, new OctetString(community),
                    new ObjectIdentifier("1.3.6.1.2.1.43.8.2.1.18"), bandejas, 5000, WalkMode.WithinSubtree); // prtInputStatus

                if (bandejas.Count == 0)
                {
                    Console.WriteLine("  Não foi possível obter informações das bandejas.");
                    return;
                }

                for (int i = 0; i < bandejas.Count; i++)
                {
                    string status = bandejas[i].Data.ToString();
                    Console.WriteLine($"  Bandeja {i + 1}: {InterpretarStatusBandeja(status)}");
                }
            }
            catch
            {
                Console.WriteLine("  Erro ao obter informações das bandejas.");
            }
        }

        static string InterpretarStatusBandeja(string codigo)
        {
            switch (codigo.Trim())
            {
                case "1": return "Pronta";
                case "2": return "Vazia";
                case "3": return "Erro";
                case "4": return "Fora de serviço";
                default: return "Desconhecido";
            }
        }

        static string PrimeiraMaiuscula(string texto)
        {
            if (string.IsNullOrEmpty(texto)) return texto;
            return char.ToUpper(texto[0]) + texto.Substring(1);
        }
    }
}
