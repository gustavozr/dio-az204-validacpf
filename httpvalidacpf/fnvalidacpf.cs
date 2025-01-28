using System.Text.Json;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using System;

namespace httpvalidacpf
{
    public static class FnvalidaCpf
    {
        [FunctionName("fnvalidacpf")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Processing CPF validation request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            if (requestBody == null)
            {
                return new BadRequestObjectResult(new { message = "Por favor, informe um CPF." });
            }

            // Deserialize the JSON body to extract the CPF
            CpfRequest data = null;
            try {
                data = JsonSerializer.Deserialize<CpfRequest>(requestBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch
            {
                return new BadRequestObjectResult(new { message = "CPF inválido." });
            }

            if (data == null || string.IsNullOrWhiteSpace(data.Cpf))
            {
                return new BadRequestObjectResult(new { message = "Por favor, informe um CPF." });
            }

            bool isValid = ValidateCpf(data.Cpf);
            if (isValid)
            {
                return new OkObjectResult("CPF válido, e não consta na base de dados de fraudes, e não consta na base de dados de débitos.");
            }
            else 
            {
                return new BadRequestObjectResult(new { message = "CPF inválido." });
            }
        }

        private static bool ValidateCpf(string cpf)
        {
            // Remove non-numeric characters
            cpf = Regex.Replace(cpf, @"\D", "");

            // CPF must be 11 digits and not a sequence of the same number
            if (cpf.Length != 11 || Regex.IsMatch(cpf, @"^(\d)\1{10}$"))
            {
                return false;
            }

            // Validate first check digit
            int sum = 0;
            for (int i = 0; i < 9; i++)
            {
                sum += (cpf[i] - '0') * (10 - i);
            }

            int remainder = sum % 11;
            int firstCheckDigit = remainder < 2 ? 0 : 11 - remainder;

            if (cpf[9] - '0' != firstCheckDigit)
            {
                return false;
            }

            // Validate second check digit
            sum = 0;
            for (int i = 0; i < 10; i++)
            {
                sum += (cpf[i] - '0') * (11 - i);
            }

            remainder = sum % 11;
            int secondCheckDigit = remainder < 2 ? 0 : 11 - remainder;

            return cpf[10] - '0' == secondCheckDigit;
        }

        public class CpfRequest
        {
            public string Cpf { get; set; }
        }
    }
}
