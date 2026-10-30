using FinanceSimplify.Dtos.Investment;
using FinanceSimplify.Models.Investment;

namespace FinanceSimplify.Services.InvestmentService {
    public interface IInvestmentInterface {
        Task<InvestmentResponseModel<InvestmentResponseDto>> CreateInvestment(Guid userId, InvestmentCreateDto dto);
        Task<InvestmentResponseModel<InvestmentSummaryDto>> GetInvestmentsByUserId(Guid userId, decimal? customCdi = null);
        Task<InvestmentResponseModel<InvestmentResponseDto>> GetInvestmentById(Guid id, decimal? customCdi = null);
        Task<InvestmentResponseModel<InvestmentResponseDto>> UpdateInvestment(Guid id, InvestmentEditDto dto);
        Task<InvestmentResponseModel<bool>> DeleteInvestment(Guid id);
        Task<decimal> GetCurrentCdiRate();
    }
}
