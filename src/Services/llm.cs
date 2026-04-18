using System.Text;
using System.Text.Json;

namespace Sensata.Services
{
    public class CopilotService
    {
        private readonly HttpClient _http;
        private readonly string _apiKey;
        private readonly string _modelId;

        // Конфигурацията идва от appsettings.json
        // "GeminiSettings": { "ApiKey": "...", "ModelId": "gemini-1.5-flash" }
        public CopilotService(IConfiguration config, HttpClient http)
        {
            _http    = http;
            _apiKey  = config["GeminiSettings:ApiKey"]
                ?? throw new InvalidOperationException("GeminiSettings:ApiKey не е конфигуриран.");
            _modelId = config["GeminiSettings:ModelId"] ?? "gemini-1.5-flash";
        }

        // ── АНАЛИЗ НА ЛИНИЯ ────────────────────────────────────────────────────────
        public async Task<object> AnalyzeAsync(object dataContext)
        {
            var systemPrompt = """
                Ти си производствен AI анализатор за Sensata Technologies.
                Получаваш реални данни от производствена линия и трябва да:
                1. Обобщиш текущото състояние на линията.
                2. Идентифицираш аномалии и отклонения (сравни дефект rate, температура, производство с нормата).
                3. Предложиш вероятни коренни причини базирани на ErrorCode алармите и операторските коментари.
                4. Идентифицираш повтарящи се модели в данните.
                5. Дадеш препоръки за мениджмънта — конкретни действия, не общи фрази.
                6. Обърнеш внимание на Token Score — по-висок score означава по-сериозен проблем.

                Отговаряй САМО в следния JSON формат без никакви допълнителни символи или markdown:
                {
                  "currentState": "...",
                  "anomalies": ["...", "..."],
                  "rootCauses": ["...", "..."],
                  "patterns": ["...", "..."],
                  "recommendations": ["...", "..."],
                  "riskLevel": "low|medium|high|critical"
                }
                """;

            var userPrompt = $"""
                Анализирай следните производствени данни и върни JSON отговор:

                {JsonSerializer.Serialize(dataContext, new JsonSerializerOptions { WriteIndented = true })}
                """;

            return await CallGeminiAsync(systemPrompt, userPrompt);
        }

        // ── ПРЕПОРЪКА ЗА РАБОТНИЦИ ────────────────────────────────────────────────
        public async Task<object> SuggestWorkersAsync(object dataContext)
        {
            var systemPrompt = """
                Ти си HR оптимизатор за производствена линия в Sensata Technologies.
                Получаваш информация за отсъстващи работници и налични заместници с техните умения.
                Трябва да:
                1. Анализираш кои умения липсват на линията заради отсъствията.
                2. Препоръчаш конкретни заместници по приоритет (lead > senior > junior).
                3. Обясниш защо конкретен работник е най-подходящ.
                4. Предупредиш ако линията е в риск дори след заместването.

                Отговаряй САМО в следния JSON формат без никакви допълнителни символи или markdown:
                {
                  "situationSummary": "...",
                  "suggestions": [
                    {
                      "workerName": "...",
                      "skillMatch": ["..."],
                      "reason": "...",
                      "priority": 1
                    }
                  ],
                  "riskAfterReplacement": "low|medium|high",
                  "managerNote": "..."
                }
                """;

            var userPrompt = $"""
                Препоръчай работници за заместване на базата на следните данни:

                {JsonSerializer.Serialize(dataContext, new JsonSerializerOptions { WriteIndented = true })}
                """;

            return await CallGeminiAsync(systemPrompt, userPrompt);
        }

        // ── ПРОГНОЗА ──────────────────────────────────────────────────────────────
        public async Task<object> ForecastAsync(object dataContext)
        {
            var systemPrompt = """
                Ти си производствен анализатор за Sensata Technologies специализиран в прогнозиране.
                Получаваш исторически данни от производствена линия за последните 30 дни.
                Трябва да:
                1. Анализираш тренда в производството, дефектите и температурата.
                2. Идентифицираш дали има влошаване или подобрение.
                3. Прогнозираш следващите 7 дни — очаквано производство, рискове.
                4. Предупредиш за потенциални проблеми базирани на алармените модели.
                5. Препоръчаш превантивни действия.

                Отговаряй САМО в следния JSON формат без никакви допълнителни символи или markdown:
                {
                  "trendAnalysis": "...",
                  "forecast": [
                    { "day": 1, "expectedProduction": 0, "riskLevel": "low|medium|high", "note": "..." }
                  ],
                  "mainRisks": ["...", "..."],
                  "preventiveActions": ["...", "..."],
                  "confidenceLevel": "low|medium|high"
                }
                """;

            var userPrompt = $"""
                Направи прогноза за следващите 7 дни базирана на тези исторически данни:

                {JsonSerializer.Serialize(dataContext, new JsonSerializerOptions { WriteIndented = true })}
                """;

            return await CallGeminiAsync(systemPrompt, userPrompt);
        }

        // ── РЕАЛЕН HTTP CALL КЪМ GOOGLE GEMINI ───────────────────────────────────
        private async Task<object> CallGeminiAsync(string systemPrompt, string userPrompt)
        {
            // Gemini REST endpoint:
            // POST https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_modelId}:generateContent?key={_apiKey}";

            var requestBody = new
            {
                system_instruction = new
                {
                    parts = new[] { new { text = systemPrompt } }
                },
                contents = new[]
                {
                    new
                    {
                        role  = "user",
                        parts = new[] { new { text = userPrompt } }
                    }
                },
                generationConfig = new
                {
                    temperature      = 0.3,
                    maxOutputTokens  = 1500,
                    responseMimeType = "application/json"   // Gemini връща чист JSON
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

            // Gemini response структура:
            // { "candidates": [ { "content": { "parts": [ { "text": "{...json...}" } ] } } ] }
            var text = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString() ?? "{}";

            // Почистваме евентуални markdown ограждения (```json ... ```)
            text = text.Trim();
            if (text.StartsWith("```"))
            {
                text = text.TrimStart('`');
                if (text.StartsWith("json")) text = text[4..];
                text = text.TrimStart('\n').TrimEnd('`').Trim();
            }

            using var resultDoc = JsonDocument.Parse(text);
            return resultDoc.RootElement.Clone();
        }
    }
}