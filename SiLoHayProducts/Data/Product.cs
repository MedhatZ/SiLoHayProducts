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
