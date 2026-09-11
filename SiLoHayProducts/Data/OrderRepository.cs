using Microsoft.Data.SqlClient;

namespace SiLoHayProductsNew.Data
{
    public class OrderRepository
    {
        private readonly string _connectionString;

        public OrderRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("SiLoHayConnString")
                ?? throw new InvalidOperationException("Connection string SiLoHayConnString is missing.");
        }

        public async Task<long> CreateOrderAsync(SiLoHayOrder order, CancellationToken cancellationToken = default)
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);

            const string sql = @"
INSERT INTO dbo.SiLoHayOrder
    (ProductId, ProductName, FullPrice, DepositAmount, RemainingAmount, Status, WasAvailableNow,
     CustomerEmail, CustomerName, CustomerPhone, CreatedAtUtc, UpdatedAtUtc)
OUTPUT INSERTED.OrderId
VALUES
    (@ProductId, @ProductName, @FullPrice, @DepositAmount, @RemainingAmount, @Status, @WasAvailableNow,
     @CustomerEmail, @CustomerName, @CustomerPhone, SYSUTCDATETIME(), SYSUTCDATETIME());";

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ProductId", order.ProductId);
            cmd.Parameters.AddWithValue("@ProductName", order.ProductName);
            cmd.Parameters.AddWithValue("@FullPrice", order.FullPrice);
            cmd.Parameters.AddWithValue("@DepositAmount", order.DepositAmount);
            cmd.Parameters.AddWithValue("@RemainingAmount", order.RemainingAmount);
            cmd.Parameters.AddWithValue("@Status", order.Status);
            cmd.Parameters.AddWithValue("@WasAvailableNow", order.WasAvailableNow);
            cmd.Parameters.AddWithValue("@CustomerEmail", (object?)order.CustomerEmail ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@CustomerName", (object?)order.CustomerName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@CustomerPhone", (object?)order.CustomerPhone ?? DBNull.Value);

            var result = await cmd.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt64(result);
        }

        public async Task<SiLoHayOrder?> GetOrderAsync(long orderId, CancellationToken cancellationToken = default)
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);

            const string sql = @"
SELECT OrderId, ProductId, ProductName, FullPrice, DepositAmount, RemainingAmount, Status, WasAvailableNow,
       CustomerEmail, CustomerName, CustomerPhone, CreatedAtUtc, UpdatedAtUtc
FROM dbo.SiLoHayOrder WHERE OrderId = @OrderId;";

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@OrderId", orderId);

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            return MapOrder(reader);
        }

        public async Task<IReadOnlyList<SiLoHayOrder>> GetOrdersNeedingRemainingAsync(CancellationToken cancellationToken = default)
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);

            const string sql = @"
SELECT OrderId, ProductId, ProductName, FullPrice, DepositAmount, RemainingAmount, Status, WasAvailableNow,
       CustomerEmail, CustomerName, CustomerPhone, CreatedAtUtc, UpdatedAtUtc
FROM dbo.SiLoHayOrder
WHERE Status = @Status
ORDER BY OrderId DESC;";

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Status", OrderStatuses.DepositPaid);

            var list = new List<SiLoHayOrder>();
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                list.Add(MapOrder(reader));
            }

            return list;
        }

        public async Task UpdateOrderStatusAsync(long orderId, string status, CancellationToken cancellationToken = default)
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);

            const string sql = @"
UPDATE dbo.SiLoHayOrder
SET Status = @Status, UpdatedAtUtc = SYSUTCDATETIME()
WHERE OrderId = @OrderId;";

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Status", status);
            cmd.Parameters.AddWithValue("@OrderId", orderId);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task<long> CreatePaymentAsync(SiLoHayOrderPayment payment, CancellationToken cancellationToken = default)
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);

            const string sql = @"
INSERT INTO dbo.SiLoHayOrderPayment
    (OrderId, PaymentType, Amount, Status, StripeSessionId, StripePaymentIntentId, CheckoutUrl, CreatedAtUtc, PaidAtUtc)
OUTPUT INSERTED.PaymentId
VALUES
    (@OrderId, @PaymentType, @Amount, @Status, @StripeSessionId, @StripePaymentIntentId, @CheckoutUrl, SYSUTCDATETIME(), NULL);";

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@OrderId", payment.OrderId);
            cmd.Parameters.AddWithValue("@PaymentType", payment.PaymentType);
            cmd.Parameters.AddWithValue("@Amount", payment.Amount);
            cmd.Parameters.AddWithValue("@Status", payment.Status);
            cmd.Parameters.AddWithValue("@StripeSessionId", (object?)payment.StripeSessionId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@StripePaymentIntentId", (object?)payment.StripePaymentIntentId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@CheckoutUrl", (object?)payment.CheckoutUrl ?? DBNull.Value);

            var result = await cmd.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt64(result);
        }

        public async Task UpdatePaymentSessionAsync(long paymentId, string sessionId, string checkoutUrl, CancellationToken cancellationToken = default)
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);

            const string sql = @"
UPDATE dbo.SiLoHayOrderPayment
SET StripeSessionId = @SessionId, CheckoutUrl = @CheckoutUrl
WHERE PaymentId = @PaymentId;";

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@SessionId", sessionId);
            cmd.Parameters.AddWithValue("@CheckoutUrl", checkoutUrl);
            cmd.Parameters.AddWithValue("@PaymentId", paymentId);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task<SiLoHayOrderPayment?> GetPaymentBySessionIdAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);

            const string sql = @"
