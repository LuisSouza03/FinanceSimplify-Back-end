using System.ComponentModel.DataAnnotations;

namespace FinanceSimplify.Dtos.Insights {
    public class ChatRequestDto {
        [Required(ErrorMessage = "A mensagem é obrigatória")]
        [MaxLength(1000, ErrorMessage = "A mensagem não pode ter mais de 1000 caracteres")]
        public string Message { get; set; } = string.Empty;

        public List<ChatMessageItemDto>? RecentHistory { get; set; }
    }

    public class ChatMessageItemDto {
        public string Sender { get; set; } = string.Empty; // "user" ou "gemini"
        public string Text { get; set; } = string.Empty;
    }
}
