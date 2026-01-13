using FinanceSimplify.Data;
using FinanceSimplify.Dtos.Installment;
using FinanceSimplify.Models.Installment;
using MongoDB.Driver;

namespace FinanceSimplify.Services.InstallmentService {
    public class InstallmentService : IInstallmentInterface {
        private readonly MongoDbContext _context;

        public InstallmentService(MongoDbContext context) {
            _context = context;
        }

        public async Task<InstallmentResponseModel<List<InstallmentResponseDto>>> CreateInstallmentsForTransaction(
            Guid transactionId,
            Guid cardId,
            Guid userId,
            decimal totalAmount,
            int totalInstallments,
            DateTime firstDueDate,
            string description,
            int cardClosingDay
        ) {
            InstallmentResponseModel<List<InstallmentResponseDto>> response = new();

            try {
                var installments = new List<InstallmentModel>();
                var installmentAmount = Math.Round(totalAmount / totalInstallments, 2);
                var remainder = totalAmount - (installmentAmount * totalInstallments);

                for (int i = 1; i <= totalInstallments; i++) {
                    var dueDate = CalculateDueDate(firstDueDate, i - 1, cardClosingDay);
                    var amount = installmentAmount;

                    // Adiciona o resto na primeira parcela
                    if (i == 1 && remainder != 0) {
                        amount += remainder;
                    }

                    var installment = new InstallmentModel {
                        Id = Guid.NewGuid(),
                        TransactionId = transactionId,
                        CardId = cardId,
                        UserId = userId,
                        Amount = amount,
                        InstallmentNumber = i,
                        TotalInstallments = totalInstallments,
                        DueDate = dueDate,
                        Description = description,
                        IsPaid = false,
                        CreatedAt = DateTime.UtcNow
                    };

                    installments.Add(installment);
                }

                await _context.Installments.InsertManyAsync(installments);

                response.InstallmentData = installments.Select(i => new InstallmentResponseDto {
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
                }).ToList();

                response.Message = $"{totalInstallments} parcelas criadas com sucesso!";
                return response;
            }
            catch (Exception ex) {
                response.Message = ex.Message;
                response.Status = false;
                return response;
            }
        }

        public async Task<List<InstallmentModel>> GetInstallmentsByInvoice(Guid invoiceId) {
            return await _context.Installments
                .Find(i => i.InvoiceId == invoiceId)
                .SortBy(i => i.InstallmentNumber)
                .ToListAsync();
        }

        public async Task<List<InstallmentModel>> GetInstallmentsByTransaction(Guid transactionId) {
            return await _context.Installments
                .Find(i => i.TransactionId == transactionId)
                .SortBy(i => i.InstallmentNumber)
                .ToListAsync();
        }

        public async Task<List<InstallmentModel>> GetPendingInstallmentsByCard(Guid cardId) {
            return await _context.Installments
                .Find(i => i.CardId == cardId && !i.IsPaid)
                .SortBy(i => i.DueDate)
                .ToListAsync();
        }

        public async Task<InstallmentResponseModel<bool>> AssignInstallmentToInvoice(Guid installmentId, Guid invoiceId) {
            InstallmentResponseModel<bool> response = new();

            try {
                var update = Builders<InstallmentModel>.Update.Set(i => i.InvoiceId, invoiceId);
                var result = await _context.Installments.UpdateOneAsync(i => i.Id == installmentId, update);

                if (result.ModifiedCount == 0) {
                    response.Message = "Parcela não encontrada!";
                    response.Status = false;
                    return response;
                }

                response.InstallmentData = true;
                response.Message = "Parcela atribuída à fatura com sucesso!";
                return response;
            }
            catch (Exception ex) {
                response.Message = ex.Message;
                response.Status = false;
                return response;
            }
        }

        private DateTime CalculateDueDate(DateTime firstDueDate, int monthsToAdd, int closingDay) {
            // Simplesmente adiciona os meses à primeira data de vencimento
            // A lógica de ajuste por fechamento já foi feita no TransactionService
            return firstDueDate.AddMonths(monthsToAdd);
        }
    }
}
