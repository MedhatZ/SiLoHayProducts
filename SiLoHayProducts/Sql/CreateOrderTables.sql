-- Run against SiLoHay Azure SQL (requires write permissions).
-- Creates order/payment tracking for Stripe Checkout.

IF OBJECT_ID(N'dbo.SiLoHayOrder', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SiLoHayOrder
    (
        OrderId            BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ProductId          INT NOT NULL,
        ProductName        NVARCHAR(500) NOT NULL,
        FullPrice          DECIMAL(18,2) NOT NULL,
        DepositAmount      DECIMAL(18,2) NOT NULL CONSTRAINT DF_SiLoHayOrder_Deposit DEFAULT (0),
        RemainingAmount    DECIMAL(18,2) NOT NULL CONSTRAINT DF_SiLoHayOrder_Remaining DEFAULT (0),
        Status             NVARCHAR(50) NOT NULL,
        WasAvailableNow    BIT NOT NULL,
        CustomerEmail      NVARCHAR(256) NULL,
        CustomerName       NVARCHAR(256) NULL,
        CustomerPhone      NVARCHAR(50) NULL,
        CreatedAtUtc       DATETIME2 NOT NULL CONSTRAINT DF_SiLoHayOrder_Created DEFAULT (SYSUTCDATETIME()),
        UpdatedAtUtc       DATETIME2 NOT NULL CONSTRAINT DF_SiLoHayOrder_Updated DEFAULT (SYSUTCDATETIME())
    );
END
GO

IF OBJECT_ID(N'dbo.SiLoHayOrderPayment', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SiLoHayOrderPayment
    (
        PaymentId              BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        OrderId                BIGINT NOT NULL,
        PaymentType            NVARCHAR(50) NOT NULL, -- Full | Deposit | Remaining
        Amount                 DECIMAL(18,2) NOT NULL,
        Status                 NVARCHAR(50) NOT NULL, -- Pending | Paid | Failed | Expired
        StripeSessionId        NVARCHAR(200) NULL,
        StripePaymentIntentId  NVARCHAR(200) NULL,
        CheckoutUrl            NVARCHAR(1000) NULL,
        CreatedAtUtc           DATETIME2 NOT NULL CONSTRAINT DF_SiLoHayOrderPayment_Created DEFAULT (SYSUTCDATETIME()),
        PaidAtUtc              DATETIME2 NULL,
        CONSTRAINT FK_SiLoHayOrderPayment_Order FOREIGN KEY (OrderId) REFERENCES dbo.SiLoHayOrder(OrderId)
    );

    CREATE INDEX IX_SiLoHayOrderPayment_StripeSessionId ON dbo.SiLoHayOrderPayment(StripeSessionId);
    CREATE INDEX IX_SiLoHayOrderPayment_OrderId ON dbo.SiLoHayOrderPayment(OrderId);
END
GO

IF OBJECT_ID(N'dbo.SiLoHayStripeEvent', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SiLoHayStripeEvent
    (
        EventId        NVARCHAR(200) NOT NULL PRIMARY KEY,
        EventType      NVARCHAR(200) NOT NULL,
        ProcessedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_SiLoHayStripeEvent_Processed DEFAULT (SYSUTCDATETIME())
    );
END
GO
