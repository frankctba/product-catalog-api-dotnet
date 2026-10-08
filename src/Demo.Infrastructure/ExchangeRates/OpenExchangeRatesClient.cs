using System.Net.Http.Headers;
using System.Net.Http.Json;
using Demo.Application.Common.Services;
using Demo.Domain.Modules.ExchangeRates;
using Microsoft.Extensions.Options;

namespace Demo.Infrastructure.ExchangeRates;

/// <summary>
/// Client for https://docs.openexchangerates.org. The free plan only supports USD as the base currency.
/// </summary>
public class OpenExchangeRatesClient : IExchangeRateProvider
{
    public const string ProviderName = "OpenExchangeRates";

    private const string DefaultBaseCurrency = "USD";

    private readonly HttpClient _httpClient;
    private readonly OpenExchangeRatesOptions _options;

    public OpenExchangeRatesClient(HttpClient httpClient, IOptions<OpenExchangeRatesOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<ExchangeRateSnapshot> GetLatestRatesAsync(string baseCurrency, IReadOnlyCollection<string> currencies, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.AppId))
        {
            throw new InvalidOperationException(
                $"{OpenExchangeRatesOptions.SectionName}:AppId is not configured. Set it with user-secrets or an environment variable.");
        }

        var url = $"latest.json?symbols={string.Join(',', currencies)}";

        // USD is the provider's default base; other bases require a paid plan.
        if (!string.Equals(baseCurrency, DefaultBaseCurrency, StringComparison.OrdinalIgnoreCase))
        {
            url += $"&base={baseCurrency}";
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, url);

        // Sent as a header, not as the app_id query parameter, so the key never appears in logged URLs.
        request.Headers.Authorization = new AuthenticationHeaderValue("Token", _options.AppId);

        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await TryReadError(response, cancellationToken);
            throw new HttpRequestException(
                $"Open Exchange Rates returned {(int)response.StatusCode}: {error?.Message} {error?.Description}".TrimEnd(),
                inner: null,
                response.StatusCode);
        }

        var body = await response.Content.ReadFromJsonAsync<LatestRatesResponse>(cancellationToken)
            ?? throw new InvalidOperationException("Open Exchange Rates returned an empty response.");

        return new ExchangeRateSnapshot
        {
            Provider = ProviderName,
            BaseCurrency = body.Base.ToUpperInvariant(),
            RateTimestampUtc = DateTimeOffset.FromUnixTimeSeconds(body.Timestamp).UtcDateTime,
            FetchedAtUtc = DateTime.UtcNow,
            Rates = body.Rates
                .Select(r => new ExchangeRate { QuoteCurrency = r.Key.ToUpperInvariant(), Rate = r.Value })
                .ToList()
        };
    }

    private static async Task<ErrorResponse?> TryReadError(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            return await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken);
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }
    }

    private record LatestRatesResponse(long Timestamp, string Base, Dictionary<string, decimal> Rates);

    private record ErrorResponse(string? Message, string? Description);
}
