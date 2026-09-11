using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SiLoHayProductsNew.Services;

namespace SiLoHayProductsNew.Pages
{
    public class CheckoutModel : PageModel
    {
        private readonly CheckoutOrchestrator _orchestrator;
        private readonly ILogger<CheckoutModel> _logger;

        public CheckoutModel(CheckoutOrchestrator orchestrator, ILogger<CheckoutModel> logger)
        {
            _orchestrator = orchestrator;
            _logger = logger;
        }

        [BindProperty]
        public int ProductId { get; set; }

        public string? ErrorMessage { get; set; }

        public void OnGet()
        {
            ErrorMessage = "Use POST with a product id to start checkout.";
        }

        public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
        {
            if (ProductId <= 0)
            {
                ErrorMessage = "ProductId inválido.";
                return Page();
            }

            try
            {
                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var result = await _orchestrator.CreateCheckoutForProductAsync(
                    ProductId,
                    baseUrl,
                    cancellationToken: cancellationToken);

                return Redirect(result.CheckoutUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create checkout for product {ProductId}", ProductId);
                ErrorMessage = "No se pudo iniciar el pago. Intente de nuevo más tarde.";
                return Page();
            }
        }
    }
}
