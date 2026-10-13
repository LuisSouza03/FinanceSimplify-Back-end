using FinanceSimplify.Dtos.Insights;
using FinanceSimplify.Models.Insight;

namespace FinanceSimplify.Services.InsightService {
    public interface IInsightInterface {
        Task<InsightResponseModel<ChatResponseDto>> ProcessChat(Guid userId, ChatRequestDto request);
        Task<InsightResponseModel<MonthlyInsightDto>> GetMonthlyInsight(Guid userId, bool forceRefresh = false);
    }
}
