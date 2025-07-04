using System.Net.Http;

namespace PCL.Core.Service;

public class HttpClientService
{
    private static HttpClient _client = new();
    
    public static HttpClient GetClient() => _client;
}