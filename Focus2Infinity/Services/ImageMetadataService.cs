namespace Focus2Infinity.Services
{
  using Focus2Infinity.Data;
  using System.Drawing;
  using System.Globalization;
  using System.Text.RegularExpressions;

  public class ImageMetadataService
  {
    private readonly ImagePathResolver _pathResolver;

    public ImageMetadataService(ImagePathResolver pathResolver)
    {
      _pathResolver = pathResolver;
    }

    public async Task<ImageStory> GetStoryText(string topic, string src)
    {
      var ui = CultureInfo.CurrentUICulture;
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

    public string Unwrap(string input)
    {
      string pattern1 = @"###(.*?)###(.*?)###";
      if (Regex.IsMatch(input, pattern1))
      {
        string replacement1 = "<a target='_blank' href=https://$2><span style='color:azure; font-weight:bold; text-decoration: none;'>$1</span></a>";
        string result1 = Regex.Replace(input, pattern1, replacement1);
        return result1;
      }

      string pattern2 = @"###(.*?)~~~(.*?)###";
      if (Regex.IsMatch(input, pattern2))
      {
        string replacement2 = "<a target='_blank' href=http://$2><span style='color:azure; font-weight:bold; text-decoration: none;'>$1</span></a>";
        string result2 = Regex.Replace(input, pattern2, replacement2);
        return result2;
      }

      string pattern3 = @"##(.*?)##";
      string replacement3 = "<a target='_blank' href=https://$1><span style='color:azure; font-weight:bold; text-decoration: none;'>$1</span></a>";
      string result3 = Regex.Replace(input, pattern3, replacement3);
      return result3;
    }

    private (int width, int height) DoGetImageFormat(string topic, string src)
    {
      string imgFilePath = _pathResolver.GetImagePath(topic, src);

      if (File.Exists(imgFilePath))
      {
        using (Image image = Image.FromFile(imgFilePath))
        {
          return (image.Width, image.Height);
        }
      }

      return (0, 0);
    }

    private ImageStory? DoGetStoryText(string topic, string src, CultureInfo ui)
    {
      var candidates = _pathResolver.GetStoryJsonCandidates(topic, src, ui);

      foreach (var candidate in candidates)
      {
        if (File.Exists(candidate))
        {
          string jsonString = File.ReadAllText(candidate);
          return new ImageStory(jsonString);
        }
      }

      return null;
    }
  }
}
