using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SiLoHayProductsNew.Services;

namespace SiLoHayProductsNew.Pages
{
    [IgnoreAntiforgeryToken]
    public class StripeWebhookModel : PageModel
    {
        private readonly StripeWebhookProcessor _processor;
        private readonly ILogger<StripeWebhookModel> _logger;

        public StripeWebhookModel(StripeWebhookProcessor processor, ILogger<StripeWebhookModel> logger)
        {
            _processor = processor;
            _logger = logger;
        }

        public IActionResult OnGet() => BadRequest("Stripe webhooks require POST.");

        public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
        {
            string json;
            using (var reader = new StreamReader(Request.Body))
            {
                json = await reader.ReadToEndAsync(cancellationToken);
            }

            var signature = Request.Headers["Stripe-Signature"].ToString();
            if (string.IsNullOrWhiteSpace(signature))
            {
                return BadRequest("Missing Stripe-Signature header.");
            }

            try
            {
                await _processor.ProcessAsync(json, signature, cancellationToken);
                return new StatusCodeResult(StatusCodes.Status200OK);
            }
            catch (Stripe.StripeException ex)
            {
                _logger.LogWarning(ex, "Stripe webhook rejected.");
                return BadRequest();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stripe webhook processing failed.");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
