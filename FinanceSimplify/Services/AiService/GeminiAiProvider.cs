using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FinanceSimplify.Services.AiService {
    public class GeminiAiProvider : IAiProvider {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GeminiAiProvider> _logger;
        private readonly string? _apiKey;
        private readonly string _model;

        public GeminiAiProvider(HttpClient httpClient, IConfiguration configuration, ILogger<GeminiAiProvider> logger) {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;

            // Busca chave do user-secrets, appsettings ou variável de ambiente sem expor seu valor
            _apiKey = _configuration["Gemini:ApiKey"] ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY");
            _model = _configuration["Gemini:Model"] ?? "gemini-1.5-flash";

            _httpClient.Timeout = TimeSpan.FromSeconds(15);
        }

        public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey);

        public async Task<string> GenerateContentAsync(string prompt, string? systemInstruction = null, bool jsonOutput = false) {
            if (!IsConfigured) {
                _logger.LogWarning("Gemini API key não configurada. Operando em modo de fallback local.");
                return string.Empty;
            }

            try {
                var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";

                var requestBody = new Dictionary<string, object> {
                    ["contents"] = new[] {
                        new {
                            role = "user",
                            parts = new[] {
                                new { text = prompt }
                            }
                        }
                    }
                };

                if (!string.IsNullOrWhiteSpace(systemInstruction)) {
                    requestBody["systemInstruction"] = new {
                        parts = new[] {
                            new { text = systemInstruction }
                        }
                    };
                }

                if (jsonOutput) {
                    requestBody["generationConfig"] = new {
                        temperature = 0.2,
                        responseMimeType = "application/json"
                    };
                } else {
                    requestBody["generationConfig"] = new {
                        temperature = 0.5
                    };
                }

                var jsonContent = JsonSerializer.Serialize(requestBody);
                using var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, httpContent);

                if (!response.IsSuccessStatusCode) {
                    var errorDetails = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Erro na chamada à API Gemini: Status {StatusCode}. Acionando fallback.", response.StatusCode);
                    return string.Empty;
                }

                var responseJson = await response.Content.ReadAsStringAsync();
                var parsedDoc = JsonDocument.Parse(responseJson);

                if (parsedDoc.RootElement.TryGetProperty("candidates", out var candidates) &&
                    candidates.GetArrayLength() > 0) {
                    var firstCandidate = candidates[0];
                    if (firstCandidate.TryGetProperty("content", out var content) &&
                        content.TryGetProperty("parts", out var parts) &&
                        parts.GetArrayLength() > 0) {
                        var firstPart = parts[0];
                        if (firstPart.TryGetProperty("text", out var textProp)) {
                            return textProp.GetString() ?? string.Empty;
                        }
                    }
                }

                return string.Empty;
            } catch (Exception ex) {
                _logger.LogError(ex, "Exceção ao comunicar com a API do Gemini. Acionando fallback.");
                return string.Empty;
            }
        }
    }
}
