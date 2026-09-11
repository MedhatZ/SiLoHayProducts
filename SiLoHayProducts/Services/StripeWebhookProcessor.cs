using SiLoHayProductsNew.Data;
using SiLoHayProductsNew.Options;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace SiLoHayProductsNew.Services
{
    public class StripeWebhookProcessor
    {
        private readonly OrderRepository _orders;
        private readonly StripeOptions _options;
        private readonly ILogger<StripeWebhookProcessor> _logger;

        public StripeWebhookProcessor(
            OrderRepository orders,
            IOptions<StripeOptions> options,
            ILogger<StripeWebhookProcessor> logger)
        {
            _orders = orders;
            _options = options.Value;
            _logger = logger;
        }

        public async Task ProcessAsync(string json, string stripeSignature, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_options.WebhookSecret))
            {
                throw new InvalidOperationException("Stripe WebhookSecret is not configured.");
            }

            Event stripeEvent;
            try
            {
                // throwOnApiVersionMismatch: false — Stripe CLI may use a newer API version than Stripe.net.
                stripeEvent = EventUtility.ConstructEvent(
                    json,
                    stripeSignature,
                    _options.WebhookSecret,
                    tolerance: 300,
                    throwOnApiVersionMismatch: false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Stripe webhook signature/API verification failed. Secret configured={HasSecret}",
                    !string.IsNullOrWhiteSpace(_options.WebhookSecret));
                throw;
            }

            var isNew = await _orders.TryRegisterStripeEventAsync(stripeEvent.Id, stripeEvent.Type, cancellationToken);
            if (!isNew)
            {
                _logger.LogInformation("Duplicate Stripe event ignored: {EventId}", stripeEvent.Id);
                return;
            }

            switch (stripeEvent.Type)
            {
                case EventTypes.CheckoutSessionCompleted:
                    await HandleSessionAsync(stripeEvent, paid: true, cancellationToken);
                    break;
                case EventTypes.CheckoutSessionAsyncPaymentSucceeded:
                    await HandleSessionAsync(stripeEvent, paid: true, cancellationToken);
                    break;
                case EventTypes.CheckoutSessionAsyncPaymentFailed:
                    await HandleSessionAsync(stripeEvent, paid: false, cancellationToken);
                    break;
                default:
                    _logger.LogInformation("Unhandled Stripe event type: {Type}", stripeEvent.Type);
                    break;
            }
        }

        /// <summary>
        /// Backup confirmation from success page: load Checkout Session from Stripe API and mark paid if confirmed.
        /// Idempotent with webhook updates.
        /// </summary>
        public async Task<bool> SyncSessionFromStripeAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return false;
            }

            StripeConfiguration.ApiKey = _options.SecretKey;
            var service = new SessionService();
            var session = await service.GetAsync(sessionId, cancellationToken: cancellationToken);

            var payment = await _orders.GetPaymentBySessionIdAsync(session.Id, cancellationToken);
            if (payment == null)
            {
                _logger.LogWarning("Sync: no local payment for session {SessionId}", session.Id);
                return false;
            }

            if (payment.Status == PaymentStatuses.Paid)
            {
                return true;
            }

            var paid = session.PaymentStatus == "paid" ||
                       string.Equals(session.Status, "complete", StringComparison.OrdinalIgnoreCase);

            if (!paid)
            {
                _logger.LogInformation("Sync: session {SessionId} not paid yet. PaymentStatus={Status}",
                    session.Id, session.PaymentStatus);
                return false;
            }

            await _orders.MarkPaymentPaidAsync(payment.PaymentId, session.PaymentIntentId, cancellationToken);
            await UpdateOrderAfterPaidAsync(payment, cancellationToken);
            _logger.LogInformation("Sync: marked payment {PaymentId} paid for session {SessionId}",
                payment.PaymentId, session.Id);
            return true;
        }

        private async Task HandleSessionAsync(Event stripeEvent, bool paid, CancellationToken cancellationToken)
        {
            Session? session = stripeEvent.Data.Object as Session;
            if (session == null)
            {
                _logger.LogWarning("Stripe event {EventId} did not contain a Checkout Session.", stripeEvent.Id);
                return;
            }

            if (string.IsNullOrWhiteSpace(session.Id))
            {
                _logger.LogWarning("Stripe event {EventId} Checkout Session has empty Id.", stripeEvent.Id);
                return;
            }

            var payment = await _orders.GetPaymentBySessionIdAsync(session.Id, cancellationToken);
            if (payment == null)
            {
                _logger.LogWarning("No local payment found for session {SessionId}", session.Id);
                return;
            }

            if (payment.Status == PaymentStatuses.Paid && paid)
            {
                return;
            }

            var paymentIntentId = session.PaymentIntentId;

            if (paid)
            {
                if (session.PaymentStatus == "unpaid" &&
                    stripeEvent.Type == EventTypes.CheckoutSessionCompleted)
                {
                    _logger.LogInformation(
                        "Session {SessionId} completed with unpaid status; waiting for async success.",
                        session.Id);
                    return;
                }

                await _orders.MarkPaymentPaidAsync(payment.PaymentId, paymentIntentId, cancellationToken);
                await UpdateOrderAfterPaidAsync(payment, cancellationToken);
            }
            else
            {
                await _orders.MarkPaymentFailedAsync(payment.PaymentId, cancellationToken);
                await _orders.UpdateOrderStatusAsync(payment.OrderId, OrderStatuses.PaymentFailed, cancellationToken);
            }
        }

        private async Task UpdateOrderAfterPaidAsync(SiLoHayOrderPayment payment, CancellationToken cancellationToken)
        {
            if (payment.PaymentType == PaymentTypes.Full || payment.PaymentType == PaymentTypes.Remaining)
            {
                await _orders.UpdateOrderStatusAsync(payment.OrderId, OrderStatuses.FullyPaid, cancellationToken);
                return;
            }

            if (payment.PaymentType == PaymentTypes.Deposit)
            {
                await _orders.UpdateOrderStatusAsync(payment.OrderId, OrderStatuses.DepositPaid, cancellationToken);
            }
        }
    }
}