SELECT PaymentId, OrderId, PaymentType, Amount, Status, StripeSessionId, StripePaymentIntentId, CheckoutUrl, CreatedAtUtc, PaidAtUtc
FROM dbo.SiLoHayOrderPayment
WHERE StripeSessionId = @SessionId;";

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@SessionId", sessionId);

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            return MapPayment(reader);
        }

        public async Task MarkPaymentPaidAsync(long paymentId, string? paymentIntentId, CancellationToken cancellationToken = default)
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);

            const string sql = @"
UPDATE dbo.SiLoHayOrderPayment
SET Status = @Status,
    StripePaymentIntentId = COALESCE(@PaymentIntentId, StripePaymentIntentId),
    PaidAtUtc = SYSUTCDATETIME()
WHERE PaymentId = @PaymentId;";

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Status", PaymentStatuses.Paid);
            cmd.Parameters.AddWithValue("@PaymentIntentId", (object?)paymentIntentId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@PaymentId", paymentId);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task MarkPaymentFailedAsync(long paymentId, CancellationToken cancellationToken = default)
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);

            const string sql = @"
UPDATE dbo.SiLoHayOrderPayment
SET Status = @Status
WHERE PaymentId = @PaymentId;";

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Status", PaymentStatuses.Failed);
            cmd.Parameters.AddWithValue("@PaymentId", paymentId);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task<bool> TryRegisterStripeEventAsync(string eventId, string eventType, CancellationToken cancellationToken = default)
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);

            const string sql = @"
IF EXISTS (SELECT 1 FROM dbo.SiLoHayStripeEvent WHERE EventId = @EventId)
    SELECT 0;
ELSE
BEGIN
    INSERT INTO dbo.SiLoHayStripeEvent (EventId, EventType, ProcessedAtUtc)
    VALUES (@EventId, @EventType, SYSUTCDATETIME());
    SELECT 1;
END";

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@EventId", eventId);
            cmd.Parameters.AddWithValue("@EventType", eventType);
            var result = await cmd.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt32(result) == 1;
        }

        private static SiLoHayOrder MapOrder(SqlDataReader reader)
        {
            return new SiLoHayOrder
            {
                OrderId = reader.GetInt64(reader.GetOrdinal("OrderId")),
                ProductId = reader.GetInt32(reader.GetOrdinal("ProductId")),
                ProductName = reader.GetString(reader.GetOrdinal("ProductName")),
                FullPrice = reader.GetDecimal(reader.GetOrdinal("FullPrice")),
                DepositAmount = reader.GetDecimal(reader.GetOrdinal("DepositAmount")),
                RemainingAmount = reader.GetDecimal(reader.GetOrdinal("RemainingAmount")),
                Status = reader.GetString(reader.GetOrdinal("Status")),
                WasAvailableNow = reader.GetBoolean(reader.GetOrdinal("WasAvailableNow")),
                CustomerEmail = reader.IsDBNull(reader.GetOrdinal("CustomerEmail")) ? null : reader.GetString(reader.GetOrdinal("CustomerEmail")),
                CustomerName = reader.IsDBNull(reader.GetOrdinal("CustomerName")) ? null : reader.GetString(reader.GetOrdinal("CustomerName")),
                CustomerPhone = reader.IsDBNull(reader.GetOrdinal("CustomerPhone")) ? null : reader.GetString(reader.GetOrdinal("CustomerPhone")),
                CreatedAtUtc = reader.GetDateTime(reader.GetOrdinal("CreatedAtUtc")),
                UpdatedAtUtc = reader.GetDateTime(reader.GetOrdinal("UpdatedAtUtc"))
            };
        }

        private static SiLoHayOrderPayment MapPayment(SqlDataReader reader)
        {
            return new SiLoHayOrderPayment
            {
                PaymentId = reader.GetInt64(reader.GetOrdinal("PaymentId")),
                OrderId = reader.GetInt64(reader.GetOrdinal("OrderId")),
                PaymentType = reader.GetString(reader.GetOrdinal("PaymentType")),
                Amount = reader.GetDecimal(reader.GetOrdinal("Amount")),
                Status = reader.GetString(reader.GetOrdinal("Status")),
                StripeSessionId = reader.IsDBNull(reader.GetOrdinal("StripeSessionId")) ? null : reader.GetString(reader.GetOrdinal("StripeSessionId")),
                StripePaymentIntentId = reader.IsDBNull(reader.GetOrdinal("StripePaymentIntentId")) ? null : reader.GetString(reader.GetOrdinal("StripePaymentIntentId")),
                CheckoutUrl = reader.IsDBNull(reader.GetOrdinal("CheckoutUrl")) ? null : reader.GetString(reader.GetOrdinal("CheckoutUrl")),
                CreatedAtUtc = reader.GetDateTime(reader.GetOrdinal("CreatedAtUtc")),
                PaidAtUtc = reader.IsDBNull(reader.GetOrdinal("PaidAtUtc")) ? null : reader.GetDateTime(reader.GetOrdinal("PaidAtUtc"))
            };
        }
    }
}
