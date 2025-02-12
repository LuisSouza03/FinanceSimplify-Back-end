using FinanceSimplify.Dtos.User;
using FinanceSimplify.Services.AuthService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FinanceSimplify.Controllers {

    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase {
        private readonly IAuthInterface _authInterface;

        public AuthController(IAuthInterface authInterface) { 
            this._authInterface = authInterface;
        }

        [HttpPost("login")]
        public async Task<ActionResult> LoginUser(UserLoginDto userLogin) {

            var response = await _authInterface.Login(userLogin);

            return Ok(response);

        }

        [HttpPost("register")]
        public async Task<ActionResult> RegisterUser(UserCreateDto user) {

            var response = await _authInterface.Registrar(user);

            return Ok(response);

        }


    }
}
