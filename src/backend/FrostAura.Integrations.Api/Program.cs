using FrostAura.Integrations.Api;
using FrostAura.Integrations.Core;
using FrostAura.Integrations.Woolworths;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<CredentialAccessor>();
builder.Services.AddProblemDetails();

// Integrations. Each one registers itself as IIntegration so discovery and
// credential resolution pick it up without a central list to keep in sync.
builder.Services.AddSingleton<WoolworthsIntegration>();
builder.Services.AddSingleton<IIntegration>(sp => sp.GetRequiredService<WoolworthsIntegration>());
builder.Services.AddHttpClient<WoolworthsSearchClient>();
builder.Services.AddSingleton<WoolworthsCartService>();

// MCP over streamable HTTP. Stateless because every tool call carries its own
// credentials and finishes in one round trip — there is no server-to-client
// sampling or elicitation to keep a session open for.
builder.Services
    .AddMcpServer(options => options.ServerInfo = new() { Name = "frostaura-integrations", Version = "0.1.0" })
    .WithHttpTransport(options => options.Stateless = true)
    .WithTools<WoolworthsTools>();

var app = builder.Build();

// Translate domain failures into honest status codes. Without this a missing
// header is an opaque 500 and the caller has no idea what to fix.
app.UseExceptionHandler(handler => handler.Run(async context =>
{
    var error = context.Features
        .Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;

    var (status, title) = error switch
    {
        MissingCredentialsException => (StatusCodes.Status401Unauthorized, "Missing credentials"),
        ProviderAuthenticationException => (StatusCodes.Status401Unauthorized, "Provider rejected the credentials"),
        TaskCanceledException or OperationCanceledException => (StatusCodes.Status504GatewayTimeout, "Upstream timed out"),
        HttpRequestException => (StatusCodes.Status502BadGateway, "Upstream request failed"),
        _ => (StatusCodes.Status500InternalServerError, "Unexpected error")
    };

    context.Response.StatusCode = status;
    await context.Response.WriteAsJsonAsync(new
    {
        error = title,
        // Safe to surface: these messages describe the caller's own request,
        // never provider internals.
        detail = error is MissingCredentialsException or ProviderAuthenticationException
            ? error.Message
            : "See server logs for detail."
    });
}));

app.MapGet("/", () => Results.Redirect("/api/integrations"));

app.MapGet("/api/health", () => Results.Ok(new { status = "healthy" }))
   .WithName("Health");

// Discovery: what can this service drive, and what must a caller supply?
// Both humans and MCP clients start here.
app.MapGet("/api/integrations", (IEnumerable<IIntegration> integrations) => Results.Ok(new
{
    integrations = integrations.Select(i => new
    {
        i.Key,
        i.DisplayName,
        i.Description,
        credentials = i.Credentials.Select(c => new
        {
            c.Key,
            header = c.Header,
            c.Description,
            c.Required,
            c.Secret
        })
    })
}));

var woolworths = app.MapGroup("/api/woolworths");

// Anonymous: search needs no account.
woolworths.MapGet("/search", async (
    string q, int? limit, WoolworthsSearchClient search, CancellationToken ct) =>
{
    var products = await search.SearchAsync(q, Math.Clamp(limit ?? 5, 1, 24), ct);
    return Results.Ok(new { query = q, count = products.Count, products });
});

// Anonymous: resolve a list without touching a cart.
woolworths.MapPost("/shopping-list/preview", async (
    ShoppingListRequest request, WoolworthsCartService cart, CancellationToken ct) =>
{
    var result = await cart.PreviewAsync(request.Text, ct);
    return Results.Ok(result);
});

// Authenticated by the caller's own Woolworths credentials, via headers.
woolworths.MapPost("/shopping-list/cart", async (
    ShoppingListRequest request,
    WoolworthsCartService cart,
    CredentialAccessor accessor,
    WoolworthsIntegration integration,
    CancellationToken ct) =>
{
    var credentials = accessor.Resolve(integration);
    var result = await cart.AddShoppingListAsync(request.Text, credentials, ct);
    return Results.Ok(result);
});

woolworths.MapPost("/cart/items", async (
    AddProductRequest request,
    WoolworthsCartService cart,
    CredentialAccessor accessor,
    WoolworthsIntegration integration,
    CancellationToken ct) =>
{
    var credentials = accessor.Resolve(integration);
    var outcome = await cart.AddProductAsync(request.ProductId, request.Quantity ?? 1, credentials, ct);
    return Results.Ok(outcome);
});

app.MapMcp("/mcp");

app.Run();

/// <param name="Text">Raw shared shopping-list text.</param>
public sealed record ShoppingListRequest(string Text);

/// <param name="ProductId">A Woolworths product id from a search result.</param>
public sealed record AddProductRequest(string ProductId, int? Quantity);

/// <summary>Exposed so the integration tests can spin the host up in-memory.</summary>
public partial class Program;
