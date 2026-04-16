namespace Focus2Infinity.Services
{
  using System.Globalization;

  public class ImagePathResolver
  {
    private readonly string _imageRoot;

    public ImagePathResolver(IWebHostEnvironment env)
    {
      _imageRoot = Path.Combine(env.WebRootPath, "img");
    }

    public string GetTopicDirectory(string topic) => Path.Combine(_imageRoot, topic);

    public string GetImagePath(string topic, string filename) => Path.Combine(_imageRoot, topic, filename);

    public string GetOverlayJsonPath(string topic, string src)
    {
      var baseName = Path.GetFileNameWithoutExtension(src);
      return Path.Combine(_imageRoot, topic, $"svg_{baseName}.overlay.json");
    }

    public string GetOverlayLocalizedJsonPath(string topic, string src, string lang)
    {
      var baseName = Path.GetFileNameWithoutExtension(src);
      return Path.Combine(_imageRoot, topic, $"svg_{baseName}.overlay.{lang}.json");
    }

    public string GetLegacyOverlayPath(string topic, string src)
    {
      var baseName = Path.GetFileNameWithoutExtension(src);
      return Path.Combine(_imageRoot, topic, $"{baseName}.overlay.json");
    }

    public string GetCommentsPath(string topic, string src) => Path.Combine(_imageRoot, topic, $"{src}.comments.json");

    public string GetDeniedCommentsPath(string topic, string src) => Path.Combine(_imageRoot, topic, $"{src}.denied.json");

    public string[] GetStoryJsonCandidates(string topic, string src, CultureInfo ui)
    {
      var baseFile = Path.Combine(_imageRoot, topic, src);
      return new[]
      {
        $"{baseFile}.{ui.Name}.json",
        $"{baseFile}.{ui.TwoLetterISOLanguageName}.json",
        $"{baseFile}.json"
      };
    }
  }
}
