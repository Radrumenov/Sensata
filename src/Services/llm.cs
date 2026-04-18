using System.Text;
using System.Text.Json;

namespace Sensata.Services
{
    public class CopilotService
    {
        private readonly HttpClient _http;
        private readonly string     _apiKey;
        private readonly string     _modelId;

        public CopilotService(IConfiguration config, HttpClient http)
        {
            _http    = http;
            _apiKey  = config["GeminiSettings:ApiKey"]
                ?? throw new InvalidOperationException("GeminiSettings:ApiKey не е конфигуриран.");
            _modelId = config["GeminiSettings:ModelId"] ?? "gemini-2.0-flash-lite";
        }

        // ── АНАЛИЗ НА ЛИНИЯ ───────────────────────────────────────────────────────
        public async Task<object> AnalyzeAsync(object dataContext)
        {
            var data = JsonSerializer.Serialize(dataContext, new JsonSerializerOptions { WriteIndented = true });

            var prompt =
                "Ти си производствен AI анализатор за Sensata Technologies.\n" +
                "Получаваш реални данни от производствена линия и трябва да:\n" +
                "1. Обобщиш текущото състояние на линията.\n" +
                "2. Идентифицираш аномалии и отклонения.\n" +
                "3. Предложиш вероятни коренни причини базирани на ErrorCode алармите и операторските коментари.\n" +
                "4. Идентифицираш повтарящи се модели в данните.\n" +
                "5. Дадеш препоръки за мениджмънта — конкретни действия, не общи фрази.\n" +
                "6. Обърнеш внимание на Token Score — по-висок score означава по-сериозен проблем.\n\n" +
                "Върни САМО валиден JSON без markdown, без обяснения, точно в този формат:\n" +
                "{\n" +
                "  \"currentState\": \"...\",\n" +
                "  \"anomalies\": [\"...\"],\n" +
                "  \"rootCauses\": [\"...\"],\n" +
                "  \"patterns\": [\"...\"],\n" +
                "  \"recommendations\": [\"...\"],\n" +
                "  \"riskLevel\": \"low или medium или high или critical\"\n" +
                "}\n\n" +
                "Данни за анализ:\n" + data;

            return await CallGeminiAsync(prompt);
        }

        // ── ПРЕПОРЪКА ЗА РАБОТНИЦИ ────────────────────────────────────────────────
        public async Task<object> SuggestWorkersAsync(object dataContext)
        {
            var data = JsonSerializer.Serialize(dataContext, new JsonSerializerOptions { WriteIndented = true });

            var prompt =
                "Ти си HR оптимизатор за производствена линия в Sensata Technologies.\n" +
                "Получаваш информация за отсъстващи работници и налични заместници с техните умения.\n" +
                "Трябва да:\n" +
                "1. Анализираш кои умения липсват на линията заради отсъствията.\n" +
                "2. Препоръчаш конкретни заместници по приоритет (lead > senior > junior).\n" +
                "3. Обясниш защо конкретен работник е най-подходящ.\n" +
                "4. Предупредиш ако линията е в риск дори след заместването.\n\n" +
                "Върни САМО валиден JSON без markdown, без обяснения, точно в този формат:\n" +
                "{\n" +
                "  \"situationSummary\": \"...\",\n" +
                "  \"suggestions\": [\n" +
                "    { \"workerName\": \"...\", \"skillMatch\": [\"...\"], \"reason\": \"...\", \"priority\": 1 }\n" +
                "  ],\n" +
                "  \"riskAfterReplacement\": \"low или medium или high\",\n" +
                "  \"managerNote\": \"...\"\n" +
                "}\n\n" +
                "Данни:\n" + data;

            return await CallGeminiAsync(prompt);
        }

        // ── ПРОГНОЗА ─────────────────────────────────────────────────────────────
        public async Task<object> ForecastAsync(object dataContext)
        {
            var data = JsonSerializer.Serialize(dataContext, new JsonSerializerOptions { WriteIndented = true });

            var prompt =
                "Ти си производствен анализатор за Sensata Technologies специализиран в прогнозиране.\n" +
                "Получаваш исторически данни от производствена линия за последните 30 дни.\n" +
                "Трябва да:\n" +
                "1. Анализираш тренда в производството, дефектите и температурата.\n" +
                "2. Идентифицираш дали има влошаване или подобрение.\n" +
                "3. Прогнозираш следващите 7 дни — очаквано производство, рискове.\n" +
                "4. Предупредиш за потенциални проблеми базирани на алармените модели.\n" +
                "5. Препоръчаш превантивни действия.\n\n" +
                "Върни САМО валиден JSON без markdown, без обяснения, точно в този формат:\n" +
                "{\n" +
                "  \"trendAnalysis\": \"...\",\n" +
                "  \"forecast\": [\n" +
                "    { \"day\": 1, \"expectedProduction\": 0, \"riskLevel\": \"low или medium или high\", \"note\": \"...\" }\n" +
                "  ],\n" +
                "  \"mainRisks\": [\"...\"],\n" +
                "  \"preventiveActions\": [\"...\"],\n" +
                "  \"confidenceLevel\": \"low или medium или high\"\n" +
                "}\n\n" +
                "Исторически данни:\n" + data;

            return await CallGeminiAsync(prompt);
        }

        // ── HTTP CALL КЪМ GEMINI ─────────────────────────────────────────────────
        private async Task<object> CallGeminiAsync(string prompt)
        {
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_modelId}:generateContent?key={_apiKey}";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[] { new { text = prompt } }
                    }
                },
                generationConfig = new
                {
                    temperature     = 0.3,
                    maxOutputTokens = 1500,
                }
            };

            var json    = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Gemini грешка {response.StatusCode}: {error}");
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc    = JsonDocument.Parse(responseJson);

            var text = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString() ?? "{}";

            // Почистваме евентуални markdown ограждения
            text = text.Trim();
            if (text.StartsWith("```"))
            {
                var lines = text.Split('\n').ToList();
                lines.RemoveAt(0);
                if (lines.Count > 0 && lines.Last().TrimEnd() == "```")
                    lines.RemoveAt(lines.Count - 1);
                text = string.Join('\n', lines).Trim();
            }

            using var resultDoc = JsonDocument.Parse(text);
            return resultDoc.RootElement.Clone();
        }
    }
}