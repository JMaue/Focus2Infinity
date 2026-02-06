namespace Focus2Infinity.Data
{
  using Microsoft.Extensions.Localization;
  using System.Collections.Generic;
  using System.Drawing;
  using System.Globalization;
  using System.Text.Json;
  using System.Text.RegularExpressions;

  public class F2IDataService
  {
    private readonly IWebHostEnvironment _hostingEnvironment;

    public F2IDataService(IWebHostEnvironment hostingEnvironment)
    {
      _hostingEnvironment = hostingEnvironment;
    }

    public async Task<List<string>> GetMainTopics()
    {
      var rc = new List<string>();
      await Task.Run(() => { rc.AddRange(new string[] { "Galaxies", "Nebulae", "Clusters", "Planets", "Eclipses", "Milkyway", "Moon", "Sun", "Sunsets", "Comets", "Landscapes", "StarTrails", "Others" }); });
      return rc;
    }

    public async Task<List<string>> GetSubTopicsSorted(string mainTopic, IStringLocalizer L)
    {
      var rc = new List<string>();
      await Task.Run(() =>
      {
        var files = GetSubTopics(mainTopic);
        var list = AnnotateWithDate(files, mainTopic, L);
        rc = ToSortedList(list);
      });
      return rc;
    }

    private List<string> ToSortedList(List<Tuple<DateTime, string>> list)
    {
      var rc = new List<string>();
      foreach (var kvp in list.OrderByDescending(kvp => kvp.Item1))
      {
        rc.Add(kvp.Item2);
      }
      return rc;
    }

    private List<Tuple<DateTime, string>> AnnotateWithDate(IEnumerable<FileInfo> files, string mainTopic, IStringLocalizer L)
    {
      var list = new List<Tuple<DateTime, string>>();
      foreach (var f in files)
      {
        var src = GetStoryText(mainTopic, f.Name).Result;
        if (src != null)
        {
          list.Add(new Tuple<DateTime, string>(src.GetDateTaken(L), f.Name));
        }
        else
        {
          list.Add(new Tuple<DateTime, string>(DateTime.MinValue, f.Name));
        }
      }
      return list;
    }

    private IEnumerable<FileInfo> GetSubTopics(string mainTopic)
    {
      string htmlFilePath = Path.Combine(_hostingEnvironment.WebRootPath, "img", mainTopic);
      if (!Directory.Exists(htmlFilePath))
        return new List<FileInfo>();

      var dirInfo = new DirectoryInfo(htmlFilePath);

      var rc = dirInfo.EnumerateFiles("*.*")
                               .Where(file => (file.Extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
                                               file.Extension.Equals(".png", StringComparison.OrdinalIgnoreCase) ||
                                               file.Extension.Equals(".tif", StringComparison.OrdinalIgnoreCase))
                                               && !file.Name.StartsWith("tbn_")
                                               && !file.Name.StartsWith("ovl_"));
      return rc;
    }

    public async Task<List<ImageItem>> GetAllTopics(IStringLocalizer L)
    {
      List<ImageItem> rc = new List<ImageItem>();

      await Task.Run(() =>
      {
        var allTopics = new List<Tuple<DateTime, string, string>>();
        foreach (var topic in GetMainTopics().Result)
        {
          var files = GetSubTopics(topic);
          var list = AnnotateWithDate(files, topic, L);
          foreach (var item in list)
          {
            allTopics.Add(new Tuple<DateTime, string, string>(item.Item1, item.Item2, topic));
          }
        }

        foreach (var kvp in allTopics.OrderByDescending(kvp => kvp.Item1))
        {
          rc.Add(new ImageItem(kvp.Item2, kvp.Item3));
        }
      });


      return rc;
    }

    public async Task<ImageStory> GetStoryText(string topic, string src)
    {
      var ui = CultureInfo.CurrentUICulture; // capture once
      ImageStory? rc = null;
      await Task.Run(() =>
      {
        rc = DoGetStoryText(topic, src, ui);
      });

      return rc ?? new ImageStory("{}");
    }

    public async Task<(int width, int height)> GetImageFormat(string topic, string src)
    {
      int width = 0;
      int height = 0;
      await Task.Run(() =>
      {
        (width, height) = DoGetImageFormat(topic, src);
      });

      return (width, height);
    }

    public (int width, int height) DoGetImageFormat(string topic, string src)
    {
      var root = _hostingEnvironment.WebRootPath;
      string imgFilePath = $"{Path.Combine(root, "img", topic, src)}";

      if (File.Exists(imgFilePath))
      {
        using (Image image = Image.FromFile(imgFilePath))
        {
          return (image.Width, image.Height);
        }
      }

      return (0, 0);
    }

    public ImageStory? DoGetStoryText(string topic, string src, CultureInfo ui)
    {
      var htmlFilePath = ResolveStoryJsonPath(topic, src, ui);
      if (htmlFilePath is null)
      {
        return null;
      }

      if (File.Exists(htmlFilePath))
      {
        string jsonString = File.ReadAllText(htmlFilePath);

        // Deserialisieren in eine Liste von Dictionary-Einträgen
        return new ImageStory(jsonString);
      }

      return null;
    }

    private string? ResolveStoryJsonPath(string topic, string src, CultureInfo ui)
    {
      var root = _hostingEnvironment.WebRootPath;
      var baseWithoutJson = Path.Combine(root, "img", topic, src); // e.g., M13.jpg

      // Try: exact culture -> language -> neutral
      var candidates = new[]
      {
        $"{baseWithoutJson}.{ui.Name}.json",                     // M13.jpg.de-DE.json
        $"{baseWithoutJson}.{ui.TwoLetterISOLanguageName}.json", // M13.jpg.de.json
        $"{baseWithoutJson}.json"                                // M13.jpg.json
      };

      foreach (var candidate in candidates)
      {
        if (File.Exists(candidate))
          return candidate;
      }

      return null;
    }

    public async Task<bool> OverlayExists(string topic, string src)
    {
      bool rc = false;
      await Task.Run(() =>
      {
        // Check for both old JPG overlay and new JSON overlay
        rc = DoOverlayExists(topic, src) || DoOverlayJsonExists(topic, src);
      });
      return rc;
    }

    private bool DoOverlayExists(string topic, string src)
    {
      var root = _hostingEnvironment.WebRootPath;
      string htmlFilePath = $"{Path.Combine(root, "img", topic, $"ovl_{src}")}";

      return File.Exists(htmlFilePath);
    }

    private bool DoOverlayJsonExists(string topic, string src)
    {
      var root = _hostingEnvironment.WebRootPath;
      var baseWithoutExt = Path.GetFileNameWithoutExtension(src);
      var svgPath = Path.Combine(root, "img", topic, $"svg_{baseWithoutExt}.overlay.json");
      if (File.Exists(svgPath)) return true;
      var legacyPath = Path.Combine(root, "img", topic, $"{baseWithoutExt}.overlay.json");
      return File.Exists(legacyPath);
    }

    public string GetOverlayPathForImage(string topic, string src)
    {
      var root = _hostingEnvironment.WebRootPath;
      var baseWithoutExt = Path.GetFileNameWithoutExtension(src);
      return Path.Combine(root, "img", topic, $"svg_{baseWithoutExt}.overlay.json");
    }

    private static readonly JsonSerializerOptions OverlayJsonOptions = new()
    {
      PropertyNameCaseInsensitive = true,
      WriteIndented = true
    };

    public void SaveOverlayData(string topic, string src, OverlayData data)
    {
      var path = GetOverlayPathForImage(topic, src);
      var json = JsonSerializer.Serialize(data, OverlayJsonOptions);
      File.WriteAllText(path, json);
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

    private OverlayData? DoGetOverlayData(string topic, string src, CultureInfo ui)
    {
      var root = _hostingEnvironment.WebRootPath;
      var baseWithoutExt = Path.GetFileNameWithoutExtension(src);

      // Try svg_ prefix first
      var candidates = new[]
      {
        Path.Combine(root, "img", topic, $"svg_{baseWithoutExt}.overlay.{ui.TwoLetterISOLanguageName}.json"),
        Path.Combine(root, "img", topic, $"svg_{baseWithoutExt}.overlay.json")
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

    public string Unwrap(string input)
    {
      // <a href="https://www.abenteuer-sterne.de" target="_blank">Abenteuer-Sterne</a>      
      string pattern1 = @"###(.*?)###(.*?)###";

      if (Regex.IsMatch(input, pattern1))
      {
        string replacement1 = "<a target='_blank' href=https://$2><span style='color:azure; font-weight:bold; text-decoration: none;'>$1</span></a>";

        string result1 = Regex.Replace(input, pattern1, replacement1);
        return result1;
      }

      // https://de.wikipedia.org/wiki/Deneb
      string pattern2 = @"##(.*?)##";
      string replacement2 = "<a target='_blank' href=https://$1><span style='color:azure; font-weight:bold; text-decoration: none;'>$1</span></a>";

      string result2 = Regex.Replace(input, pattern2, replacement2);
      return result2;

    }

    public async Task<List<CommentItem>> GetCommentHistory(string topic, string src)
    {
      var rc = new List<CommentItem>();
      var root = _hostingEnvironment.WebRootPath;
      string commentsFilePath = $"{Path.Combine(root, "img", topic, $"{src}.comments.json")}";
      if (File.Exists(commentsFilePath))
      {
        await Task.Run(() =>
        {
          var jsonString = File.ReadAllText(commentsFilePath);
          rc = JsonSerializer.Deserialize<List<CommentItem>>(jsonString) ?? new List<CommentItem>();
        });
      }
      return rc;
    }

    public async Task AddComment(string topic, string src, CommentItem comment, bool isValid)
    {
      var root = _hostingEnvironment.WebRootPath;
      string commentsFilePath = isValid ?
          $"{Path.Combine(root, "img", topic, $"{src}.comments.json")}" :
          $"{Path.Combine(root, "img", topic, $"{src}.denied.json")}";
      var comments = new List<CommentItem>();
      if (File.Exists(commentsFilePath))
      {
        var jsonString = await File.ReadAllTextAsync(commentsFilePath);
        comments = JsonSerializer.Deserialize<List<CommentItem>>(jsonString) ?? new List<CommentItem>();
      }
      comments.Add(comment);
      var updatedJson = JsonSerializer.Serialize(comments, new JsonSerializerOptions { WriteIndented = true });
      await File.WriteAllTextAsync(commentsFilePath, updatedJson);
    }
  }
}




