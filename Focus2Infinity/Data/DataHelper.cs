namespace Focus2Infinity.Data
{
  public static class DataHelper
  {
    private static string Headline = "Headline";
    public static string GetHeadline (this Dictionary<string, string> content)
    {
      if (content.ContainsKey(Headline))
      {
        return content[Headline];
      }
      return "";
    }

    public static IEnumerable<KeyValuePair<string, string>> GetContent(this Dictionary<string, string> content)
    {
      foreach (var key in content.Keys)
      {
        if (key == Headline)
          continue;

        yield return new KeyValuePair<string, string>(key, content[key]);
      }
    }
  }
}