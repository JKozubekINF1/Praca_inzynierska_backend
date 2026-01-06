using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Market.Services
{
    public interface IAiModerationService
    {
        Task<(bool IsSafe, string Reason)> CheckContentAsync(string title, string description, decimal price);
        Task<string> TestConnectionAsync();
    }

    public class OpenAiModerationService : IAiModerationService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public OpenAiModerationService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["OpenAi:ApiKey"];
        }

        public async Task<string> TestConnectionAsync()
        {
            if (string.IsNullOrEmpty(_apiKey)) return "BŁĄD: Brak klucza API w appsettings.json";

            var requestBody = new
            {
                model = "gpt-4o-mini",
                messages = new[]
                {
                    new { role = "user", content = "Test połączenia. Odpowiedz jednym słowem: OK." }
                }
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            try
            {
                var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", jsonContent);
                var responseString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return $"SUKCES! OpenAI odpowiedziało: {response.StatusCode}. Treść: {responseString}";
                }
                else
                {
                    return $"BŁĄD API ({response.StatusCode}): {responseString}";
                }
            }
            catch (Exception ex)
            {
                return $"WYJĄTEK SYSTEMOWY: {ex.Message}";
            }
        }

        public async Task<(bool IsSafe, string Reason)> CheckContentAsync(string title, string description, decimal price)
        {
            if (string.IsNullOrEmpty(_apiKey)) return (true, "No API Key");

            var systemPrompt = "Jesteś surowym moderatorem w serwisie ogłoszeniowym. " +
                               "Analizuj pod kątem: oszustw (scam), sprzedaży nielegalnych towarów, wulgaryzmów i mowy nienawiści. " +
                               "Jeśli ogłoszenie jest podejrzane lub narusza zasady, zwróć isSafe: false. " +
                               "Odpowiedz TYLKO JSONem.";

            var userPrompt = $"Tytuł: {title}\nOpis: {description}\nCena: {price}";

            var requestBody = new
            {
                model = "gpt-4o-mini",
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                response_format = new { type = "json_object" }
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            try
            {
                var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", jsonContent);

                if (!response.IsSuccessStatusCode) return (true, "API Error");

                var responseString = await response.Content.ReadAsStringAsync();
                var aiResponse = JsonSerializer.Deserialize<OpenAiResponse>(responseString);
                var content = aiResponse?.Choices?.FirstOrDefault()?.Message?.Content;

                if (!string.IsNullOrEmpty(content))
                {
                    var result = JsonSerializer.Deserialize<ModerationResult>(content);
                    return (result.IsSafe, result.Reason ?? "Naruszenie regulaminu");
                }
            }
            catch
            {
                return (true, "Exception");
            }

            return (true, "OK");
        }

        private class OpenAiResponse { [JsonPropertyName("choices")] public List<Choice> Choices { get; set; } }
        private class Choice { [JsonPropertyName("message")] public Message Message { get; set; } }
        private class Message { [JsonPropertyName("content")] public string Content { get; set; } }
        private class ModerationResult
        {
            [JsonPropertyName("isSafe")] public bool IsSafe { get; set; }
            [JsonPropertyName("reason")] public string Reason { get; set; }
        }
    }
}