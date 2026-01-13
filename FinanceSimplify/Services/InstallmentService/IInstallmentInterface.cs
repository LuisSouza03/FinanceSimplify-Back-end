using FinanceSimplify.Dtos.Installment;
using FinanceSimplify.Models.Installment;

namespace FinanceSimplify.Services.InstallmentService {
    public interface IInstallmentInterface {
        Task<InstallmentResponseModel<List<InstallmentResponseDto>>> CreateInstallmentsForTransaction(
            Guid transactionId,
            Guid cardId,
            Guid userId,
            decimal totalAmount,
            int totalInstallments,
            DateTime firstDueDate,
            string description,
            int cardClosingDay
        );
        
        Task<List<InstallmentModel>> GetInstallmentsByInvoice(Guid invoiceId);
        Task<List<InstallmentModel>> GetInstallmentsByTransaction(Guid transactionId);
        Task<List<InstallmentModel>> GetPendingInstallmentsByCard(Guid cardId);
        Task<InstallmentResponseModel<bool>> AssignInstallmentToInvoice(Guid installmentId, Guid invoiceId);
    }
}
