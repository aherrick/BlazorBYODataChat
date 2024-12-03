using Microsoft.AspNetCore.Components.WebAssembly.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace Client.Helpers;

public class CustomHttpClient(HttpClient httpClient, CustomSweetAlertService customSweetAlertService)
{
    public async Task<T> Get<T>(string url)
    {
        var response = await httpClient.GetAsync(url);

        return await HandleResponse<T>(response);
    }

    public async Task<T> Post<T>(string url, object value = null)
    {
        var response = await httpClient.PostAsJsonAsync(url, value);

        return await HandleResponse<T>(response);
    }

    public async IAsyncEnumerable<T> PostStream<T>(string url, HttpContent content)
    {
        HttpRequestMessage request = new(HttpMethod.Post, url)
        {
            Content = content
        };
        request.SetBrowserResponseStreamingEnabled(true);

        using HttpResponseMessage response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        if (!response.IsSuccessStatusCode)
        {
            await ShowExceptionAlert(response);

            yield return default;
        }
        else
        {
            // TODO: how can i wrap this in a try catch?
            var stream = await response.Content.ReadAsStreamAsync();

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            await foreach (var resp in JsonSerializer.DeserializeAsyncEnumerable<T>(stream, jsonOptions))
            {
                yield return resp;
            }
        }
    }

    private async Task<T> HandleResponse<T>(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            await ShowExceptionAlert(response);

            return default;
        }
        else
        {
            return await response.Content.ReadFromJsonAsync<T>();
        }
    }

    private async Task ShowExceptionAlert(HttpResponseMessage response)
    {
        var data = await response.Content.ReadAsStringAsync();

        await customSweetAlertService.Error(data);
    }
}