namespace Focus2Infinity.Data
{
  using System.Text.Json;

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

    public async Task<List<string>> GetSubTopics(string mainTopic)
    {
      string htmlFilePath = Path.Combine(_hostingEnvironment.WebRootPath, "img", mainTopic);

      List<string> rc = new List<string>(); 
      if (!Directory.Exists(htmlFilePath))
        return rc;

      await Task.Run(() =>
      {
        var dirInfo = new DirectoryInfo(htmlFilePath);

        var files = dirInfo.EnumerateFiles("*.*")
                                 .Where(file => (    file.Extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
                                                     file.Extension.Equals(".png", StringComparison.OrdinalIgnoreCase) ||
                                                     file.Extension.Equals(".tif", StringComparison.OrdinalIgnoreCase))
                                                 && !file.Name.StartsWith("tbn_"))
                                 .OrderByDescending(file => file.CreationTime);
       
        rc.AddRange (files.Select(file => file.Name));
      });
      return rc;
    }

    public async Task<Dictionary<string, string>> GetStoryText(string topic, string src)
    {
      Dictionary<string, string> keyValuePairs = new Dictionary<string, string>();
      var root = _hostingEnvironment.WebRootPath;
      //string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(src);
      //fileNameWithoutExtension = Path.GetFullPath(fileNameWithoutExtension);
      string htmlFilePath = $"{Path.Combine(root, "img", topic, src)}.json";

      await Task.Run(() =>
      {
        if (File.Exists(htmlFilePath))
        {
          string jsonString = File.ReadAllText(htmlFilePath);

          // Deserialisieren in eine Liste von Dictionary-Einträgen
          keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonString);
        }
       
      });

      return keyValuePairs;
    }
  }
}




