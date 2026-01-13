using FinanceSimplify.Data;
using FinanceSimplify.Dtos.Installment;
using FinanceSimplify.Dtos.Invoice;
using FinanceSimplify.Models.Invoice;
using FinanceSimplify.Services.InstallmentService;
using MongoDB.Driver;

namespace FinanceSimplify.Services.InvoiceService {
    public class InvoiceService : IInvoiceInterface {
        private readonly MongoDbContext _context;
        private readonly IInstallmentInterface _installmentService;

        public InvoiceService(MongoDbContext context, IInstallmentInterface installmentService) {
            _context = context;
            _installmentService = installmentService;
        }

        public async Task<InvoiceResponseModel<InvoiceResponseDto>> GenerateInvoiceForCard(Guid cardId, Guid userId, int month, int year) {
            InvoiceResponseModel<InvoiceResponseDto> response = new();

            try {
                // Verificar se já existe fatura para este mês
                var existingInvoice = await _context.Invoices
                    .Find(i => i.CardId == cardId && i.ReferenceMonth == month && i.ReferenceYear == year)
                    .FirstOrDefaultAsync();

                InvoiceModel invoice;
                bool isNewInvoice = false;

                if (existingInvoice != null) {
                    // Fatura já existe, vamos atualizar com novas parcelas
                    invoice = existingInvoice;
                } else {
                    // Criar nova fatura
                    isNewInvoice = true;

                    // Buscar o cartão para pegar os dias de fechamento e vencimento
                    var card = await _context.Cards.Find(c => c.Id == cardId).FirstOrDefaultAsync();
                    if (card == null) {
                        response.Message = "Cartão não encontrado!";
                        response.Status = false;
                        return response;
                    }

                    if (!card.ClosingDay.HasValue || !card.DueDay.HasValue) {
                        response.Message = "Cartão não possui dias de fechamento e vencimento configurados!";
                        response.Status = false;
                        return response;
                    }

                    var closingDate = new DateTime(year, month, card.ClosingDay.Value);
                    var dueDate = new DateTime(year, month, card.DueDay.Value);

                    // Se o vencimento é antes do fechamento, o vencimento é no próximo mês
                    if (card.DueDay.Value < card.ClosingDay.Value) {
                        dueDate = dueDate.AddMonths(1);
                    }

                    invoice = new InvoiceModel {
                        Id = Guid.NewGuid(),
                        CardId = cardId,
                        UserId = userId,
                        ReferenceMonth = month,
                        ReferenceYear = year,
                        ClosingDate = closingDate,
                        DueDate = dueDate,
                        TotalAmount = 0,
                        IsPaid = false,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _context.Invoices.InsertOneAsync(invoice);
                }

                // Buscar parcelas pendentes que vencem no mês/ano desta fatura
                // A parcela pertence à fatura se o mês/ano do vencimento da parcela corresponde ao mês/ano de referência da fatura
                var installments = await _context.Installments
                    .Find(i => i.CardId == cardId && 
                               i.InvoiceId == null &&
                               i.DueDate.Month == month &&
                               i.DueDate.Year == year)
                    .ToListAsync();

                // Atribuir parcelas à fatura
                foreach (var installment in installments) {
                    await _installmentService.AssignInstallmentToInvoice(installment.Id, invoice.Id);
                }

                // Recalcular total da fatura
                var total = await CalculateInvoiceTotal(invoice.Id);
                var update = Builders<InvoiceModel>.Update.Set(i => i.TotalAmount, total);
                await _context.Invoices.UpdateOneAsync(i => i.Id == invoice.Id, update);

                invoice.TotalAmount = total;

                // Buscar todas as parcelas da fatura para retornar
                var allInstallments = await _installmentService.GetInstallmentsByInvoice(invoice.Id);

                response.InvoiceData = new InvoiceResponseDto {
                    Id = invoice.Id,
                    CardId = invoice.CardId,
                    ReferenceMonth = invoice.ReferenceMonth,
                    ReferenceYear = invoice.ReferenceYear,
                    ClosingDate = invoice.ClosingDate,
                    DueDate = invoice.DueDate,
                    TotalAmount = invoice.TotalAmount,
                    IsPaid = invoice.IsPaid,
                    PaidDate = invoice.PaidDate,
                    Installments = allInstallments.Select(i => new InstallmentResponseDto {
                        Id = i.Id,
                        TransactionId = i.TransactionId,
                        CardId = i.CardId,
                        Amount = i.Amount,
                        InstallmentNumber = i.InstallmentNumber,
                        TotalInstallments = i.TotalInstallments,
                        DueDate = i.DueDate,
                        InvoiceId = invoice.Id,
                        IsPaid = i.IsPaid,
                        Description = i.Description
                    }).ToList()
                };

                response.Message = isNewInvoice ? "Fatura gerada com sucesso!" : "Fatura atualizada com novas parcelas!";
                return response;
            }
            catch (Exception ex) {
                response.Message = ex.Message;
                response.Status = false;
                return response;
            }
        }

        public async Task<List<InvoiceModel>> GetInvoicesByCard(Guid cardId, int page, int pageSize) {
            return await _context.Invoices
                .Find(i => i.CardId == cardId)
                .SortByDescending(i => i.ReferenceYear)
                .ThenByDescending(i => i.ReferenceMonth)
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync();
        }

        public async Task<InvoiceResponseModel<InvoiceResponseDto>> GetInvoiceById(Guid invoiceId) {
            InvoiceResponseModel<InvoiceResponseDto> response = new();

            try {
                var invoice = await _context.Invoices.Find(i => i.Id == invoiceId).FirstOrDefaultAsync();

                if (invoice == null) {
                    response.Message = "Fatura não encontrada!";
                    response.Status = false;
                    return response;
                }

                var installments = await _installmentService.GetInstallmentsByInvoice(invoiceId);

                response.InvoiceData = new InvoiceResponseDto {
                    Id = invoice.Id,
                    CardId = invoice.CardId,
                    ReferenceMonth = invoice.ReferenceMonth,
                    ReferenceYear = invoice.ReferenceYear,
                    ClosingDate = invoice.ClosingDate,
                    DueDate = invoice.DueDate,
                    TotalAmount = invoice.TotalAmount,
                    IsPaid = invoice.IsPaid,
                    PaidDate = invoice.PaidDate,
                    Installments = installments.Select(i => new InstallmentResponseDto {
                        Id = i.Id,
                        TransactionId = i.TransactionId,
                        CardId = i.CardId,
                        Amount = i.Amount,
                        InstallmentNumber = i.InstallmentNumber,
                        TotalInstallments = i.TotalInstallments,
                        DueDate = i.DueDate,
                        InvoiceId = i.InvoiceId,
                        IsPaid = i.IsPaid,
                        Description = i.Description
                    }).ToList()
                };

                response.Message = "Fatura encontrada com sucesso!";
                return response;
            }
            catch (Exception ex) {
                response.Message = ex.Message;
                response.Status = false;
                return response;
            }
        }

        public async Task<InvoiceResponseModel<InvoiceResponseDto>> GetCurrentInvoice(Guid cardId) {
            InvoiceResponseModel<InvoiceResponseDto> response = new();

            try {
                var now = DateTime.UtcNow;
                var invoice = await _context.Invoices
                    .Find(i => i.CardId == cardId && 
                               i.ReferenceMonth == now.Month && 
                               i.ReferenceYear == now.Year)
                    .FirstOrDefaultAsync();

                if (invoice == null) {
                    response.Message = "Nenhuma fatura encontrada para o mês atual!";
                    response.Status = false;
                    return response;
                }

                var installments = await _installmentService.GetInstallmentsByInvoice(invoice.Id);

                response.InvoiceData = new InvoiceResponseDto {
                    Id = invoice.Id,
                    CardId = invoice.CardId,
                    ReferenceMonth = invoice.ReferenceMonth,
                    ReferenceYear = invoice.ReferenceYear,
                    ClosingDate = invoice.ClosingDate,
                    DueDate = invoice.DueDate,
                    TotalAmount = invoice.TotalAmount,
                    IsPaid = invoice.IsPaid,
                    PaidDate = invoice.PaidDate,
                    Installments = installments.Select(i => new InstallmentResponseDto {
                        Id = i.Id,
                        TransactionId = i.TransactionId,
                        CardId = i.CardId,
                        Amount = i.Amount,
                        InstallmentNumber = i.InstallmentNumber,
                        TotalInstallments = i.TotalInstallments,
                        DueDate = i.DueDate,
                        InvoiceId = i.InvoiceId,
                        IsPaid = i.IsPaid,
                        Description = i.Description
                    }).ToList()
                };

                response.Message = "Fatura atual encontrada!";
                return response;
            }
            catch (Exception ex) {
                response.Message = ex.Message;
                response.Status = false;
                return response;
            }
        }

        public async Task<InvoiceResponseModel<bool>> MarkInvoiceAsPaid(Guid invoiceId) {
            InvoiceResponseModel<bool> response = new();

            try {
                // Buscar a fatura para pegar o cardId
                var invoice = await _context.Invoices.Find(i => i.Id == invoiceId).FirstOrDefaultAsync();
                
                if (invoice == null) {
                    response.Message = "Fatura não encontrada!";
                    response.Status = false;
                    return response;
                }

                // Marcar fatura como paga
                var invoiceUpdate = Builders<InvoiceModel>.Update
                    .Set(i => i.IsPaid, true)
                    .Set(i => i.PaidDate, DateTime.UtcNow);

                await _context.Invoices.UpdateOneAsync(i => i.Id == invoiceId, invoiceUpdate);

                // Buscar todas as parcelas desta fatura
                var installments = await _installmentService.GetInstallmentsByInvoice(invoiceId);

                // Marcar todas as parcelas como pagas
                var installmentUpdate = Builders<Models.Installment.InstallmentModel>.Update
                    .Set(i => i.IsPaid, true);

                foreach (var installment in installments) {
                    await _context.Installments.UpdateOneAsync(
                        i => i.Id == installment.Id,
                        installmentUpdate
                    );
                }

                // Recalcular e atualizar o limite disponível do cartão
                var card = await _context.Cards.Find(c => c.Id == invoice.CardId).FirstOrDefaultAsync();
                if (card != null && card.CreditLimit.HasValue) {
                    // Buscar todas as parcelas pendentes (não pagas) do cartão
                    var pendingInstallments = await _context.Installments
                        .Find(i => i.CardId == invoice.CardId && !i.IsPaid)
                        .ToListAsync();

                    var usedLimit = pendingInstallments.Sum(i => i.Amount);
                    var availableLimit = card.CreditLimit.Value - usedLimit;

                    var cardUpdate = Builders<Models.Card.CardModel>.Update
                        .Set(c => c.AvailableLimit, availableLimit);

                    await _context.Cards.UpdateOneAsync(c => c.Id == invoice.CardId, cardUpdate);
                }

                response.InvoiceData = true;
                response.Message = "Fatura marcada como paga e limite do cartão atualizado!";
                return response;
            }
            catch (Exception ex) {
                response.Message = ex.Message;
                response.Status = false;
                return response;
            }
        }

        public async Task<decimal> CalculateInvoiceTotal(Guid invoiceId) {
            var installments = await _installmentService.GetInstallmentsByInvoice(invoiceId);
            return installments.Sum(i => i.Amount);
        }
    }
}
