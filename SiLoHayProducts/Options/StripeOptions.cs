namespace SiLoHayProductsNew.Options
{
    public class StripeOptions
    {
        public const string SectionName = "Stripe";

        public string SecretKey { get; set; } = string.Empty;
        public string PublishableKey { get; set; } = string.Empty;
        public string WebhookSecret { get; set; } = string.Empty;
        public string TaxRateId { get; set; } = string.Empty;
        public string Currency { get; set; } = "usd";
        /// <summary>
        /// Optional business-rule minimum (USD) before Klarna is offered on full/remaining sessions.
        /// If null, Klarna is included and Stripe eligibility still applies at Checkout.
        /// </summary>
        public decimal? KlarnaMinAmountUsd { get; set; }
        public string SuccessPath { get; set; } = "/PaymentSuccess";
        public string CancelPath { get; set; } = "/PaymentCancel";
        /// <summary>
        /// Shared secret for staff GeneratePaymentLink page (query or form). Empty = page open (dev only).
        /// </summary>
        public string StaffAccessKey { get; set; } = string.Empty;
    }
}
