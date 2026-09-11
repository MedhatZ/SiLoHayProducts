using SiLoHayProductsNew.Data;
using SiLoHayProductsNew.Options;
using SiLoHayProductsNew.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddMvc();
builder.Services.AddAntiforgery(o => o.HeaderName = "XSRF-TOKEN");
builder.Services.AddHttpClient();

builder.Services.Configure<StripeOptions>(builder.Configuration.GetSection(StripeOptions.SectionName));
builder.Services.AddSingleton<OrderRepository>();
builder.Services.AddScoped<StripeCheckoutService>();
builder.Services.AddScoped<CheckoutOrchestrator>();
builder.Services.AddScoped<StripeWebhookProcessor>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

// Reliable webhook endpoint (raw body) — preferred over Razor Page for Stripe CLI / Dashboard.
app.MapPost("/api/stripe/webhook", async (HttpRequest request, StripeWebhookProcessor processor, ILoggerFactory loggerFactory) =>
{
    var logger = loggerFactory.CreateLogger("StripeWebhookApi");
    string json;
    using (var reader = new StreamReader(request.Body))
    {
        json = await reader.ReadToEndAsync();
    }

    var signature = request.Headers["Stripe-Signature"].ToString();
    if (string.IsNullOrWhiteSpace(signature))
    {
        return Results.BadRequest("Missing Stripe-Signature");
    }

    try
    {
        await processor.ProcessAsync(json, signature);
        return Results.Ok();
    }
    catch (Stripe.StripeException ex)
    {
        logger.LogWarning(ex, "Stripe webhook rejected");
        return Results.BadRequest();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Stripe webhook failed");
        return Results.StatusCode(500);
    }
}).DisableAntiforgery();

app.Run();
