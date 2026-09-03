using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SiLoHayProductsNew.Data;

namespace SiLoHayProducts.Pages
{
    public class AddProductModel : PageModel
    {
        private readonly IConfiguration _configuration;
        public AddProductModel(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
        }

        [BindProperty]
        public string? ImageUrl { get; set; }
        [BindProperty]
        public string? ImageUrl2 { get; set; }
        [BindProperty]
        public string? ProductName { get; set; }
        [BindProperty]
        public string? ASIN { get; set; }
        [BindProperty]
        public decimal PriceNew { get; set; }
        [BindProperty]
        public decimal AmazonPriceUsed { get; set; }
        [BindProperty]
        public decimal Price { get; set; }
        [BindProperty]
        public bool IsAvailable { get; set; }
        [BindProperty]
        public string? AmazonLink { get; set; }
        [BindProperty]
        public string? LinkCustomerCompare { get; set; }
        [BindProperty]
        public bool IsAvailableNow { get; set; }
        [BindProperty]
        public decimal Prepayment { get; set; }
        [BindProperty]
        public bool ShowComparisonMsg { get; set; }
        [BindProperty]
        public string? StoreUsedForComparison { get; set; }
        public void OnGet()
        {
        }
        public void OnPost()
        {
            string SiLoHayConnString = _configuration.GetConnectionString("SiLoHayConnString");
  
            Product product = new Product();
            product.ProductName = ProductName;
            product.ASIN = ASIN;
            product.PriceNew = PriceNew;
            product.AmazonPriceUsed = AmazonPriceUsed;
            product.Price = Price;
            product.IsAvailable = IsAvailable;
            product.IsAvailableNow = IsAvailableNow;
            product.AmazonLink = AmazonLink;
            product.LinkCustomerCompare = LinkCustomerCompare;
            product.ImageUrl = ImageUrl;
            product.ImageUrl2 = ImageUrl2;
            product.Prepayment = Prepayment;
            product.ShowComparisonMsg = ShowComparisonMsg;
            product.StoreUsedForComparison = StoreUsedForComparison;
            product.Add(SiLoHayConnString);
        }
    }
}
