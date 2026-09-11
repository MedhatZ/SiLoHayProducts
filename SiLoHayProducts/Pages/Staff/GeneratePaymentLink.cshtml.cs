using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using QRCoder;
using SiLoHayProductsNew.Data;
using SiLoHayProductsNew.Options;
using SiLoHayProductsNew.Services;

namespace SiLoHayProductsNew.Pages.Staff
{
    public class GeneratePaymentLinkModel : PageModel
    {
        private readonly CheckoutOrchestrator _orchestrator;
        private readonly OrderRepository _orders;
        private readonly StripeOptions _stripeOptions;
        private readonly ILogger<GeneratePaymentLinkModel> _logger;

        public GeneratePaymentLinkModel(
            CheckoutOrchestrator orchestrator,
            OrderRepository orders,
            IOptions<StripeOptions> stripeOptions,
            ILogger<GeneratePaymentLinkModel> logger)
        {
            _orchestrator = orchestrator;
            _orders = orders;
            _stripeOptions = stripeOptions.Value;
            _logger = logger;
        }

        [BindProperty(SupportsGet = true)]
        public string? Key { get; set; }

        [BindProperty]
        public int? ProductId { get; set; }

        [BindProperty]
        public long? OrderIdForRemaining { get; set; }

        [BindProperty]
        public string Mode { get; set; } = "product";

        public bool IsAuthorized { get; set; }
        public string? ErrorMessage { get; set; }
        public string? CheckoutUrl { get; set; }
        public string? QrDataUrl { get; set; }
        public long? CreatedOrderId { get; set; }
        public string? PaymentType { get; set; }
        public decimal? Amount { get; set; }
        public IReadOnlyList<SiLoHayOrder> OrdersNeedingRemaining { get; set; } = Array.Empty<SiLoHayOrder>();

        public async Task OnGetAsync(CancellationToken cancellationToken)
        {
            IsAuthorized = IsKeyValid();
            if (!IsAuthorized)
            {
                ErrorMessage = "Clave de acceso inválida o faltante. Use ?key=...";
                return;
            }

            try
            {
                OrdersNeedingRemaining = await _orders.GetOrdersNeedingRemainingAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not load orders needing remaining (tables may not exist yet).");
                OrdersNeedingRemaining = Array.Empty<SiLoHayOrder>();
            }
        }

        public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
        {
            IsAuthorized = IsKeyValid();
            if (!IsAuthorized)
            {
                ErrorMessage = "Clave de acceso inválida.";
                return Page();
            }

            try
            {
                OrdersNeedingRemaining = await _orders.GetOrdersNeedingRemainingAsync(cancellationToken);
            }
            catch
            {
                OrdersNeedingRemaining = Array.Empty<SiLoHayOrder>();
            }

            try
            {
                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                PaymentLinkResult result;

                if (string.Equals(Mode, "remaining", StringComparison.OrdinalIgnoreCase))
                {
                    if (!OrderIdForRemaining.HasValue || OrderIdForRemaining.Value <= 0)
                    {
                        ErrorMessage = "Seleccione una orden para el saldo restante.";
                        return Page();
                    }

                    result = await _orchestrator.CreateRemainingBalanceCheckoutAsync(
                        OrderIdForRemaining.Value, baseUrl, cancellationToken);
                }
                else
                {
                    if (!ProductId.HasValue || ProductId.Value <= 0)
                    {
                        ErrorMessage = "Ingrese un ProductId válido.";
                        return Page();
                    }

                    result = await _orchestrator.CreateCheckoutForProductAsync(
                        ProductId.Value, baseUrl, cancellationToken: cancellationToken);
                }

                CheckoutUrl = result.CheckoutUrl;
                CreatedOrderId = result.OrderId;
                PaymentType = result.PaymentType;
                Amount = result.Amount;
                QrDataUrl = BuildQrDataUrl(result.CheckoutUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate payment link.");
                ErrorMessage = ex.Message;
            }

            return Page();
        }

        private bool IsKeyValid()
        {
            var configured = _stripeOptions.StaffAccessKey;
            if (string.IsNullOrWhiteSpace(configured))
            {
                // Dev-friendly: allow when key not configured (warn via empty means open).
                return true;
            }

            return string.Equals(Key, configured, StringComparison.Ordinal);
        }

        private static string BuildQrDataUrl(string url)
        {
            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new PngByteQRCode(data);
            var bytes = qrCode.GetGraphic(8);
            return "data:image/png;base64," + Convert.ToBase64String(bytes);
        }
    }
}
