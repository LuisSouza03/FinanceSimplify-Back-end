using System.Collections.Generic;
using FinanceSimplify.Data;
using FinanceSimplify.Dtos.Transactions;
using FinanceSimplify.Models.Transaction;
using FinanceSimplify.Models.User;
using Microsoft.EntityFrameworkCore;

namespace FinanceSimplify.Services.TransactionService {
    public class TransactionService : ITransactionInterface {
        private readonly AppDbContext _context;

        public TransactionService(AppDbContext context) {
            _context = context;
        }

        public async Task<TransactionResponseModel<TransactionResponseDto>> CreateTransaction(TransactionCreateDto transactionDto) {

            TransactionResponseModel<TransactionResponseDto> response = new();

            try {
                TransactionModel transaction = new() {
                    Id = Guid.NewGuid(),
                    Name = transactionDto.Name,
                    Amount = transactionDto.Amount,
                    Date = transactionDto.Date,
                    Type = transactionDto.Type,
                    PaymentMethod = transactionDto.PaymentMethod,
                    Installments = transactionDto.Installments,
                    CardId = transactionDto.CardId,
                    CategoryId = transactionDto.CategoryId,
                    UserId = transactionDto.UserId
                };



                _context.Transaction.Add(transaction);
                await _context.SaveChangesAsync();

                response.TransactionData = new TransactionResponseDto {
                    Id = transaction.Id,
                    Name = transactionDto.Name,
                    Amount = transactionDto.Amount,
                    Date = transactionDto.Date,
                    Type = transactionDto.Type,
                    PaymentMethod = transactionDto.PaymentMethod,
                    Installments = transactionDto.Installments,
                    CardId = transactionDto.CardId,
                    CategoryId = transactionDto.CategoryId,
                    UserId = transactionDto.UserId
                };

                response.Message = "Transação criada com sucesso!";
                return response;
            }
            catch (Exception ex) {
                response.Message = ex.Message;
                response.Status = false;
                return response;
            }

        }

        public async Task<TransactionResponseModel<List<TransactionResponseDto>>> GetAllTransactions() {
            TransactionResponseModel<List<TransactionResponseDto>> response = new();

            try {
                var transactions = await _context.Transaction.ToListAsync();

                var data = transactions.Select(transactionDB => new TransactionResponseDto {
                    Name = transactionDB.Name,
                    Amount = transactionDB.Amount,
                    Date = transactionDB.Date,
                    Type = transactionDB.Type,
                    PaymentMethod = transactionDB.PaymentMethod,
                    Installments = transactionDB.Installments,
                    CardId = transactionDB.CardId,
                    CategoryId = transactionDB.CategoryId,
                    UserId = transactionDB.UserId
                }).ToList();

                response.TransactionData = data;
                response.Message = "Todas as transações foram coletadas!";
                return response;


            }
            catch (Exception ex) {
                response.Message = ex.Message;
                response.Status = false;
                return response;
            }
        }

        public async Task<List<TransactionModel>> GetTransactionsByCard(Guid cardId, int page, int pageSize) {

            return await _context.Transaction
                .Where(t => t.CardId == cardId)
                .OrderByDescending(t => t.Date)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<TransactionResponseModel<TransactionResponseDto>> GetTransactionById(Guid transactionId) {
            TransactionResponseModel<TransactionResponseDto> response = new();

            try {

                var transaction = await _context.Transaction.FindAsync(transactionId);

                if (transaction == null) {
                    response.Message = "Transação não encontrada!";
                    response.Status = false;
                    return response;
                }

                response.TransactionData = new TransactionResponseDto {
                    Id = transaction.Id,
                    Name = transaction.Name,
                    Amount = transaction.Amount,
                    Date = transaction.Date,
                    Type = transaction.Type,
                    PaymentMethod = transaction.PaymentMethod,
                    Installments = transaction.Installments,
                    CardId = transaction.CardId,
                    CategoryId = transaction.CategoryId,
                    UserId = transaction.UserId
                };

                return response;

            }
            catch (Exception ex) {
                response.Message = ex.Message;
                response.Status = false;
                return response;
            }
        }

        public async Task<TransactionResponseModel<bool>> DeleteTransaction(Guid transactionId) {
            TransactionResponseModel<bool> response = new();

            try {
                var transaction = await _context.Transaction.FirstOrDefaultAsync(transactionDb => transactionDb.Id == transactionId);

                if (transaction == null) {
                    response.Message = "Transação não encontrada!";
                    response.Status = false;
                    return response;
                }

                _context.Remove(transaction);
                await _context.SaveChangesAsync();

                response.TransactionData = true;
                response.Message = "Transação removida com sucesso!";
                return response;

            }
            catch (Exception ex) {
                response.Message = ex.Message;
                response.Status = false;
                return response;
            }
        }

        public async Task<TransactionResponseModel<TransactionResponseDto>> EditTransaction(Guid transactionId, TransactionEditDto transactionEditDto) {
            TransactionResponseModel<TransactionResponseDto> response = new();

            try {

                var transaction = await _context.Transaction.FirstOrDefaultAsync(transactionDb => transactionDb.Id == transactionId);

                if (transaction == null) {
                    response.Message = "Nenhuma transaction localizada!";
                    response.Status = false;
                    return response;
                }

                transaction.Amount = transactionEditDto.Amount;
                transaction.PaymentMethod = transactionEditDto.PaymentMethod;
                transaction.Name = transactionEditDto.Name;
                transaction.Installments = transactionEditDto.Installments;
                transaction.CardId = transactionEditDto.CardId;
                transaction.CategoryId = transactionEditDto.CategoryId;
                transaction.Date = transactionEditDto.Date;

                _context.Update(transaction);
                await _context.SaveChangesAsync();

                response.TransactionData = new TransactionResponseDto {
                    Id = transaction.Id,
                    Name = transaction.Name,
                    Amount = transaction.Amount,
                    Date = transaction.Date,
                    Type = transaction.Type,
                    PaymentMethod = transaction.PaymentMethod,
                    Installments = transaction.Installments,
                    CardId = transaction.CardId,
                    CategoryId = transaction.CategoryId,
                };
                response.Message = "Transaction atualizada com sucesso!";
                return response;

            }
            catch (Exception ex) {
                response.Message = ex.Message;
                response.Status = false;
                return response;
            }
             
        }

        public async Task<List<TransactionModel>> GetTransactionsByUserId(Guid userId, int page, int pageSize) {

            return await _context.Transaction
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.Date)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
    }
}
