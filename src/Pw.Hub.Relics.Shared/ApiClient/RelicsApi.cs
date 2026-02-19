using IdentityModel.Client;
using RestSharp;

namespace Pw.Hub.Relics.Shared.ApiClient;

/// <summary>
/// API клиент для работы с реликвиями через RestSharp.
/// </summary>
public class RelicsApi : IDisposable
{
    private readonly RestClient _client;
    private readonly ClientCredentialsOptions _options;
    private string? _accessToken;
    private DateTime _tokenExpiresAt;
    private readonly HttpClient _authHttpClient;

    public RelicsApi(string baseUrl, ClientCredentialsOptions options)
    {
        _options = options;
        
        var restOptions = new RestClientOptions(baseUrl);
        _client = new RestClient(restOptions);
        _authHttpClient = new HttpClient();
    }

    /// <summary>
    /// Парсинг бинарных данных лотов реликвий.
    /// </summary>
    /// <param name="server">Название сервера.</param>
    /// <param name="data">Бинарные данные пакета.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    public async Task<RestResponse<ParseRelicResult>> ParseRelicsAsync(string server, byte[] data, CancellationToken cancellationToken = default)
    {
        var token = await GetAccessTokenAsync(cancellationToken);
        
        var request = new RestRequest("api/relics/parse", Method.Post);
        request.AddHeader("Authorization", $"Bearer {token}");
        request.AddJsonBody(new ParseRelicRequest(server, data));

        return await _client.ExecuteAsync<ParseRelicResult>(request, cancellationToken);
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiresAt)
        {
            return _accessToken;
        }

        var disco = await _authHttpClient.GetDiscoveryDocumentAsync(_options.Authority, cancellationToken);
        if (disco.IsError) throw new Exception($"Discovery error: {disco.Error}");

        var tokenResponse = await _authHttpClient.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
        {
            Address = disco.TokenEndpoint,
            ClientId = _options.ClientId,
            ClientSecret = _options.ClientSecret,
            Scope = _options.Scope
        }, cancellationToken);

        if (tokenResponse.IsError) throw new Exception($"Token error: {tokenResponse.Error}");

        _accessToken = tokenResponse.AccessToken;
        _tokenExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn - 30);

        return _accessToken!;
    }

    public void Dispose()
    {
        _client.Dispose();
        _authHttpClient.Dispose();
    }
}