namespace Focus2Infinity.Services
{
  using Focus2Infinity.Data;
  using Microsoft.Extensions.Localization;

  public class ImageCatalogService
  {
    private readonly ImagePathResolver _pathResolver;
    private readonly ImageMetadataService _imageMetadataService;

    public ImageCatalogService(ImagePathResolver pathResolver, ImageMetadataService imageMetadataService)
    {
      _pathResolver = pathResolver;
      _imageMetadataService = imageMetadataService;
    }

    public async Task<List<string>> GetMainTopics()
    {
      var rc = new List<string>();
      await Task.Run(() => { rc.AddRange(new string[] { "Galaxies", "Nebulae", "Clusters", "Eclipses", "Milkyway", "Moon", "Sun", "Sunsets", "Comets", "Landscapes", "StarTrails", "Others" }); });
      return rc;
    }

    public async Task<List<string>> GetSubTopicsSorted(string mainTopic, IStringLocalizer L)
    {
      var files = GetSubTopics(mainTopic);
      var list = await AnnotateWithDate(files, mainTopic, L);
      return ToSortedList(list);
    }

    public async Task<List<ImageItem>> GetAllTopics(IStringLocalizer L)
    {
      var rc = new List<ImageItem>();
      var allTopics = new List<Tuple<DateTime, string, string>>();

      foreach (var topic in await GetMainTopics())
      {
        var files = GetSubTopics(topic);
        var list = await AnnotateWithDate(files, topic, L);
        foreach (var item in list)
        {
          allTopics.Add(new Tuple<DateTime, string, string>(item.Item1, item.Item2, topic));
        }
      }

      foreach (var kvp in allTopics.OrderByDescending(kvp => kvp.Item1))
      {
        rc.Add(new ImageItem(kvp.Item2, kvp.Item3));
      }

      return rc;
    }

    public async Task<(string, string)> GetPreviosNextReferences(string topic, string name, string context, IStringLocalizer<SharedResource> localizer)
    {
      List<(string, string)> items;
      if (context == "all")
      {
        var imageItems = await GetAllTopics(localizer);
        items = imageItems.Select(ii => (ii.Topic, ii.Src)).ToList();
      }
      else
      {
        var imageItems = await GetSubTopicsSorted(topic, localizer);
        items = imageItems.Select(ii => (topic, ii)).ToList();
      }

      var idx = items.FindIndex(item => item.Item1 == topic && item.Item2 == name);
      var prev = idx > 0 ? items[idx - 1] : items[items.Count - 1];
      var next = idx < items.Count - 1 ? items[idx + 1] : items[0];

      return ($"/imagedetails/{prev.Item1}/{prev.Item2}", $"/imagedetails/{next.Item1}/{next.Item2}");
    }

    private IEnumerable<FileInfo> GetSubTopics(string mainTopic)
    {
      string dirPath = _pathResolver.GetTopicDirectory(mainTopic);
      if (!Directory.Exists(dirPath))
        return new List<FileInfo>();

      var dirInfo = new DirectoryInfo(dirPath);

      var rc = dirInfo.EnumerateFiles("*.*")
                               .Where(file => (file.Extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
                                               file.Extension.Equals(".png", StringComparison.OrdinalIgnoreCase) ||
                                               file.Extension.Equals(".tif", StringComparison.OrdinalIgnoreCase))
                                               && !file.Name.StartsWith("tbn_")
                                               && !file.Name.StartsWith("ovl_")
                                               && !file.Name.StartsWith("full_"));
      return rc;
    }

    private async Task<List<Tuple<DateTime, string>>> AnnotateWithDate(IEnumerable<FileInfo> files, string mainTopic, IStringLocalizer L)
    {
      var list = new List<Tuple<DateTime, string>>();
      foreach (var f in files)
      {
        var src = await _imageMetadataService.GetStoryText(mainTopic, f.Name);
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

    private List<string> ToSortedList(List<Tuple<DateTime, string>> list)
    {
      var rc = new List<string>();
      foreach (var kvp in list.OrderByDescending(kvp => kvp.Item1))
      {
        rc.Add(kvp.Item2);
      }
      return rc;
    }
  }
}
