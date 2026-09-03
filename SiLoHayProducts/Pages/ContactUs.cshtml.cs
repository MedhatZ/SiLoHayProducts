using System.Net.Mail;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Numerics;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SiLoHayProductsNew.Pages
{
    public class ContactUsModel : PageModel
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        public ContactUsModel(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }

        [BindProperty]
        public string? Nombre { get; set; }

        [BindProperty]
        public string? Telefono { get; set; }

        [BindProperty]
        public string? Email { get; set; }

        [BindProperty]
        public string? Titulo { get; set; }

        [BindProperty]
        public string? Pregunta { get; set; }

        public void OnPost()
        {
            string t = Telefono;
            Thread.Sleep(5000);
        }

        public async Task<ActionResult> OnPostSend(string Nombre, string Telefono, string Email, string Titulo, string Pregunta, string recaptchaResponse)
        {
            var secretKey = _configuration["ReCaptchaSettings:SecretKey"];
            var verificationUrl = _configuration["ReCaptchaSettings:VerificationUrl"];

            var client = _httpClientFactory.CreateClient();
            var response = await client.PostAsync(verificationUrl,
                new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("secret", secretKey),
                    new KeyValuePair<string, string>("response", recaptchaResponse)
                }));

            var jsonString = await response.Content.ReadAsStringAsync();
            var captchaResult = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonString);

            if (captchaResult == null || !bool.Parse(captchaResult["success"].ToString()))
            {
                return new JsonResult("recaptcha_failed");
            }

            // Check the score (optional, adjust threshold as needed)
            if (captchaResult.ContainsKey("score") && double.TryParse(captchaResult["score"].ToString(), out double score) && score < 0.5)
            {
                return new JsonResult("recaptcha_score_too_low");
            }

            // If valid, proceed with email sending
            string emailBody = "Pregunta de cliente<br>" +
                "Nombre: {0}<br>" +
                "Telefono: {1}<br>" +
                "Email: {2}<br>" +
                "Titulo: {3}<br>" +
                "Pregunta: {4}<br>";
            emailBody = string.Format(emailBody, Nombre, Telefono, Email, Titulo, Pregunta);
            var smtpClient = new SmtpClient("smtp.office365.com")
            {
                Port = 587,
                Credentials = new NetworkCredential("vmyers@SiLoHay.com", "Mamita@6667"),
                EnableSsl = true
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress("vmyers@SiLoHay.com"),
                Subject = "Orden cliente",
                Body = emailBody,
                IsBodyHtml = true
            };

            mailMessage.To.Add("ayuda@SiLoHay.com");

            smtpClient.Send(mailMessage);
            return new JsonResult("true");
        }
    }
}