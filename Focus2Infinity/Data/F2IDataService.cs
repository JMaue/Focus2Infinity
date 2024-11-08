﻿namespace Focus2Infinity.Data
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
      await Task.Run(() => { rc.AddRange(new string[] { "Galaxies", "Nebulae", "Clusters", "Planets", "Eclipses", "Milkyway", "Moon", "Sun", "Sunsets", "Auroras", "Landscapes", "Others" }); });
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
          list.Add(new Tuple<DateTime, string>(src.GetDateTime(), f.Name));
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
                                               && !file.Name.StartsWith("tbn_"));
      return rc;
    }

    public async Task<List<Tuple<string, string>>> GetAllTopics()
    {
      List<Tuple<string, string>> rc = new List<Tuple<string, string>>();
      
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
          rc.Add(new Tuple<string, string>(kvp.Item2, kvp.Item3));
        }
      });


      return rc;
    }

    public async Task<Dictionary<string, string>> GetStoryText(string topic, string src)
    {
      Dictionary<string, string> kvp = new Dictionary<string, string>();
      await Task.Run(() =>
      {
        kvp = DoGetStoryText(topic, src);
       
      });

      return kvp;
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

    public Dictionary<string, string> DoGetStoryText(string topic, string src)
    {
      Dictionary<string, string> keyValuePairs = new Dictionary<string, string>();
      var root = _hostingEnvironment.WebRootPath;
      string htmlFilePath = $"{Path.Combine(root, "img", topic, src)}.json";

      if (File.Exists(htmlFilePath))
      {
        string jsonString = File.ReadAllText(htmlFilePath);

        // Deserialisieren in eine Liste von Dictionary-Einträgen
        keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonString);
      }

      return keyValuePairs;
    }

    public string Unwrap (string input)
    {
      // ##https://www.abenteuer-sterne.de##
      // <a href="https://www.abenteuer-sterne.de" target="_blank">Abenteuer-Sterne</a>

      string pattern = @"##(.*?)##";
      string replacement = "<a target='_blank' href=https://$1><span style='color:aliceblue; font-weight:bold'>$1</span></a>";

      string result = Regex.Replace(input, pattern, replacement);
      return result;
    }
  }
}




