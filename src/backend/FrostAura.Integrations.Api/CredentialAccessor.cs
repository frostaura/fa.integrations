using FrostAura.Integrations.Core;

namespace FrostAura.Integrations.Api;

/// <summary>
/// Resolves an integration's declared credentials from the inbound request
/// headers.
///
/// REST and MCP share this deliberately: an MCP client configures exactly the
/// same headers a curl caller would send, so there is one auth story to
/// document per integration rather than two.
/// </summary>
public sealed class CredentialAccessor(IHttpContextAccessor accessor)
{
    /// <summary>
    /// Collect every credential the integration declared.
    /// </summary>
    /// <exception cref="MissingCredentialsException">
    /// Thrown when a required field is absent. The message names the exact
    /// headers to set, because a caller wiring up an MCP client cannot see
    /// this code.
    /// </exception>
    public IntegrationCredentials Resolve(IIntegration integration)
    {
        var headers = accessor.HttpContext?.Request.Headers
            ?? throw new MissingCredentialsException("No active HTTP request.");

        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var missing = new List<string>();

        foreach (var field in integration.Credentials)
        {
            var value = headers[field.Header].ToString();

            if (string.IsNullOrWhiteSpace(value))
            {
                if (field.Required) missing.Add(field.Header);
                continue;
            }

            values[field.Key] = value;
        }

        if (missing.Count > 0)
        {
            throw new MissingCredentialsException(
                $"{integration.DisplayName} requires the following request header(s): {string.Join(", ", missing)}.");
        }

        return new IntegrationCredentials(values);
    }
}
