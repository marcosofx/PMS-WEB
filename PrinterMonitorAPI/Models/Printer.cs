using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Text.Json.Serialization;


namespace PrinterMonitorAPI.Models
{
    public class Printer
    {
        // ===============================
        // Dados básicos
        // ===============================

        public Guid Id { get; set; }
        public string Ip { get; set; } = "";
        public string Nome { get; set; } = "";
        public string NomeCustomizado { get; set; } = "";
        public string Descricao { get; set; } = "";
        public string Modelo { get; set; } = "";
        public string NumeroSerie { get; set; } = "";
        public string Status { get; set; } = "";
        public int ContadorTotal { get; set; } = 0;
        public bool EColorida { get; set; } = false;

        // Marca adicionada recentemente
        public string Marca { get; set; } = "";

        // Foto da impressora (padronizado)
        public string? imagemUrl { get; set; }


        // ===============================
        // Campos complexos
        // ===============================

        // Toners: key = cor, value = porcentagem
        public Dictionary<string, int> Toners { get; set; } = new Dictionary<string, int>
        {
            { "Black", 0 },
            { "Cyan", 0 },
            { "Magenta", 0 },
            { "Yellow", 0 }
        };

        // Bandejas: key = nome, value = nível/status
        public Dictionary<string, string> Bandejas { get; set; } = new Dictionary<string, string>();

        // Lista de alertas ativos
        public List<string> Alertas { get; set; } = new List<string>();

        // ===============================
        // Configuração EF Core
        // ===============================
        public static void Configure(ModelBuilder builder)
        {
            var options = new JsonSerializerOptions();

            // --------------------------
            // Conversores
            // --------------------------

            var dictIntConverter = new ValueConverter<Dictionary<string, int>, string>(
                v => JsonSerializer.Serialize(v, options),
                v => JsonSerializer.Deserialize<Dictionary<string, int>>(v, options) ?? new Dictionary<string, int>()
            );

            var dictStringConverter = new ValueConverter<Dictionary<string, string>, string>(
                v => JsonSerializer.Serialize(v, options),
                v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, options) ?? new Dictionary<string, string>()
            );

            var listStringConverter = new ValueConverter<List<string>, string>(
                v => JsonSerializer.Serialize(v, options),
                v => JsonSerializer.Deserialize<List<string>>(v, options) ?? new List<string>()
            );

            // --------------------------
            // Comparadores (permitem EF detectar mudanças corretamente)
            // --------------------------

            var dictIntComparer = new ValueComparer<Dictionary<string, int>>(
                (c1, c2) => (c1 ?? new Dictionary<string, int>()).SequenceEqual(c2 ?? new Dictionary<string, int>()),
                c => (c ?? new Dictionary<string, int>()).Aggregate(0, (a, v) => HashCode.Combine(a, v.Key.GetHashCode(), v.Value.GetHashCode())),
                c => (c ?? new Dictionary<string, int>()).ToDictionary(entry => entry.Key, entry => entry.Value)
            );

            var dictStringComparer = new ValueComparer<Dictionary<string, string>>(
                (c1, c2) => (c1 ?? new Dictionary<string, string>()).SequenceEqual(c2 ?? new Dictionary<string, string>()),
                c => (c ?? new Dictionary<string, string>()).Aggregate(0, (a, v) => HashCode.Combine(a, v.Key.GetHashCode(), v.Value.GetHashCode())),
                c => (c ?? new Dictionary<string, string>()).ToDictionary(entry => entry.Key, entry => entry.Value)
            );

            var listStringComparer = new ValueComparer<List<string>>(
                (l1, l2) => (l1 ?? new List<string>()).SequenceEqual(l2 ?? new List<string>()),
                l => (l ?? new List<string>()).Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                l => (l ?? new List<string>()).ToList()
            );

            // --------------------------
            // Aplicação ao EF Core
            // --------------------------

            builder.Entity<Printer>()
                .Property(p => p.Toners)
                .HasConversion(dictIntConverter)
                .Metadata.SetValueComparer(dictIntComparer);

            builder.Entity<Printer>()
                .Property(p => p.Bandejas)
                .HasConversion(dictStringConverter)
                .Metadata.SetValueComparer(dictStringComparer);

            builder.Entity<Printer>()
                .Property(p => p.Alertas)
                .HasConversion(listStringConverter)
                .Metadata.SetValueComparer(listStringComparer);
        }
    }
}
