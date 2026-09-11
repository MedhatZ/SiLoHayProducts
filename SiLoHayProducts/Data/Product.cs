using System.Data;
using Microsoft.Data.SqlClient;

namespace SiLoHayProductsNew.Data
{
    public class Product
    {
        public readonly IConfiguration _configuration;

        public string? ImageUrl { get; set; }
        public string? ImageUrl2 { get; set; }
        public string? ProductName { get; set; }
        public string? ASIN { get; set; }
        public decimal PriceNew { get; set; }
        public decimal AmazonPriceUsed { get; set; }
        public decimal Price { get; set; }
        public bool IsAvailable { get; set; }
        public string? AmazonLink { get; set; }
        public string? LinkCustomerCompare { get; set; }
        public int ProductId { get; set; }
        public bool IsAvailableNow { get; set; }
        public decimal? Prepayment { get; set; }
        public bool ShowComparisonMsg { get; set; }
        public string? StoreUsedForComparison { get; set; }


        public List<Product> GetProducts(string SiLoHayConnString)
        {
            string connString = SiLoHayConnString;
            List<Product> productsList = new List<Product>();

            SqlConnection sqlConn = new SqlConnection(connString);
            sqlConn.Open();
            string sqlQuery = "exec usp_TienditaGetItems";

            SqlCommand cmd = new SqlCommand(sqlQuery, sqlConn);

            SqlDataReader reader = cmd.ExecuteReader();


            while (reader.Read())
            {
                Product product = new Product();
                product.ProductName = reader["ProductName"].ToString();
                product.Price = (decimal)reader["OurPrice"];
                product.ImageUrl = reader["ImageUrl"].ToString();
                product.ImageUrl2 = reader["ImageUrl2"].ToString();
                product.LinkCustomerCompare = reader["linkCustomerCompare"].ToString();
                product.PriceNew = (decimal)reader["PriceNew"];
                product.ProductId = (int)reader["SiLoHayProductId"];
                product.IsAvailableNow = (bool)reader["IsAvailableNow"];
                product.Prepayment = (decimal)reader["Prepayment"];
                product.StoreUsedForComparison = reader["StoreUsedForComparison"].ToString();
                product.ShowComparisonMsg = (bool)reader["ShowComparisonMsg"];
                productsList.Add(product);

            }
            sqlConn.Close();
            return productsList;
        }

        public Product? GetById(string SiLoHayConnString, int productId)
        {
            using var sqlConn = new SqlConnection(SiLoHayConnString);
            sqlConn.Open();

            const string sqlQuery = @"
SELECT TOP 1
    SiLoHayProductId,
    ProductName,
    OurPrice,
    ImageUrl,
    ImageUrl2,
    linkCustomerCompare,
    PriceNew,
    IsAvailableNow,
    Prepayment,
    StoreUsedForComparison,
    ShowComparisonMsg,
    IsAvailable
FROM dbo.SiLoHayProduct
WHERE SiLoHayProductId = @ProductId;";

            using var cmd = new SqlCommand(sqlQuery, sqlConn);
            cmd.Parameters.AddWithValue("@ProductId", productId);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
            {
                return null;
            }

            return new Product
            {
                ProductId = (int)reader["SiLoHayProductId"],
                ProductName = reader["ProductName"].ToString(),
                Price = (decimal)reader["OurPrice"],
                ImageUrl = reader["ImageUrl"]?.ToString(),
                ImageUrl2 = reader["ImageUrl2"] == DBNull.Value ? null : reader["ImageUrl2"].ToString(),
                LinkCustomerCompare = reader["linkCustomerCompare"] == DBNull.Value ? null : reader["linkCustomerCompare"].ToString(),
                PriceNew = reader["PriceNew"] == DBNull.Value ? 0 : (decimal)reader["PriceNew"],
                IsAvailableNow = reader["IsAvailableNow"] != DBNull.Value && (bool)reader["IsAvailableNow"],
                Prepayment = reader["Prepayment"] == DBNull.Value ? null : (decimal?)reader["Prepayment"],
                StoreUsedForComparison = reader["StoreUsedForComparison"] == DBNull.Value ? null : reader["StoreUsedForComparison"].ToString(),
                ShowComparisonMsg = reader["ShowComparisonMsg"] != DBNull.Value && (bool)reader["ShowComparisonMsg"],
                IsAvailable = reader["IsAvailable"] != DBNull.Value && (bool)reader["IsAvailable"]
            };
        }

        /* Add() is to add add products in AddProduct.csjtml */
        public bool Add(string SiLoHayConnString)
        {
            string connString = SiLoHayConnString;


            string sqlInsert = "insert into dbo.SiLoHayProduct " +
                "(ASIN, ProductName, ImageUrl, ImageUrl2, PriceNew, AmazonPriceUsed, OurPrice, IsAvailable, AmazonLink, LinkCustomerCompare, IsAvailableNow, Prepayment, StoreUsedForComparison, ShowComparisonMsg)" +
                "select '{0}', '{1}', '{2}', '{3}', '{4}', '{5}', '{6}', '{7}', '{8}', '{9}', '{10}', '{11}', '{12}', '{13}'";

            sqlInsert = string.Format(sqlInsert, ASIN, ProductName, ImageUrl, ImageUrl2, PriceNew, AmazonPriceUsed, Price, IsAvailable, AmazonLink, LinkCustomerCompare, IsAvailableNow, Prepayment, StoreUsedForComparison, ShowComparisonMsg);

            SqlConnection sqlConn = new SqlConnection(connString);
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = sqlConn;
            cmd.CommandText = sqlInsert;
            cmd.CommandType = CommandType.Text;
            sqlConn.Open();
            cmd.ExecuteNonQuery();

            sqlConn.Close();
            return true;
        }
    }


}
