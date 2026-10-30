namespace FinanceSimplify.Dtos.Investment {
    public class InvestmentCreateDto {
        public required string Name { get; set; }
        public decimal InitialAmount { get; set; }
        public DateTime StartDate { get; set; }
        public decimal CdbRate { get; set; } = 100; // % do CDI (ex: 100%, 110%)
        public string? Bank { get; set; }
    }

    public class InvestmentEditDto {
        public required string Name { get; set; }
        public decimal InitialAmount { get; set; }
        public DateTime StartDate { get; set; }
        public decimal CdbRate { get; set; } = 100;
        public string? Bank { get; set; }
    }

    public class InvestmentResponseDto {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal InitialAmount { get; set; }
        public DateTime StartDate { get; set; }
        public decimal CdbRate { get; set; }
        public string Bank { get; set; } = string.Empty;
        public string Type { get; set; } = "CDB";
        public DateTime CreatedAt { get; set; }

        // Indicadores e Cálculos em tempo real
        public int ElapsedDays { get; set; }            // Dias corridos decorridos
        public int BusinessDays { get; set; }           // Dias úteis trabalhados
        public decimal ReferenceAnnualCdi { get; set; } // CDI anual de referência (% a.a.)
        public decimal GrossEarnings { get; set; }       // Rendimento bruto (R$)
        public decimal GrossAmount { get; set; }         // Saldo bruto acumulado (R$)
        public decimal TaxRate { get; set; }            // Alíquota de IR (ex: 22.5%, 20%, 17.5%, 15%)
        public decimal TaxAmount { get; set; }          // Valor descontado de IR (R$)
        public decimal NetEarnings { get; set; }         // Rendimento líquido no bolso (R$)
        public decimal NetAmount { get; set; }           // Saldo líquido resgatável (R$)
        public decimal NetReturnPercentage { get; set; } // Rentabilidade líquida acumulada (%)
    }

    public class InvestmentSummaryDto {
        public decimal TotalInvested { get; set; }       // Total aportado inicialmente
        public decimal TotalGrossAmount { get; set; }     // Total bruto acumulado hoje
        public decimal TotalGrossEarnings { get; set; }   // Total de rendimento bruto
        public decimal TotalTaxAmount { get; set; }       // Total de IR descontado
        public decimal TotalNetAmount { get; set; }       // Total líquido disponível
        public decimal TotalNetEarnings { get; set; }     // Total de lucro líquido
        public decimal GlobalNetReturnPercentage { get; set; } // Rentabilidade líquida geral (%)
        public decimal ReferenceAnnualCdi { get; set; }   // Taxa CDI referência
        public List<InvestmentResponseDto> Investments { get; set; } = new();
    }
}
