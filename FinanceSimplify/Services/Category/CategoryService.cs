using FinanceSimplify.Data;
using FinanceSimplify.Dtos.Category;
using FinanceSimplify.Models.Category;
using MongoDB.Driver;

namespace FinanceSimplify.Services.Category {
    public class CategoryService: ICategoryInterface {
        private readonly MongoDbContext _context;

        public CategoryService(MongoDbContext context) {
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

                await _context.Categories.InsertOneAsync(category);

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
            var query = _context.Categories.Find(c => c.UserId == userId);

            if (pageSize > 0) {
                query = query.Skip((page - 1) * pageSize).Limit(pageSize);
            }

            return await query.ToListAsync();
        }

        public async Task<CategoryResponseModel<CategoryResponseDto>> EditCategory(Guid userId, Guid categoryId, CategoryDto categoryDto) {
            CategoryResponseModel<CategoryResponseDto> response = new();

            try {
                var update = Builders<CategoryModel>.Update.Set(c => c.Name, categoryDto.Name);
                var result = await _context.Categories.UpdateOneAsync(c => c.Id == categoryId && c.UserId == userId, update);

                if (result.MatchedCount == 0) {
                    response.Message = "Categoria não encontrada";
                    response.Status = false;
                    return response;
                }

                response.CategoryData = new CategoryResponseDto {
                    Id = categoryId,
                    Name = categoryDto.Name
                };
                response.Message = "Categoria atualizada com sucesso!";
                return response;
            }
            catch (Exception ex) {
                response.Message = ex.Message;
                response.Status = false;
                return response;
            }
        }

        public async Task<CategoryResponseModel<bool>> DeleteCategory(Guid UserId, Guid categoryId) {
            CategoryResponseModel<bool> response = new();

            try {
                var result = await _context.Categories.DeleteOneAsync(c => c.Id == categoryId && c.UserId == UserId);

                if(result.DeletedCount == 0) {
                    response.Message = "Categoria não encontrada";
                    response.Status = false;
                    return response;
                }

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

        public async Task<CategoryModel?> GetCategoryByName(Guid userId, string categoryName) {
            return await _context.Categories
                .Find(c => c.UserId == userId && c.Name.ToLower() == categoryName.ToLower())
                .FirstOrDefaultAsync();
        }
    }
}
