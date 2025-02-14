﻿using FinanceSimplify.Dtos.Transactions;
using FinanceSimplify.Services.TransactionService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceSimplify.Controllers {
    [Route("api/[controller]")]
    [ApiController]
    public class TransactionController : Controller {

        private readonly ITransactionInterface _transactionInterface;

        public TransactionController(ITransactionInterface transactionService) {
            _transactionInterface = transactionService;
        }

        [Authorize]
        [HttpPost("Create")]
        public async Task<ActionResult> CreateTransaction (TransactionCreateDto trnsaction) {
            var response = await _transactionInterface.CreateTransaction(trnsaction);
            return Ok(response);
        }

        [Authorize]
        [HttpGet("getAll")]
        public async Task<ActionResult> GetAllTransactions() {
            var response = await _transactionInterface.GetAllTransactions();
            return Ok(response);
        }

        [Authorize]
        [HttpGet("byCard/{cardId}")]
        public async Task<ActionResult> GetTransactionByCard(Guid cardId, int page = 1, int pageSize = 10) {

            var response = await _transactionInterface.GetTransactionsByCard(cardId, page, pageSize);
            return Ok(response);
        }

        [Authorize]
        [HttpGet("byUser/{userId}")]
        public async Task<ActionResult> GetTransactionsByUserId(Guid userId, int page = 1, int pageSize = 10) {

            var response = await _transactionInterface.GetTransactionsByUserId(userId, page, pageSize);
            return Ok(response);
        }

        [Authorize]
        [HttpGet("byId/{transactionId}")]
        public async Task<ActionResult> GetTransactionById(Guid transactionId) {
            var response = await _transactionInterface.GetTransactionById(transactionId);
            return Ok(response);
        }

        [Authorize]
        [HttpDelete("deleteTransacion/{transactionId}")]
        public async Task<ActionResult> DeleteTransaction(Guid transactionId) {
            var repsonse = await _transactionInterface.DeleteTransaction(transactionId);
            return Ok(repsonse);
        }

        [Authorize]
        [HttpPut("editarAutor/{transactionId}")]
        public async Task<ActionResult> EditTransaction(Guid transactionId, [FromBody] TransactionEditDto transactionEditDto) {
            var response = await _transactionInterface.EditTransaction(transactionId, transactionEditDto);
            return Ok(response);
        }
    }
}
