using Newtonsoft.Json.Linq;

namespace Market.Services
{
    public class RecaptchaService
    {
        private readonly string _secretKey;
        private readonly HttpClient _httpClient;
        private readonly IWebHostEnvironment _environment; 

        public RecaptchaService(IConfiguration configuration, HttpClient httpClient, IWebHostEnvironment environment)
        {
            _secretKey = configuration["Recaptcha:SecretKey"];
            _httpClient = httpClient;
            _environment = environment; 
        }

        public async Task<bool> VerifyTokenAsync(string token)
        {
           
            if (_environment.IsDevelopment() && token == "TEST")
            {
                return true;
            }
            
            if (string.IsNullOrEmpty(token)) return false;

            var response = await _httpClient.GetStringAsync(
                $"https://www.google.com/recaptcha/api/siteverify?secret={_secretKey}&response={token}");

            var jsonResponse = JObject.Parse(response);
            var success = jsonResponse["success"]?.Value<bool>();

            return success ?? false;
        }
    }
}