using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranslationManager
{
  using System.Net.Http;
  using System.Net.Http.Headers;
  using System.Text.Json;

  public sealed class DeepLTranslator
  {
    private static readonly Uri ApiUri = new("https://api-free.deepl.com/v2/translate");
    private readonly HttpClient _http;
    private readonly string _apiKey;

    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
      PropertyNameCaseInsensitive = true
    };

    public DeepLTranslator(HttpClient httpClient, string apiKey)
    {
      _http = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
      _apiKey = string.IsNullOrWhiteSpace(apiKey) ? throw new ArgumentException("DeepL API key is required.", nameof(apiKey)) : apiKey;

      // DeepL expects Authorization: DeepL-Auth-Key <key>
      _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("DeepL-Auth-Key", _apiKey);
      _http.DefaultRequestHeaders.Accept.Clear();
      _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    // Translates `text` into `targetLang` (e.g., "EN", "DE"). Optional `sourceLang` (e.g., "DE").
    public async Task<string> TranslateAsync(string text, string targetLang, string? sourceLang = null, CancellationToken ct = default)
    {
      if (string.IsNullOrWhiteSpace(text))
        return string.Empty;
      if (string.IsNullOrWhiteSpace(targetLang))
        throw new ArgumentException("Target language is required.", nameof(targetLang));

      using var content = new FormUrlEncodedContent(new[]
      {
      new KeyValuePair<string, string>("text", text),
      new KeyValuePair<string, string>("target_lang", targetLang),
      // Optional: uncomment to force a source language
      // new KeyValuePair<string, string>("source_lang", sourceLang ?? string.Empty),
      new KeyValuePair<string, string>("preserve_formatting", "1")
    }.Where(kv => !string.IsNullOrEmpty(kv.Value)));

      using var response = await _http.PostAsync(ApiUri, content, ct).ConfigureAwait(false);
      var payload = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

      response.EnsureSuccessStatusCode();

      var model = JsonSerializer.Deserialize<DeepLResponse>(payload, JsonOptions);
      var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(payload);
      var translated = model?.Translations?.FirstOrDefault()?.Text;
      Console.WriteLine($"{text} : {translated}");

      Thread.Sleep (1000); // To avoid hitting rate limits
      return translated ?? text;
    }

    private sealed class DeepLResponse
    {
      public List<DeepLTranslation>? Translations { get; set; }
    }

    private sealed class DeepLTranslation
    {
      public string? Detected_Source_Language { get; set; }
      public string? Text { get; set; }
    }
  
  }
}
