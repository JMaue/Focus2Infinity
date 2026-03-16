using System.Globalization;

namespace Focus2Infinity.Services
{
  public interface ILegalContentService
  {
    Task<string> GetLegalNotice();
    Task<string> GetPrivacyPolicy();
    Task<string> GetAboutMeContent();
  }

  public class LegalContentService : ILegalContentService
  {
    private readonly ILogger<LegalContentService> _logger;
    private readonly string _contentPath;

    public LegalContentService(ILogger<LegalContentService> logger)
    {
      _logger = logger;
      _contentPath = Path.Combine(AppContext.BaseDirectory, "LegalContent");
    }

    public async Task<string> GetLegalNotice()
    {
      return await ReadContentFile("impressum");
    }

    public async Task<string> GetPrivacyPolicy()
    {
      return await ReadContentFile("datenschutz");
    }

    public async Task<string> GetAboutMeContent()
    {
      return await ReadContentFile("aboutMe");
    }

    private async Task<string> ReadContentFile(string filename)
    {
      try
      { 
        var filePath = GetCandidate(filename);

        if (!File.Exists(filePath))
        {
          _logger.LogWarning("Legal content file not found: {FilePath}", filePath);
          return $"<p class='text-danger'>Content file '{filename}' not found. Please contact the administrator.</p>";
        }

        var content = await File.ReadAllTextAsync(filePath);
        _logger.LogInformation("Loaded legal content: {FileName} ({Length} characters)", filename, content.Length);
        
        return content;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error reading legal content file: {FileName}", filename);
        return $"<p class='text-danger'>Error loading content. Please contact the administrator.</p>";
      }

      string GetCandidate(string filename)
      {
        var ui = CultureInfo.CurrentUICulture;
        var baseWithoutHtml = Path.Combine(_contentPath, filename);

        // Try: exact culture -> language -> neutral
        var candidates = new[]
        {
            $"{baseWithoutHtml}.{ui.Name}.html",                     // ?.de-DE.html
            $"{baseWithoutHtml}.{ui.TwoLetterISOLanguageName}.html", // ?.de.html
            $"{baseWithoutHtml}.en.html"                             // ?.en.html
          };

        foreach (var candidate in candidates)
        {
          if (File.Exists(candidate))
            return candidate;
        }
        return $"{baseWithoutHtml}.en.html"; // Fallback to English
      }
    }
  }
}
