using Microsoft.Extensions.Options;
using SiLoHayProductsNew.Data;
using SiLoHayProductsNew.Options;
using Stripe;
using Stripe.Checkout;

namespace SiLoHayProductsNew.Services
{
    public class CheckoutSessionResult
    {
        public string SessionId { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }

    public class StripeCheckoutService
    {
        private readonly StripeOptions _options;

        public StripeCheckoutService(IOptions<StripeOptions> options)
        {
            _options = options.Value;
            StripeConfiguration.ApiKey = _options.SecretKey;
        }

        public async Task<CheckoutSessionResult> CreateSessionAsync(
            SiLoHayOrder order,
            SiLoHayOrderPayment payment,
            string successUrl,
            string cancelUrl,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_options.SecretKey))
            {
                throw new InvalidOperationException("Stripe SecretKey is not configured.");
            }

            if (string.IsNullOrWhiteSpace(_options.TaxRateId))
            {
                throw new InvalidOperationException("Stripe TaxRateId is not configured.");
            }

            var paymentMethodTypes = BuildPaymentMethodTypes(payment.PaymentType, payment.Amount);
            var productLabel = BuildProductLabel(order, payment.PaymentType);
            var amountCents = ToCents(payment.Amount);
            if (amountCents <= 0)
            {
                throw new InvalidOperationException("Payment amount must be greater than zero.");
            }

            var options = new SessionCreateOptions
            {
                Mode = "payment",
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,
                // Explicit allow-list only — do NOT omit this (omitting enables Dashboard dynamic methods).
                PaymentMethodTypes = paymentMethodTypes,
                // Hide Link wallet (appears alongside card otherwise).
                WalletOptions = new SessionWalletOptionsOptions
                {
                    Link = new SessionWalletOptionsLinkOptions
                    {
                        Display = "never"
                    }
                },
                LineItems = new List<SessionLineItemOptions>
                {
                    new()
                    {
                        Quantity = 1,
                        TaxRates = new List<string> { _options.TaxRateId },
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = _options.Currency,
                            UnitAmount = amountCents,
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = productLabel,
                                Metadata = new Dictionary<string, string>
                                {
                                    ["productId"] = order.ProductId.ToString(),
                                    ["orderId"] = order.OrderId.ToString()
                                }
                            }
                        }
                    }
                },
                Metadata = new Dictionary<string, string>
                {
                    ["orderId"] = order.OrderId.ToString(),
                    ["paymentId"] = payment.PaymentId.ToString(),
                    ["paymentType"] = payment.PaymentType,
                    ["productId"] = order.ProductId.ToString()
                }
            };

            if (payment.PaymentType == PaymentTypes.Deposit)
            {
                // Business rule: deposit is non-refundable (policy). Shown in product description.
                options.LineItems[0].PriceData!.ProductData!.Description =
                    "Pago inicial no reembolsable según las reglas del negocio.";
            }

            var service = new SessionService();
            var session = await service.CreateAsync(options, cancellationToken: cancellationToken);

            if (string.IsNullOrWhiteSpace(session.Url))
            {
                throw new InvalidOperationException("Stripe did not return a Checkout Session URL.");
            }

            return new CheckoutSessionResult
            {
                SessionId = session.Id,
                Url = session.Url
            };
        }

        /// <summary>
        /// Deposit: card only.
        /// Full / Remaining: card + klarna (Klarna omitted when below optional KlarnaMinAmountUsd).
        /// </summary>
        public List<string> BuildPaymentMethodTypes(string paymentType, decimal amountUsd)
        {
            var methods = new List<string> { "card" };

            if (paymentType == PaymentTypes.Deposit)
            {
                return methods;
            }

            // Full and Remaining may include Klarna subject to optional configurable threshold.
            if (_options.KlarnaMinAmountUsd.HasValue && amountUsd < _options.KlarnaMinAmountUsd.Value)
            {
                return methods;
            }

            methods.Add("klarna");
            return methods;
        }

        private static string BuildProductLabel(SiLoHayOrder order, string paymentType)
        {
            return paymentType switch
            {
                PaymentTypes.Deposit => $"{order.ProductName} — Pago inicial",
                PaymentTypes.Remaining => $"{order.ProductName} — Saldo restante",
                _ => order.ProductName
            };
        }

        private static long ToCents(decimal amount)
        {
            return (long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero);
        }
    }
}
