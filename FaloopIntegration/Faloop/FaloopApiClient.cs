﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Divination.FaloopIntegration.Faloop.Model;

namespace Divination.FaloopIntegration.Faloop;

public class FaloopApiClient : IDisposable
{
    private readonly HttpClient client = new();

    public async Task<UserRefreshResponse?> RefreshAsync()
    {
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri("https://faloop.app/api/auth/user/refresh"),
            Content = JsonContent.Create(new Dictionary<string, string?>
                {
                    {"sessionId", null},
                },
                MediaTypeHeaderValue.Parse("application/json")),
            Headers =
            {
                {"Accept", "application/json, text/plain, */*"},
                {"Accept-Language", "ja"},
                {"Origin", "https://faloop.app"},
                {"Referer", "https://faloop.app/"},
                {"Sec-Ch-Ua", "\"Not/A)Brand\";v=\"8\", \"Chromium\";v=\"126\", \"Microsoft Edge\";v=\"126\""},
                {"Sec-Ch-Ua-Mobile", "?0"},
                {"Sec-Ch-Ua-Platform", "\"Windows\""},
                {"Sec-Fetch-Dest", "empty"},
                {"Sec-Fetch-Mode", "cors"},
                {"Sec-Fetch-Site", "same-origin"},
                {
                    "User-Agent",
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36 Edg/126.0.0.0"
                },
            },
        };

        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<UserRefreshResponse>();
    }

    public async Task<UserLoginResponse?> LoginAsync(string username, string password, string sessionId, string token)
    {
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri("https://faloop.app/api/auth/user/login"),
            Content = JsonContent.Create(new Dictionary<string, object>
                {
                    {"username", username},
                    {"password", password},
                    {"rememberMe", false},
                    {"sessionId", sessionId},
                },
                MediaTypeHeaderValue.Parse("application/json")),
            Headers =
            {
                {"Accept", "application/json, text/plain, */*"},
                {"Accept-Language", "ja"},
                {"Authorization", token}, // JWT eyJ...
                {"Origin", "https://faloop.app"},
                {"Referer", "https://faloop.app/login"},
                {"Sec-Ch-Ua", "\"Not/A)Brand\";v=\"8\", \"Chromium\";v=\"126\", \"Microsoft Edge\";v=\"126\""},
                {"Sec-Ch-Ua-Mobile", "?0"},
                {"Sec-Ch-Ua-Platform", "\"Windows\""},
                {"Sec-Fetch-Dest", "empty"},
                {"Sec-Fetch-Mode", "cors"},
                {"Sec-Fetch-Site", "same-origin"},
                {
                    "User-Agent",
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36 Edg/126.0.0.0"
                },
            },
        };

        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<UserLoginResponse>();
    }

    public async Task<DcDataResponse?> GetDCDataAsync(string token, string dataCenter)
    {
         var request = new HttpRequestMessage
         {
             Method = HttpMethod.Get,
             RequestUri = new Uri($"https://faloop.app/api/app/datacenter/{dataCenter}"),
             Headers =
             {
                 {"Accept", "application/json, text/plain, */*"},
                 {"Accept-Language", "ja"},
                 {"Authorization", token}, // JWT eyJ...
                 {"Referer", "https://faloop.app/s"},
                 {"Sec-Ch-Ua", "\"Not/A)Brand\";v=\"8\", \"Chromium\";v=\"126\", \"Microsoft Edge\";v=\"126\""},
                 {"Sec-Ch-Ua-Mobile", "?0"},
                 {"Sec-Ch-Ua-Platform", "\"Windows\""},
                 {"Sec-Fetch-Dest", "empty"},
                 {"Sec-Fetch-Mode", "cors"},
                 {"Sec-Fetch-Site", "same-origin"},
                 {
                     "User-Agent",
                     "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36 Edg/126.0.0.0"
                 },
             },
         };

         using var response = await client.SendAsync(request);
         response.EnsureSuccessStatusCode();

         return await response.Content.ReadFromJsonAsync<DcDataResponse>();
    }

    public void Dispose()
    {
        client.Dispose();
    }
}
