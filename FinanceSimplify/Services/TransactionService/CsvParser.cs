using System.Globalization;
using System.Text.RegularExpressions;

namespace FinanceSimplify.Services.TransactionService {
    public static class CsvParser {
        
        /// <summary>
        /// Faz parse de uma linha CSV considerando aspas
        /// </summary>
        public static List<string> ParseCsvLine(string line) {
            var fields = new List<string>();
            var currentField = "";
            bool inQuotes = false;
            
            for (int i = 0; i < line.Length; i++) {
                char c = line[i];
                
                if (c == '"') {
                    inQuotes = !inQuotes;
                } else if (c == ',' && !inQuotes) {
                    fields.Add(currentField.Trim());
                    currentField = "";
                } else {
                    currentField += c;
                }
            }
            
            // Adiciona o último campo
            fields.Add(currentField.Trim());
            
            return fields;
        }
        
        /// <summary>
        /// Converte valor brasileiro "R$ 1.234,56" para decimal
        /// </summary>
        public static decimal ParseBrazilianCurrency(string value) {
            if (string.IsNullOrWhiteSpace(value)) {
                throw new ArgumentException("Valor não pode ser vazio");
            }
            
            // Remove "R$", espaços e pontos (separador de milhar)
            string cleanValue = value
                .Replace("R$", "")
                .Replace(" ", "")
                .Replace(".", "")
                .Trim();
            
            // Troca vírgula por ponto (separador decimal)
            cleanValue = cleanValue.Replace(",", ".");
            
            if (decimal.TryParse(cleanValue, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal result)) {
                return result;
            }
            
            throw new FormatException($"Não foi possível converter o valor: {value}");
        }
        
        /// <summary>
        /// Converte data brasileira "DD/MM/YYYY" para DateTime
        /// </summary>
        public static DateTime ParseBrazilianDate(string date) {
            if (string.IsNullOrWhiteSpace(date)) {
                throw new ArgumentException("Data não pode ser vazia");
            }
            
            string[] formats = { "dd/MM/yyyy", "d/M/yyyy", "dd/M/yyyy", "d/MM/yyyy" };
            
            if (DateTime.TryParseExact(date.Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result)) {
                return result;
            }
            
            throw new FormatException($"Não foi possível converter a data: {date}");
        }
        
        /// <summary>
        /// Extrai informação de parcela do campo "Tipo"
        /// Retorna (numeroParcela, totalParcelas) ou (0, 0) se for compra à vista
        /// </summary>
        public static (int currentInstallment, int totalInstallments) ExtractInstallmentInfo(string tipo) {
            if (string.IsNullOrWhiteSpace(tipo)) {
                return (0, 0);
            }
            
            // Regex para capturar "Parcela X/Y"
            var match = Regex.Match(tipo, @"Parcela\s+(\d+)/(\d+)", RegexOptions.IgnoreCase);
            
            if (match.Success) {
                int current = int.Parse(match.Groups[1].Value);
                int total = int.Parse(match.Groups[2].Value);
                return (current, total);
            }
            
            // Se não encontrou padrão de parcela, é compra à vista
            return (0, 0);
        }
        
        /// <summary>
        /// Valida se a linha tem o número esperado de campos
        /// </summary>
        public static bool IsValidCsvLine(List<string> fields, int expectedFieldCount = 5) {
            return fields != null && fields.Count == expectedFieldCount;
        }
    }
}
