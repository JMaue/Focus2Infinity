namespace Focus2Infinity.Services
{
  using Focus2Infinity.Data;
  using System.Globalization;
  using System.Text.Json;

  public class OverlayService
  {
    private readonly ImagePathResolver _pathResolver;

    private static readonly JsonSerializerOptions OverlayJsonOptions = new()
    {
      PropertyNameCaseInsensitive = true,
      WriteIndented = true
    };

    public OverlayService(ImagePathResolver pathResolver)
    {
      _pathResolver = pathResolver;
    }

    public async Task<bool> OverlayExists(string topic, string src)
    {
      bool rc = false;
      await Task.Run(() =>
      {
        rc = DoOverlayExists(topic, src) || DoOverlayJsonExists(topic, src);
      });
      return rc;
    }

    public string GetOverlayPathForImage(string topic, string src)
    {
      return _pathResolver.GetOverlayJsonPath(topic, src);
    }

    public async Task SaveOverlayData(string topic, string src, OverlayData data)
    {
      var path = _pathResolver.GetOverlayJsonPath(topic, src);
      var json = JsonSerializer.Serialize(data, OverlayJsonOptions);
      await File.WriteAllTextAsync(path, json);
    }

    public async Task<OverlayData?> GetOverlayData(string topic, string src)
    {
      OverlayData? rc = null;
      var ui = CultureInfo.CurrentUICulture;

      await Task.Run(() =>
      {
        rc = DoGetOverlayData(topic, src, ui);
      });

      return rc;
    }

    private bool DoOverlayExists(string topic, string src)
    {
      string filePath = _pathResolver.GetImagePath(topic, $"ovl_{src}");
      return File.Exists(filePath);
    }

    private bool DoOverlayJsonExists(string topic, string src)
    {
      var svgPath = _pathResolver.GetOverlayJsonPath(topic, src);
      if (File.Exists(svgPath)) return true;
      var legacyPath = _pathResolver.GetLegacyOverlayPath(topic, src);
      return File.Exists(legacyPath);
    }

    private OverlayData? DoGetOverlayData(string topic, string src, CultureInfo ui)
    {
      var baseName = Path.GetFileNameWithoutExtension(src);
      var candidates = new[]
      {
        _pathResolver.GetOverlayLocalizedJsonPath(topic, src, ui.TwoLetterISOLanguageName),
        _pathResolver.GetOverlayJsonPath(topic, src)
      };

      foreach (var candidate in candidates)
      {
        if (File.Exists(candidate))
        {
          try
          {
            string jsonString = File.ReadAllText(candidate);
            return JsonSerializer.Deserialize<OverlayData>(jsonString, OverlayJsonOptions);
          }
          catch (Exception)
          {
            return null;
          }
        }
      }

      return null;
    }
  }
}
