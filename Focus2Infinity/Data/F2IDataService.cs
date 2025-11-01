namespace Focus2Infinity.Data
{
  using System.Collections.Generic;
  using System.Text.Json;
  using System.Text.RegularExpressions;
  using System.Drawing;

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

    public async Task<List<string>> GetSubTopicsSorted(string mainTopic)
    {
      var rc = new List<string>();
      await Task.Run(() =>
      {
        var files = GetSubTopics(mainTopic);
        var list = AnnotateWithDate(files, mainTopic);
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

    private List<Tuple<DateTime, string>> AnnotateWithDate(IEnumerable<FileInfo> files, string mainTopic)
    {
      var list = new List<Tuple<DateTime, string>>();
      foreach (var f in files)
      {
        var src = GetStoryText(mainTopic, f.Name).Result;
        if (src != null)
        {
          list.Add(new Tuple<DateTime, string>(src.DateTaken, f.Name));
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

    public async Task<List<ImageItem>> GetAllTopics()
    {
      List<ImageItem> rc = new List<ImageItem>();
      
      await Task.Run(() =>
      {
        var allTopics = new List<Tuple<DateTime, string, string>>();
        foreach (var topic in GetMainTopics().Result)
        {
          var files = GetSubTopics(topic);
          var list = AnnotateWithDate(files, topic);
          foreach (var item in list)
          {
            allTopics.Add(new Tuple<DateTime, string, string>(item.Item1, item.Item2, topic));
          }
        }

        foreach (var kvp in allTopics.OrderByDescending(kvp => kvp.Item1))
        {
          rc.Add(new ImageItem (kvp.Item2, kvp.Item3));
        }
      });


      return rc;
    }

    public async Task<ImageStory> GetStoryText(string topic, string src)
    {
      ImageStory? rc = null;
      await Task.Run(() =>
      {
        rc = DoGetStoryText(topic, src);
      });

      return rc ?? new ImageStory ("{}");
    }

    public async Task<(int width, int height)> GetImageFormat (string topic, string src)
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

    public ImageStory? DoGetStoryText(string topic, string src)
    {
      var root = _hostingEnvironment.WebRootPath;
      string htmlFilePath = $"{Path.Combine(root, "img", topic, src)}.json";

      if (File.Exists(htmlFilePath))
      {
        string jsonString = File.ReadAllText(htmlFilePath);

        // Deserialisieren in eine Liste von Dictionary-Einträgen
        return new ImageStory (jsonString);
      }

      return null;
    }

    public async Task<bool> OverlayExists(string topic, string src)
    { 
      bool rc = false;
      await Task.Run(() =>
      {
        rc = DoOverlayExists(topic, src);
      });

      return rc;
    }

    private bool DoOverlayExists(string topic, string src)
    {
      var root = _hostingEnvironment.WebRootPath;
      string htmlFilePath = $"{Path.Combine(root, "img", topic, $"ovl_{src}")}";

      return File.Exists(htmlFilePath);
    }

    public string Unwrap(string input)
    {
      // https://de.wikipedia.org/wiki/Deneb
      string pattern1 = @"###(.*?)###(.*?)###";

      if (Regex.IsMatch(input, pattern1))
      {
        string replacement1 = "<a target='_blank' href=https://$2><span style='color:azure; font-weight:bold; text-decoration: none;'>$1</span></a>";

        string result1 = Regex.Replace(input, pattern1, replacement1);
        return result1;
      }

      // ##https://www.abenteuer-sterne.de##
      // <a href="https://www.abenteuer-sterne.de" target="_blank">Abenteuer-Sterne</a>

      string pattern2 = @"##(.*?)##";
      string replacement2 = "<a target='_blank' href=https://$1><span style='color:azure; font-weight:bold; text-decoration: none;'>$1</span></a>";

      string result2 = Regex.Replace(input, pattern2, replacement2);
      return result2;

    }
  }
}




