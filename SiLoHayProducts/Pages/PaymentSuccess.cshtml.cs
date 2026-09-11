using Microsoft.AspNetCore.Mvc.RazorPages;
using SiLoHayProductsNew.Services;

namespace SiLoHayProductsNew.Pages
{
    public class PaymentSuccessModel : PageModel
    {
        private readonly StripeWebhookProcessor _stripeSync;
        private readonly ILogger<PaymentSuccessModel> _logger;

        public PaymentSuccessModel(StripeWebhookProcessor stripeSync, ILogger<PaymentSuccessModel> logger)
        {
            _stripeSync = stripeSync;
            _logger = logger;
        }

        public string? SessionId { get; set; }
        public bool PaymentConfirmed { get; set; }

        public async Task OnGetAsync(string? session_id, CancellationToken cancellationToken)
        {
            SessionId = session_id;

            if (string.IsNullOrWhiteSpace(session_id))
            {
                return;
            }

            try
            {
                // Backup when webhook is delayed/unavailable (e.g. local without stripe listen).
                // Idempotent with webhook processing.
                PaymentConfirmed = await _stripeSync.SyncSessionFromStripeAsync(session_id, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not sync Stripe session {SessionId} on success page.", session_id);
            }
        }
    }
}
