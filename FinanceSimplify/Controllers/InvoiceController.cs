using FinanceSimplify.Dtos.Invoice;
using FinanceSimplify.Models.Invoice;
using FinanceSimplify.Services.InvoiceService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FinanceSimplify.Controllers {
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class InvoiceController : ControllerBase {
        private readonly IInvoiceInterface _invoiceService;

        public InvoiceController(IInvoiceInterface invoiceService) {
            _invoiceService = invoiceService;
        }

        [HttpGet("card/{cardId}")]
        public async Task<ActionResult<List<InvoiceModel>>> GetInvoicesByCard(Guid cardId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10) {
            try {
                var invoices = await _invoiceService.GetInvoicesByCard(cardId, page, pageSize);
                return Ok(invoices);
            }
            catch (Exception ex) {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{invoiceId}")]
        public async Task<ActionResult<InvoiceResponseModel<InvoiceResponseDto>>> GetInvoiceById(Guid invoiceId) {
            try {
                var response = await _invoiceService.GetInvoiceById(invoiceId);
                
                if (!response.Status) {
                    return NotFound(response);
                }

                return Ok(response);
            }
            catch (Exception ex) {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("card/{cardId}/current")]
        public async Task<ActionResult<InvoiceResponseModel<InvoiceResponseDto>>> GetCurrentInvoice(Guid cardId) {
            try {
                var response = await _invoiceService.GetCurrentInvoice(cardId);
                
                if (!response.Status) {
                    return NotFound(response);
                }

                return Ok(response);
            }
            catch (Exception ex) {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("generate/{cardId}")]
        public async Task<ActionResult<InvoiceResponseModel<InvoiceResponseDto>>> GenerateInvoice(Guid cardId, [FromQuery] int month, [FromQuery] int year) {
            try {
                var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty);
                var response = await _invoiceService.GenerateInvoiceForCard(cardId, userId, month, year);
                
                if (!response.Status) {
                    return BadRequest(response);
                }

                return Ok(response);
            }
            catch (Exception ex) {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{invoiceId}/pay")]
        public async Task<ActionResult<InvoiceResponseModel<bool>>> MarkInvoiceAsPaid(Guid invoiceId) {
            try {
                var response = await _invoiceService.MarkInvoiceAsPaid(invoiceId);
                
                if (!response.Status) {
                    return NotFound(response);
                }

                return Ok(response);
            }
            catch (Exception ex) {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
