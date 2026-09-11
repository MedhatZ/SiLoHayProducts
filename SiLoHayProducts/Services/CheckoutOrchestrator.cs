using SiLoHayProductsNew.Data;
using SiLoHayProductsNew.Options;
using SiLoHayProductsNew.Services;
using Microsoft.Extensions.Options;

namespace SiLoHayProductsNew.Services
{
    public class PaymentLinkResult
    {
        public long OrderId { get; set; }
        public long PaymentId { get; set; }
        public string PaymentType { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string CheckoutUrl { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;
    }

    public class CheckoutOrchestrator
    {
        private readonly IConfiguration _configuration;
        private readonly OrderRepository _orders;
        private readonly StripeCheckoutService _stripe;
        private readonly StripeOptions _stripeOptions;

        public CheckoutOrchestrator(
            IConfiguration configuration,
            OrderRepository orders,
            StripeCheckoutService stripe,
            IOptions<StripeOptions> stripeOptions)
        {
            _configuration = configuration;
            _orders = orders;
            _stripe = stripe;
            _stripeOptions = stripeOptions.Value;
        }

        public async Task<PaymentLinkResult> CreateCheckoutForProductAsync(
            int productId,
            string baseUrl,
            string? customerName = null,
            string? customerEmail = null,
            string? customerPhone = null,
            CancellationToken cancellationToken = default)
        {
            var conn = _configuration.GetConnectionString("SiLoHayConnString")
                ?? throw new InvalidOperationException("Connection string missing.");

            var product = new Product().GetById(conn, productId)
                ?? throw new InvalidOperationException($"Product {productId} not found.");

            var fullPrice = product.Price;
            var deposit = product.Prepayment ?? 0m;
            var availableNow = product.IsAvailableNow;

            string paymentType;
            decimal amount;
            decimal remaining;

            if (availableNow)
            {
                paymentType = PaymentTypes.Full;
                amount = fullPrice;
                remaining = 0m;
                deposit = 0m;
            }
            else
            {
                if (deposit <= 0m || deposit >= fullPrice)
                {
                    throw new InvalidOperationException(
                        "Product requires a valid Prepayment less than OurPrice for deposit checkout.");
                }

                paymentType = PaymentTypes.Deposit;
                amount = deposit;
                remaining = fullPrice - deposit;
            }

            var order = new SiLoHayOrder
            {
                ProductId = product.ProductId,
                ProductName = product.ProductName ?? $"Product {product.ProductId}",
                FullPrice = fullPrice,
                DepositAmount = deposit,
                RemainingAmount = remaining,
                Status = OrderStatuses.PendingPayment,
                WasAvailableNow = availableNow,
                CustomerName = customerName,
                CustomerEmail = customerEmail,
                CustomerPhone = customerPhone
            };

            var orderId = await _orders.CreateOrderAsync(order, cancellationToken);
            order.OrderId = orderId;

            var payment = new SiLoHayOrderPayment
            {
                OrderId = orderId,
                PaymentType = paymentType,
                Amount = amount,
                Status = PaymentStatuses.Pending
            };

            var paymentId = await _orders.CreatePaymentAsync(payment, cancellationToken);
            payment.PaymentId = paymentId;

            var successUrl = BuildAbsoluteUrl(baseUrl, _stripeOptions.SuccessPath) +
                             "?session_id={CHECKOUT_SESSION_ID}";
            var cancelUrl = BuildAbsoluteUrl(baseUrl, _stripeOptions.CancelPath);

            var session = await _stripe.CreateSessionAsync(order, payment, successUrl, cancelUrl, cancellationToken);
            await _orders.UpdatePaymentSessionAsync(paymentId, session.SessionId, session.Url, cancellationToken);

            return new PaymentLinkResult
            {
                OrderId = orderId,
                PaymentId = paymentId,
                PaymentType = paymentType,
                Amount = amount,
                CheckoutUrl = session.Url,
                SessionId = session.SessionId
            };
        }

        public async Task<PaymentLinkResult> CreateRemainingBalanceCheckoutAsync(
            long orderId,
            string baseUrl,
            CancellationToken cancellationToken = default)
        {
            var order = await _orders.GetOrderAsync(orderId, cancellationToken)
                ?? throw new InvalidOperationException($"Order {orderId} not found.");

            if (order.Status != OrderStatuses.DepositPaid)
            {
                throw new InvalidOperationException(
                    $"Order {orderId} is not ready for remaining payment. Status={order.Status}");
            }

            if (order.RemainingAmount <= 0m)
            {
                throw new InvalidOperationException($"Order {orderId} has no remaining balance.");
            }

            var payment = new SiLoHayOrderPayment
            {
                OrderId = order.OrderId,
                PaymentType = PaymentTypes.Remaining,
                Amount = order.RemainingAmount,
                Status = PaymentStatuses.Pending
            };

            var paymentId = await _orders.CreatePaymentAsync(payment, cancellationToken);
            payment.PaymentId = paymentId;

            var successUrl = BuildAbsoluteUrl(baseUrl, _stripeOptions.SuccessPath) +
                             "?session_id={CHECKOUT_SESSION_ID}";
            var cancelUrl = BuildAbsoluteUrl(baseUrl, _stripeOptions.CancelPath);

            var session = await _stripe.CreateSessionAsync(order, payment, successUrl, cancelUrl, cancellationToken);
            await _orders.UpdatePaymentSessionAsync(paymentId, session.SessionId, session.Url, cancellationToken);

            return new PaymentLinkResult
            {
                OrderId = order.OrderId,
                PaymentId = paymentId,
                PaymentType = PaymentTypes.Remaining,
                Amount = order.RemainingAmount,
                CheckoutUrl = session.Url,
                SessionId = session.SessionId
            };
        }

        private static string BuildAbsoluteUrl(string baseUrl, string path)
        {
            var trimmedBase = baseUrl.TrimEnd('/');
            var trimmedPath = path.StartsWith('/') ? path : "/" + path;
            return trimmedBase + trimmedPath;
        }
    }
}
