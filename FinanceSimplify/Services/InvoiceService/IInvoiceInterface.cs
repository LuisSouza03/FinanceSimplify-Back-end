using FinanceSimplify.Dtos.Invoice;
using FinanceSimplify.Models.Invoice;

namespace FinanceSimplify.Services.InvoiceService {
    public interface IInvoiceInterface {
        Task<InvoiceResponseModel<InvoiceResponseDto>> GenerateInvoiceForCard(Guid cardId, Guid userId, int month, int year);
        Task<List<InvoiceModel>> GetInvoicesByCard(Guid cardId, int page, int pageSize);
        Task<InvoiceResponseModel<InvoiceResponseDto>> GetInvoiceById(Guid invoiceId);
        Task<InvoiceResponseModel<InvoiceResponseDto>> GetCurrentInvoice(Guid cardId);
        Task<InvoiceResponseModel<bool>> MarkInvoiceAsPaid(Guid invoiceId);
        Task<decimal> CalculateInvoiceTotal(Guid invoiceId);
    }
}
