using System.Globalization;
using System.Text.Json;
using FinanceSimplify.Data;
using FinanceSimplify.Dtos.Investment;
using FinanceSimplify.Models.Investment;
using MongoDB.Driver;

namespace FinanceSimplify.Services.InvestmentService {
    public class InvestmentService : IInvestmentInterface {
        private readonly MongoDbContext _context;
        private readonly HttpClient _httpClient;
        private readonly ILogger<InvestmentService> _logger;

        // Taxa oficial da Meta Selic padrão (13.75% a.a.) caso a API do BCB falhe
        private const decimal DefaultCdiRate = 13.75m;
        private static decimal? _cachedCdiRate = null;
        private static DateTime _cacheTimestamp = DateTime.MinValue;

        public InvestmentService(MongoDbContext context, HttpClient httpClient, ILogger<InvestmentService> logger) {
            _context = context;
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<decimal> GetCurrentCdiRate() {
            // Cache de 2 horas para a taxa Selic/CDI oficial
            if (_cachedCdiRate.HasValue && (DateTime.UtcNow - _cacheTimestamp).TotalHours < 2) {
                return _cachedCdiRate.Value;
            }

            try {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.bcb.gov.br/dados/serie/bcdata.sgs.432/dados/ultimos/1?formato=json");
                request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");

                var response = await _httpClient.SendAsync(request, cts.Token);
                if (response.IsSuccessStatusCode) {
                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.ValueKind == JsonValueKind.Array && doc.RootElement.GetArrayLength() > 0) {
                        var first = doc.RootElement[0];
                        if (first.TryGetProperty("valor", out var valProp)) {
                            var valStr = valProp.GetString();
                            if (decimal.TryParse(valStr?.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var rate) && rate > 0) {
                                _cachedCdiRate = rate;
                                _cacheTimestamp = DateTime.UtcNow;
                                _logger.LogInformation("Taxa Selic/CDI oficial obtida do Governo (BCB SGS 432): {Rate}% a.a.", rate);
                                return rate;
                            }
                        }
                    }
                }
            } catch (Exception ex) {
                _logger.LogWarning("Não foi possível buscar Selic/CDI da API do BCB: {Message}. Usando taxa oficial padrão {Default}%.", ex.Message, DefaultCdiRate);
            }

            _cachedCdiRate = DefaultCdiRate;
            _cacheTimestamp = DateTime.UtcNow;
            return DefaultCdiRate;
        }

