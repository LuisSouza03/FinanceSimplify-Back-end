namespace FinanceSimplify.Services.AiService {
    public interface IAiProvider {
        bool IsConfigured { get; }
        Task<string> GenerateContentAsync(string prompt, string? systemInstruction = null, bool jsonOutput = false);
    }
}
