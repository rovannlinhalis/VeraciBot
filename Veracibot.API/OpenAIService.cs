using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using Veracibot.API.Models;
using NJsonSchema;

namespace Veracibot.API
{
    public class OpenAIService
   
    {

 

        public class OpenAIResponse
        {
            public ETweetVeracity VeracityCheckOutcome { get; set; }
            public string Message { get; set; }
        }

        public static async Task<OpenAIResponse> CheckThread(TweeterService.ThreadContext thread, OpenAIOptions options)
        {

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
            var schema = JsonSchema.FromType<OpenAIResponse>();


            var requestBody = new
            {
                model = options.Model,
                messages = new[]
                {
                new { role = "system", content = options.SystemPrompt + "\n\nretorne a resposta com o seguinte formato: "+schema.ToJson() },
                new { role = "user", content = thread.GetFullDialog() }
            },
                temperature = options.Temperature
            };

            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("https://api.openai.com/v1/chat/completions", content);
            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine("Erro ao acessar a OpenAI API:");
                Console.WriteLine(responseString);
                return null;
            }


            OpenAIResponse  objResponse = JsonSerializer.Deserialize<OpenAIResponse>(responseString);

            return objResponse;


            //using var doc = JsonDocument.Parse(responseString);
            //string baseResult = doc.RootElement
            //    .GetProperty("choices")[0]
            //    .GetProperty("message")
            //    .GetProperty("content")
            //    .GetString()
            //    .Trim();

            //FullEvaluation resp = new FullEvaluation();

            //string numberPart = baseResult.Substring(0, 1); // Pega apenas o primeiro caractere da resposta

            //// Tenta converter a resposta para número
            //resp.Result = int.TryParse(numberPart, out int nota) ? nota : -1;
            //resp.Response = baseResult.Substring(2).Trim(); // Pega o restante da resposta após o número   

            //if (resp.Response.StartsWith("-"))
            //    resp.Response = resp.Response.Substring(2).Trim();

            //return resp;

        }

    }
}