        public async Task<InvestmentResponseModel<InvestmentResponseDto>> CreateInvestment(Guid userId, InvestmentCreateDto dto) {
            var response = new InvestmentResponseModel<InvestmentResponseDto>();

            try {
                if (string.IsNullOrWhiteSpace(dto.Name)) {
                    response.Status = false;
                    response.Message = "O nome da caixinha / investimento é obrigatório.";
                    return response;
                }

                if (dto.InitialAmount <= 0) {
                    response.Status = false;
                    response.Message = "O valor investido deve ser maior que zero.";
                    return response;
                }

                var cdbRate = dto.CdbRate <= 0 ? 100 : dto.CdbRate;
                var startDate = dto.StartDate == default ? DateTime.UtcNow : DateTime.SpecifyKind(dto.StartDate, DateTimeKind.Utc);

                var model = new InvestmentModel {
                    UserId = userId,
                    Name = dto.Name.Trim(),
                    InitialAmount = dto.InitialAmount,
                    StartDate = startDate,
                    CdbRate = cdbRate,
                    Bank = string.IsNullOrWhiteSpace(dto.Bank) ? "Nubank" : dto.Bank.Trim(),
                    Type = "CDB",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _context.Investments.InsertOneAsync(model);

                var cdi = await GetCurrentCdiRate();
                response.InvestmentData = CalculateInvestment(model, cdi);
                response.Message = "Caixinha / CDB cadastrado com sucesso!";
                response.Status = true;
            } catch (Exception ex) {
                response.Status = false;
                response.Message = $"Erro ao cadastrar investimento: {ex.Message}";
            }

            return response;
        }

        public async Task<InvestmentResponseModel<InvestmentSummaryDto>> GetInvestmentsByUserId(Guid userId, decimal? customCdi = null) {
            var response = new InvestmentResponseModel<InvestmentSummaryDto>();

            try {
                var cdi = customCdi.HasValue && customCdi.Value > 0 ? customCdi.Value : await GetCurrentCdiRate();
                var list = await _context.Investments.Find(x => x.UserId == userId).ToListAsync();

                var calculatedList = list.Select(inv => CalculateInvestment(inv, cdi)).OrderByDescending(x => x.GrossAmount).ToList();

                var summary = new InvestmentSummaryDto {
                    TotalInvested = calculatedList.Sum(x => x.InitialAmount),
                    TotalGrossAmount = calculatedList.Sum(x => x.GrossAmount),
                    TotalGrossEarnings = calculatedList.Sum(x => x.GrossEarnings),
                    TotalTaxAmount = calculatedList.Sum(x => x.TaxAmount),
                    TotalNetAmount = calculatedList.Sum(x => x.NetAmount),
                    TotalNetEarnings = calculatedList.Sum(x => x.NetEarnings),
                    ReferenceAnnualCdi = cdi,
                    Investments = calculatedList
                };

                if (summary.TotalInvested > 0) {
                    summary.GlobalNetReturnPercentage = Math.Round((summary.TotalNetEarnings / summary.TotalInvested) * 100m, 2);
                }

                response.InvestmentData = summary;
                response.Status = true;
                response.Message = "Investimentos listados com sucesso.";
            } catch (Exception ex) {
                response.Status = false;
                response.Message = $"Erro ao carregar investimentos: {ex.Message}";
            }

            return response;
        }

        public async Task<InvestmentResponseModel<InvestmentResponseDto>> GetInvestmentById(Guid id, decimal? customCdi = null) {
            var response = new InvestmentResponseModel<InvestmentResponseDto>();

            try {
                var model = await _context.Investments.Find(x => x.Id == id).FirstOrDefaultAsync();
                if (model == null) {
                    response.Status = false;
                    response.Message = "Investimento não encontrado.";
                    return response;
                }

                var cdi = customCdi.HasValue && customCdi.Value > 0 ? customCdi.Value : await GetCurrentCdiRate();
                response.InvestmentData = CalculateInvestment(model, cdi);
                response.Status = true;
            } catch (Exception ex) {
                response.Status = false;
                response.Message = $"Erro ao buscar investimento: {ex.Message}";
            }

            return response;
        }

        public async Task<InvestmentResponseModel<InvestmentResponseDto>> UpdateInvestment(Guid id, InvestmentEditDto dto) {
            var response = new InvestmentResponseModel<InvestmentResponseDto>();

            try {
                var existing = await _context.Investments.Find(x => x.Id == id).FirstOrDefaultAsync();
                if (existing == null) {
                    response.Status = false;
                    response.Message = "Investimento não encontrado.";
                    return response;
                }

                if (!string.IsNullOrWhiteSpace(dto.Name)) existing.Name = dto.Name.Trim();
                if (dto.InitialAmount > 0) existing.InitialAmount = dto.InitialAmount;
                if (dto.StartDate != default) existing.StartDate = DateTime.SpecifyKind(dto.StartDate, DateTimeKind.Utc);
                if (dto.CdbRate > 0) existing.CdbRate = dto.CdbRate;
                if (!string.IsNullOrWhiteSpace(dto.Bank)) existing.Bank = dto.Bank.Trim();
                existing.UpdatedAt = DateTime.UtcNow;

                await _context.Investments.ReplaceOneAsync(x => x.Id == id, existing);

                var cdi = await GetCurrentCdiRate();
                response.InvestmentData = CalculateInvestment(existing, cdi);
                response.Status = true;
                response.Message = "Investimento atualizado com sucesso!";
            } catch (Exception ex) {
                response.Status = false;
                response.Message = $"Erro ao atualizar investimento: {ex.Message}";
            }

            return response;
        }

        public async Task<InvestmentResponseModel<bool>> DeleteInvestment(Guid id) {
            var response = new InvestmentResponseModel<bool>();

            try {
                var result = await _context.Investments.DeleteOneAsync(x => x.Id == id);
                if (result.DeletedCount > 0) {
                    response.InvestmentData = true;
                    response.Status = true;
                    response.Message = "Caixinha / Investimento excluído com sucesso!";
                } else {
                    response.Status = false;
                    response.Message = "Investimento não encontrado para exclusão.";
                }
            } catch (Exception ex) {
                response.Status = false;
                response.Message = $"Erro ao excluir investimento: {ex.Message}";
            }

            return response;
        }

        /// <summary>
        /// Calcula o rendimento de um CDB pós-fixado indexado ao CDI com aplicação da tabela regressiva de IR.
        /// </summary>
        public static InvestmentResponseDto CalculateInvestment(InvestmentModel model, decimal annualCdiRate) {
            var today = DateTime.UtcNow.Date;
            var start = model.StartDate.Date;

            // Dias corridos entre a data inicial e hoje
            var elapsedDays = Math.Max(0, (today - start).Days);

            // Dias úteis (252 dias/ano na convenção B3/ANBIMA para CDI)
            var businessDays = 0;
            if (elapsedDays > 0) {
                var cur = start;
                while (cur < today) {
                    cur = cur.AddDays(1);
                    if (cur.DayOfWeek != DayOfWeek.Saturday && cur.DayOfWeek != DayOfWeek.Sunday) {
                        businessDays++;
                    }
                }
            }

            decimal grossAmount = model.InitialAmount;
            decimal grossEarnings = 0;

            if (businessDays > 0 && annualCdiRate > 0) {
                // Taxa diária do CDI base 252: (1 + CDI_anual)^(1/252) - 1
                var annualRateDecimal = (double)(annualCdiRate / 100m);
                var cdiDaily = Math.Pow(1.0 + annualRateDecimal, 1.0 / 252.0) - 1.0;

                // Fator diário do CDB ajustado pelo percentual do CDI (ex: 100%, 110%)
                var cdbRateMultiplier = (double)(model.CdbRate / 100m);
                var dailyYield = cdiDaily * cdbRateMultiplier;

                // Fator acumulado no período de dias úteis
                var accumulatedFactor = Math.Pow(1.0 + dailyYield, businessDays);

                grossAmount = Math.Round(model.InitialAmount * (decimal)accumulatedFactor, 2);
                grossEarnings = Math.Max(0, grossAmount - model.InitialAmount);
            }

            // Tabela Regressiva de IR sobre Renda Fixa / CDB (Lei 11.033/2004)
            // Até 180 dias corridos: 22.5%
            // De 181 a 360 dias: 20.0%
            // De 361 a 720 dias: 17.5%
            // Acima de 720 dias: 15.0%
            decimal taxRate = elapsedDays switch {
                <= 180 => 0.225m,
                <= 360 => 0.200m,
                <= 720 => 0.175m,
                _ => 0.150m
            };

            var taxAmount = Math.Round(grossEarnings * taxRate, 2);
            var netEarnings = Math.Round(grossEarnings - taxAmount, 2);
            var netAmount = Math.Round(model.InitialAmount + netEarnings, 2);
            var netReturnPercentage = model.InitialAmount > 0 
                ? Math.Round((netEarnings / model.InitialAmount) * 100m, 2) 
                : 0m;

            return new InvestmentResponseDto {
                Id = model.Id,
                UserId = model.UserId,
                Name = model.Name,
                InitialAmount = model.InitialAmount,
                StartDate = model.StartDate,
                CdbRate = model.CdbRate,
                Bank = model.Bank,
                Type = model.Type,
                CreatedAt = model.CreatedAt,
                ElapsedDays = elapsedDays,
                BusinessDays = businessDays,
                ReferenceAnnualCdi = annualCdiRate,
                GrossEarnings = grossEarnings,
                GrossAmount = grossAmount,
                TaxRate = taxRate * 100m,
                TaxAmount = taxAmount,
                NetEarnings = netEarnings,
                NetAmount = netAmount,
                NetReturnPercentage = netReturnPercentage
            };
        }
    }
}
