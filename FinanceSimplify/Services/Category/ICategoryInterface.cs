using FinanceSimplify.Dtos.Category;
using FinanceSimplify.Models.Category;

namespace FinanceSimplify.Services.Category {
    public interface ICategoryInterface {

        Task<CategoryResponseModel<CategoryResponseDto>> CreateCategory(Guid userId, CategoryDto categoryDto);

        Task<List<CategoryModel>> GetCategoriesByUserId(Guid userId, int page, int pageSize);

        Task<CategoryResponseModel<bool>> DeleteCategory(Guid UserId, Guid categoryId);
    }
}
