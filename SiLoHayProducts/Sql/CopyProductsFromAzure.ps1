$ErrorActionPreference = "Stop"

function DbVal($v) {
    if ($null -eq $v -or $v -is [DBNull]) { return [DBNull]::Value }
    return $v
}

$azure = "Server=silohaycom.database.windows.net;Database=SiLoHay;User Id=silohayreader;Password=GasolinaSiLoHay2026;Encrypt=True;TrustServerCertificate=False;"
$local = "Server=DESKTOP-K9P6I3C\SQLEXPRESS01;Database=SiLoHay;Integrated Security=True;TrustServerCertificate=True;"

$src = New-Object System.Data.SqlClient.SqlConnection $azure
$dst = New-Object System.Data.SqlClient.SqlConnection $local
$src.Open()
$dst.Open()

$clear = $dst.CreateCommand()
$clear.CommandText = @"
IF OBJECT_ID('dbo.SiLoHayOrderPayment') IS NOT NULL DELETE FROM dbo.SiLoHayOrderPayment;
IF OBJECT_ID('dbo.SiLoHayOrder') IS NOT NULL DELETE FROM dbo.SiLoHayOrder;
IF OBJECT_ID('dbo.SiLoHayStripeEvent') IS NOT NULL DELETE FROM dbo.SiLoHayStripeEvent;
DELETE FROM dbo.SiLoHayProduct;
"@
$clear.ExecuteNonQuery() | Out-Null

$cmd = $src.CreateCommand()
$cmd.CommandText = @"
SELECT SiLoHayProductId, ASIN, ProductName, IsAvailableNow, ImageUrl, ImageUrl2, PriceNew, AmazonPriceUsed,
       Prepayment, OurPrice, IsAvailable, AmazonLink, LinkCustomerCompare, StoreUsedForComparison,
       ShowComparisonMsg, InsertDate, Notes
FROM dbo.SiLoHayProduct
ORDER BY SiLoHayProductId
"@

$adapter = New-Object System.Data.SqlClient.SqlDataAdapter $cmd
$dt = New-Object System.Data.DataTable
[void]$adapter.Fill($dt)
Write-Host "Loaded $($dt.Rows.Count) products from Azure"

$identityOn = $dst.CreateCommand()
$identityOn.CommandText = "SET IDENTITY_INSERT dbo.SiLoHayProduct ON;"
$identityOn.ExecuteNonQuery() | Out-Null

$inserted = 0
foreach ($row in $dt.Rows) {
    $asin = ""
    if ($row.ASIN -isnot [DBNull] -and $null -ne $row.ASIN) {
        $asin = $row.ASIN.ToString()
        if ($asin.Length -lt 10) { $asin = $asin.PadRight(10) }
        if ($asin.Length -gt 10) { $asin = $asin.Substring(0, 10) }
    }

    $ins = $dst.CreateCommand()
    $ins.CommandText = @"
INSERT INTO dbo.SiLoHayProduct
(SiLoHayProductId, ASIN, ProductName, IsAvailableNow, ImageUrl, ImageUrl2, PriceNew, AmazonPriceUsed,
 Prepayment, OurPrice, IsAvailable, AmazonLink, LinkCustomerCompare, StoreUsedForComparison,
 ShowComparisonMsg, InsertDate, Notes)
VALUES
(@Id, @ASIN, @ProductName, @IsAvailableNow, @ImageUrl, @ImageUrl2, @PriceNew, @AmazonPriceUsed,
 @Prepayment, @OurPrice, @IsAvailable, @AmazonLink, @LinkCustomerCompare, @StoreUsedForComparison,
 @ShowComparisonMsg, @InsertDate, @Notes)
"@
    [void]$ins.Parameters.AddWithValue("@Id", $row.SiLoHayProductId)
    [void]$ins.Parameters.AddWithValue("@ASIN", $asin)
    [void]$ins.Parameters.AddWithValue("@ProductName", $row.ProductName)
    [void]$ins.Parameters.AddWithValue("@IsAvailableNow", $row.IsAvailableNow)
    [void]$ins.Parameters.AddWithValue("@ImageUrl", $row.ImageUrl)
    [void]$ins.Parameters.AddWithValue("@ImageUrl2", (DbVal $row.ImageUrl2))
    [void]$ins.Parameters.AddWithValue("@PriceNew", $row.PriceNew)
    [void]$ins.Parameters.AddWithValue("@AmazonPriceUsed", (DbVal $row.AmazonPriceUsed))
    [void]$ins.Parameters.AddWithValue("@Prepayment", (DbVal $row.Prepayment))
    [void]$ins.Parameters.AddWithValue("@OurPrice", $row.OurPrice)
    [void]$ins.Parameters.AddWithValue("@IsAvailable", $row.IsAvailable)
    [void]$ins.Parameters.AddWithValue("@AmazonLink", (DbVal $row.AmazonLink))
    [void]$ins.Parameters.AddWithValue("@LinkCustomerCompare", (DbVal $row.LinkCustomerCompare))
    [void]$ins.Parameters.AddWithValue("@StoreUsedForComparison", (DbVal $row.StoreUsedForComparison))
    [void]$ins.Parameters.AddWithValue("@ShowComparisonMsg", (DbVal $row.ShowComparisonMsg))
    [void]$ins.Parameters.AddWithValue("@InsertDate", (DbVal $row.InsertDate))
    [void]$ins.Parameters.AddWithValue("@Notes", (DbVal $row.Notes))
    [void]$ins.ExecuteNonQuery()
    $inserted++
}

$identityOff = $dst.CreateCommand()
$identityOff.CommandText = "SET IDENTITY_INSERT dbo.SiLoHayProduct OFF;"
$identityOff.ExecuteNonQuery() | Out-Null

# Reseed identity to max id
$reseed = $dst.CreateCommand()
$reseed.CommandText = "DECLARE @m INT = (SELECT ISNULL(MAX(SiLoHayProductId),0) FROM dbo.SiLoHayProduct); DBCC CHECKIDENT ('dbo.SiLoHayProduct', RESEED, @m);"
$reseed.ExecuteNonQuery() | Out-Null

$countCmd = $dst.CreateCommand()
$countCmd.CommandText = "SELECT COUNT(*) FROM dbo.SiLoHayProduct"
$localCount = [int]$countCmd.ExecuteScalar()

$spCmd = $dst.CreateCommand()
$spCmd.CommandText = "EXEC usp_TienditaGetItems"
$spReader = $spCmd.ExecuteReader()
$spCount = 0
while ($spReader.Read()) { $spCount++ }
$spReader.Close()

Write-Host "Inserted: $inserted"
Write-Host "Local product count: $localCount"
Write-Host "SP row count: $spCount"

$src.Close()
$dst.Close()
