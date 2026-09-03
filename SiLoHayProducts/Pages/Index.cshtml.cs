using System.Net.Mail;
using Microsoft.AspNetCore.Http;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SiLoHayProductsNew.Data;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using System.Numerics;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;

namespace SiLoHayProductsNew.Pages
{
    //public class Customer
    //{
    //    [Required(AllowEmptyStrings = false)]
    //    public string? FullName { get; set; }

    //    [Required(AllowEmptyStrings = false)]
    //    public string? EmailAddress { get; set; }

    //}
    [IgnoreAntiforgeryToken]
    public class IndexModel : PageModel
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        public IndexModel(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }

        public List<Product> availableProducts = new List<Product>();
        public Product product = new Product();

        public void OnGet()
        {
            var httpContext = this.HttpContext;
            string hostName = httpContext.Request.Host.Host.ToLower();
            if (hostName == "ecficiens.com")
            {
                httpContext.Response.Redirect("/ContactUs");
                return;
            }
            string SiLoHayConnString = _configuration.GetConnectionString("SiLoHayConnString");

            Product products = new Product();
            availableProducts = products.GetProducts(SiLoHayConnString);
            //product = availableProducts.First();


        }

        public class SendOrderRequest
        {
            public string ProductId { get; set; }
            public string Name { get; set; }
            public string ImageUrl { get; set; }
            public string FullName { get; set; }
            public string Email { get; set; }
            public string Phone { get; set; }
            public string RecaptchaResponse { get; set; }
        }



        public async Task<IActionResult> OnPostSendJson([FromBody] JsonElement body)
        {
            // JSON
            var productId = body.GetProperty("productId").GetInt32();
            var name = body.GetProperty("name").GetString();
            var imageUrl = body.GetProperty("imageUrl").GetString();
            var fullName = body.GetProperty("fullName").GetString();
            var email = body.GetProperty("email").GetString();
            var phone = body.GetProperty("phone").GetString();
            var recaptchaResponse = body.GetProperty("recaptchaResponse").GetString();



            string emailBody = string.Empty;



            // ===== reCAPTCHA =====
            var secretKey = _configuration["ReCaptchaSettings:SecretKey"];
            var verificationUrl = _configuration["ReCaptchaSettings:VerificationUrl"];



            var client = _httpClientFactory.CreateClient();
            var response = await client.PostAsync(
            verificationUrl,
            new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("secret", secretKey),
                new KeyValuePair<string, string>("response", recaptchaResponse)
            })
            );

            var jsonString = await response.Content.ReadAsStringAsync();
            var captchaResult = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonString);

            if (captchaResult.ContainsKey("score") &&
            double.TryParse(captchaResult["score"]?.ToString(), out double score) &&
            score < 0.5)
            {
                return new JsonResult("recaptcha_score_too_low");
            }



            // ===== Email body =====
            emailBody += "Interés en {0}<br>" +
            "ProductId = {1}<br>" +
            "articulo: {2}<br>" +
            "Cliente: {3}<br>" +
            "email: {4}<br>" +
            "telefono: {5}<br>" +
            "image: {6}";



            emailBody = string.Format(
            emailBody,
            name,
            productId,
            name,
            fullName,
            email,
            phone,
            imageUrl
            );



            // ===== SMTP =====
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



            return new JsonResult(new
            {
                success = true,
                message = "Correo enviado exitosamente"
            });
        }


        public async Task<ActionResult> OnPostSend(string productId, string name, string imageUrl, string fullName, string email, string phone, string recaptchaResponse)
        {
            string emailBody = string.Empty;
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
                return new JsonResult(new { success = false, message = "reCAPTCHA validation failed" });
            }

            // Check the score (optional, adjust threshold as needed)
            if (captchaResult.ContainsKey("score") && double.TryParse(captchaResult["score"].ToString(), out double score) && score < 0.5)
            {
                return new JsonResult("recaptcha_score_too_low");
            }

            //if ok proceed
            emailBody += "Interés en {0}<br>" +
                "ProductId = {1}<br>" +
                "articulo: {2}<br>" +
                "Cliente: {3}<br>" +
                "email: {4}<br>" +
                "telefono: {5}<br>" +
                "image: {6}";

            emailBody = string.Format(emailBody, name, productId, name, fullName, email, phone, imageUrl);
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
            return new JsonResult(new
            {
                success = true,
                message = "Correo enviado exitosamente"
            });
        }
    }
}