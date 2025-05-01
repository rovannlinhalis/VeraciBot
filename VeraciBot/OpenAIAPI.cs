using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace VeraciBot
{

    public class OpenAIAPI
    {

        public static async Task<int> AvaliarVeracidadeAsync(string afirmacao)
        {

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AppKeys.keys.openAIKey);

            var requestBody = new
            {
                model = "gpt-4o",
                messages = new[]
                {
                new { role = "system", content = "Você é um verificador de fatos. Para cada afirmação recebida, responda apenas com um número de 1 a 5 representando o grau de veracidade:\n1 - completamente falso/impossível\n2 - provavelmente falso\n3 - incerto/sem dados suficientes\n4 - provavelmente verdadeiro\n5 - verdade factual." },
                new { role = "user", content = afirmacao }
            },
                temperature = 0.2
            };

            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("https://api.openai.com/v1/chat/completions", content);
            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine("Erro ao acessar a OpenAI API:");
                Console.WriteLine(responseString);
                return -1;
            }

            using var doc = JsonDocument.Parse(responseString);
            string resposta = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString()
                .Trim();

            resposta = resposta.Substring(0, 1); // Pega apenas o primeiro caractere da resposta

            // Tenta converter a resposta para número
            return int.TryParse(resposta, out int nota) ? nota : -1;

        }

        public static async Task<int> AvaliarArgumentoAsync(string afirmacao, string contraafirmacao)
        {

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AppKeys.keys.openAIKey);

            string prompt = $@"
Considere as duas afirmações abaixo:

Afirmação 1: ""{afirmacao}""
Afirmação 2: ""{contraafirmacao}""

Responda apenas com um número de 1 a 5 de acordo com a seguinte escala:

1 - A segunda afirmação está 100% correta
2 - A segunda afirmação está mais correta que a primeira
3 - Ambas estão incorretas, ambas estão corretas, ou é impossível decidir
4 - A primeira afirmação está mais correta que a segunda
5 - A primeira afirmação está 100% correta

Responda apenas com o número.
";


            var requestBody = new
            {
                model = "gpt-4o",
                messages = new[]
                {
                new { role = "user", content = prompt }
            },
                temperature = 0.2
            };

            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("https://api.openai.com/v1/chat/completions", content);
            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine("Erro ao acessar a OpenAI API:");
                Console.WriteLine(responseString);
                return -1;
            }

            using var doc = JsonDocument.Parse(responseString);
            string resposta = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString()
                .Trim();

            resposta = resposta.Substring(0, 1); // Pega apenas o primeiro caractere da resposta

            // Tenta converter a resposta para número
            return int.TryParse(resposta, out int nota) ? nota : -1;

        }

    }

}
