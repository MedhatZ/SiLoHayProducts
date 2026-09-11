namespace SiLoHayProductsNew.Data
{
    public static class OrderStatuses
    {
        public const string PendingPayment = "PendingPayment";
        public const string DepositPaid = "DepositPaid";
        public const string FullyPaid = "FullyPaid";
        public const string PaymentFailed = "PaymentFailed";
    }

    public static class PaymentTypes
    {
        public const string Full = "Full";
        public const string Deposit = "Deposit";
        public const string Remaining = "Remaining";
    }

    public static class PaymentStatuses
    {
        public const string Pending = "Pending";
        public const string Paid = "Paid";
        public const string Failed = "Failed";
        public const string Expired = "Expired";
    }

    public class SiLoHayOrder
    {
        public long OrderId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal FullPrice { get; set; }
        public decimal DepositAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public string Status { get; set; } = OrderStatuses.PendingPayment;
        public bool WasAvailableNow { get; set; }
        public string? CustomerEmail { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
    }

    public class SiLoHayOrderPayment
    {
        public long PaymentId { get; set; }
        public long OrderId { get; set; }
        public string PaymentType { get; set; } = PaymentTypes.Full;
        public decimal Amount { get; set; }
        public string Status { get; set; } = PaymentStatuses.Pending;
        public string? StripeSessionId { get; set; }
        public string? StripePaymentIntentId { get; set; }
        public string? CheckoutUrl { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? PaidAtUtc { get; set; }
    }
}
