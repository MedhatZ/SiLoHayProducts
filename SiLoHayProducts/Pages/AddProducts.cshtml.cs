using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SiLoHayProductsNew.Data;

namespace SiLoHayProductsNew.Pages
{
    public class AddProductsModel : PageModel
    {
        [BindProperty]
        public string? ImageUrl { get; set; }
        [BindProperty]
        public string? ProductName { get; set; }
        [BindProperty]
        public string? ASIN { get; set; }
        [BindProperty]
        public decimal AmazonPriceNew { get; set; }
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
        public void OnPost()
        {
            Product product = new Product();
            product.ProductName = ProductName;
            product.ASIN = ASIN;
            product.AmazonPriceNew = AmazonPriceNew;
            product.AmazonPriceUsed = AmazonPriceUsed;
            product.Price = Price;
            product.IsAvailable = IsAvailable;
            product.IsAvailableNow = IsAvailableNow;
            product.AmazonLink = AmazonLink;
            product.LinkCustomerCompare = LinkCustomerCompare;
            product.ImageUrl = ImageUrl;
            product.Prepayment = Prepayment;
            product.Add();
        }
    }
}
