using FinanceSimplify.Dtos.Category;
using FinanceSimplify.Services.Category;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceSimplify.Controllers {

    [Route("api/category")]
    [ApiController]
    public class CategoryController : Controller {

        private readonly ICategoryInterface _categoryService;

        public CategoryController(ICategoryInterface categoryService) {
            _categoryService = categoryService;
        }

        [Authorize]
        [HttpPost("create/{userId}")]
        public async Task<ActionResult> CreateCategory(Guid userId, CategoryDto categoryDto) {
            var response = await _categoryService.CreateCategory(userId, categoryDto);
            return Ok(response);
        }

        [Authorize]
        [HttpGet("byUser/{userId}")]
        public async Task<ActionResult> GetCategoriesByUserId(Guid userId, int page = 1, int pageSize = 10) {
            var response = await _categoryService.GetCategoriesByUserId(userId, page, pageSize);
            return Ok(response);
        }

        [Authorize]
        [HttpDelete("delete/{userId}/{categoryId}")]
        public async Task<ActionResult> DeleteCategory(Guid userId, Guid categoryId) {
            var response = await _categoryService.DeleteCategory(userId, categoryId);
            return Ok(response);
        }
    }
}
