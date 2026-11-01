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
                // Verificar se jÃ¡ existe fatura para este mÃªs
                var existingInvoice = await _context.Invoices
                    .Find(i => i.CardId == cardId && i.ReferenceMonth == month && i.ReferenceYear == year)
                    .FirstOrDefaultAsync();

                InvoiceModel invoice;
                bool isNewInvoice = false;

                if (existingInvoice != null) {
                    // Fatura jÃ¡ existe, vamos atualizar com novas parcelas
                    invoice = existingInvoice;
                } else {
                    // Criar nova fatura
                    isNewInvoice = true;

                    // Buscar o cartÃ£o para pegar os dias de fechamento e vencimento
                    var card = await _context.Cards.Find(c => c.Id == cardId).FirstOrDefaultAsync();
                    if (card == null) {
                        response.Message = "CartÃ£o nÃ£o encontrado!";
                        response.Status = false;
                        return response;
                    }

                    if (!card.ClosingDay.HasValue || !card.DueDay.HasValue) {
                        response.Message = "CartÃ£o nÃ£o possui dias de fechamento e vencimento configurados!";
                        response.Status = false;
                        return response;
                    }

                    var closingDate = new DateTime(year, month, card.ClosingDay.Value);
                    var dueDate = new DateTime(year, month, card.DueDay.Value);

                    // Se o vencimento Ã© antes do fechamento, o vencimento Ã© no prÃ³ximo mÃªs
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

                // Buscar parcelas pendentes que vencem no mÃªs/ano desta fatura
                // A parcela pertence Ã  fatura se o mÃªs/ano do vencimento da parcela corresponde ao mÃªs/ano de referÃªncia da fatura
                var installments = await _context.Installments
                    .Find(i => i.CardId == cardId && 
                               i.InvoiceId == null &&
                               i.DueDate.Month == month &&
                               i.DueDate.Year == year)
                    .ToListAsync();

                // Atribuir parcelas Ã  fatura
                foreach (var installment in installments) {
                    await _installmentService.AssignInstallmentToInvoice(installment.Id, invoice.Id);
                }

                // Recalcular total da fatura
                var total = await CalculateInvoiceTotal(invoice.Id);
                var update = Builders<InvoiceModel>.Update.Set(i => i.TotalAmount, total);
                await _context.Invoices.UpdateOneAsync(i => i.Id == invoice.Id, update);

                invoice.TotalAmount = total;
                await SyncInvoiceTransaction(invoice.Id);

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
                    response.Message = "Fatura nÃ£o encontrada!";
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
                    response.Message = "Nenhuma fatura encontrada para o mÃªs atual!";
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
                    response.Message = "Fatura nÃ£o encontrada!";
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

                // Recalcular e atualizar o limite disponÃ­vel do cartÃ£o
                var card = await _context.Cards.Find(c => c.Id == invoice.CardId).FirstOrDefaultAsync();
                if (card != null && card.CreditLimit.HasValue) {
                    // Buscar todas as parcelas pendentes (nÃ£o pagas) do cartÃ£o
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
                response.Message = "Fatura marcada como paga e limite do cartÃ£o atualizado!";
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

        public async Task SyncInvoiceTransaction(Guid invoiceId) {
            try {
                var invoice = await _context.Invoices.Find(i => i.Id == invoiceId).FirstOrDefaultAsync();
                if (invoice == null) return;

                var card = await _context.Cards.Find(c => c.Id == invoice.CardId).FirstOrDefaultAsync();
                var cardName = card?.Name ?? "Cartão";

                var invoiceTransaction = await _context.Transactions
                    .Find(t => t.InvoiceId == invoice.Id)
                    .FirstOrDefaultAsync();

                if (invoice.TotalAmount <= 0) {
                    if (invoiceTransaction != null) {
                        await _context.Transactions.DeleteOneAsync(t => t.Id == invoiceTransaction.Id);
                    }
                    return;
                }

                if (invoiceTransaction == null) {
                    invoiceTransaction = new Models.Transaction.TransactionModel {
                        Id = Guid.NewGuid(),
                        Name = $"Fatura - {cardName}",
                        Amount = invoice.TotalAmount,
                        Date = invoice.DueDate,
                        Type = Enum.TypeTransactionEnum.Despesa,
                        PaymentMethod = Enum.TypePaymentMethodEnum.Credito,
                        CardId = invoice.CardId,
                        BankAccountId = card?.BankAccountId,
                        UserId = invoice.UserId,
                        InvoiceId = invoice.Id
                    };
                    await _context.Transactions.InsertOneAsync(invoiceTransaction);
                } else {
                    var update = Builders<Models.Transaction.TransactionModel>.Update
                        .Set(t => t.Name, $"Fatura - {cardName}")
                        .Set(t => t.Amount, invoice.TotalAmount)
                        .Set(t => t.Date, invoice.DueDate)
                        .Set(t => t.BankAccountId, card?.BankAccountId);
                    await _context.Transactions.UpdateOneAsync(t => t.Id == invoiceTransaction.Id, update);
                }
            }
            catch {
                // Silently continue if non-critical sync issue
            }
        }

        public async Task SyncAllInvoicesForUser(Guid userId) {
            try {
                var invoices = await _context.Invoices.Find(i => i.UserId == userId).ToListAsync();
                foreach (var invoice in invoices) {
                    var total = await CalculateInvoiceTotal(invoice.Id);
                    if (total != invoice.TotalAmount) {
                        invoice.TotalAmount = total;
                        await _context.Invoices.UpdateOneAsync(
                            i => i.Id == invoice.Id,
                            Builders<InvoiceModel>.Update.Set(i => i.TotalAmount, total)
                        );
                    }
                    await SyncInvoiceTransaction(invoice.Id);
                }
            }
            catch {
                // Silently continue
            }
        }
    }
}
