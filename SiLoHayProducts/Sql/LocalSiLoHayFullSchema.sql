-- Full local SiLoHay schema for DESKTOP-K9P6I3C\SQLEXPRESS01
USE SiLoHay;
GO

IF OBJECT_ID(N'dbo.SiLoHayProduct', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SiLoHayProduct
    (
        SiLoHayProductId       INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ASIN                   NCHAR(10) NOT NULL,
        ProductName            VARCHAR(100) NOT NULL,
        IsAvailableNow         BIT NOT NULL CONSTRAINT DF_SiLoHayProduct_IsAvailableNow DEFAULT (0),
        ImageUrl               VARCHAR(1000) NOT NULL,
        ImageUrl2              VARCHAR(1000) NULL,
        PriceNew               DECIMAL(18,2) NOT NULL,
        AmazonPriceUsed        DECIMAL(18,2) NULL,
        Prepayment             DECIMAL(18,2) NULL,
        OurPrice               DECIMAL(18,2) NOT NULL,
        IsAvailable            BIT NOT NULL CONSTRAINT DF_SiLoHayProduct_IsAvailable DEFAULT (1),
        AmazonLink             VARCHAR(1000) NULL,
        LinkCustomerCompare    VARCHAR(1000) NULL,
        StoreUsedForComparison VARCHAR(30) NULL,
        ShowComparisonMsg      BIT NULL,
        InsertDate             DATETIME NULL CONSTRAINT DF_SiLoHayProduct_InsertDate DEFAULT (GETDATE()),
        Notes                  VARCHAR(500) NULL
    );
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_TienditaGetItems
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        ASIN,
        ProductName,
        ImageUrl,
        ImageUrl2,
        PriceNew,
        OurPrice,
        IsAvailable,
        LinkCustomerCompare AS linkCustomerCompare,
        SiLoHayProductId,
        IsAvailableNow,
        ISNULL(Prepayment, 0) AS Prepayment,
        StoreUsedForComparison,
        ISNULL(ShowComparisonMsg, 0) AS ShowComparisonMsg
    FROM dbo.SiLoHayProduct
    WHERE IsAvailable = 1
    ORDER BY SiLoHayProductId DESC;
END
GO

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
        PaymentType            NVARCHAR(50) NOT NULL,
        Amount                 DECIMAL(18,2) NOT NULL,
        Status                 NVARCHAR(50) NOT NULL,
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
