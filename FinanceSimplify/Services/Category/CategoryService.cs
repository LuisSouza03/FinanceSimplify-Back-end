using FinanceSimplify.Data;
using FinanceSimplify.Dtos.Category;
using FinanceSimplify.Models.Category;
using Microsoft.EntityFrameworkCore;

namespace FinanceSimplify.Services.Category {
    public class CategoryService: ICategoryInterface {
        private readonly AppDbContext _context;

        public CategoryService(AppDbContext context) {
            _context = context;
        }

        public async Task<CategoryResponseModel<CategoryResponseDto>> CreateCategory(Guid userId, CategoryDto categoryDto) {
            CategoryResponseModel<CategoryResponseDto> response = new();

            try {
                CategoryModel category = new() {
                    Id = Guid.NewGuid(),
                    Name = categoryDto.Name,
                    UserId = userId,
                };

                _context.Categories.Add(category);
                await _context.SaveChangesAsync();

                response.CategoryData = new CategoryResponseDto {
                    Id = category.Id,
                    Name = category.Name,
                };

                response.Message = "Categoria criada com sucesso!";
                return response;


            }
            catch (Exception ex) {
                response.Message = ex.Message;
                response.Status = false;
                return response;
            }
        }

        public async Task<List<CategoryModel>> GetCategoriesByUserId(Guid userId, int page, int pageSize) {

            return await _context.Categories
                .Where(c => c.UserId ==  userId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<CategoryResponseModel<bool>> DeleteCategory(Guid UserId, Guid categoryId) {
            CategoryResponseModel<bool> response = new();

            try {
                var category = await _context.Categories.FirstOrDefaultAsync(categoryDb => categoryDb.Id == categoryId && categoryDb.UserId == UserId);

                if(category == null) {
                    response.Message = "Categoria não encontrada";
                    response.Status = false;
                    return response;
                }

                _context.Remove(category);
                await _context.SaveChangesAsync();

                response.CategoryData = true;
                response.Message = "Categoria removida com sucesso!";
                return response;
            }
            catch (Exception ex) {
                response.Message = ex.Message;
                response.Status = false;
                return response;
            }
        }

    }
}
