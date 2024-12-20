﻿namespace Focus2Infinity.Data
{
  public static class DataHelper
  {
    private static string Headline = "Headline";
    private static string Details = "Details";
    private static string Date = "Datum";

    public static string GetHeadline (this Dictionary<string, string> content)
    {
      if (content.ContainsKey(Headline))
      {
        return content[Headline];
      }
      return "";
    }

    public static DateTime GetDateTime(this Dictionary<string, string> content)
    {
      if (content.ContainsKey(Date))
      {
        DateTime dt = DateTime.MinValue;
        try
        {
          dt = DateTime.Parse(content[Date]);
        }
        catch(Exception e)
        {

        }
        return dt;
      }
      return DateTime.MinValue;
    }

    public static IEnumerable<KeyValuePair<string, string>> GetContent(this Dictionary<string, string> content)
    {
      foreach (var key in content.Keys)
      {
        if (key == Headline || key == Details)
          continue;

        yield return new KeyValuePair<string, string>(key, content[key]);
      }
    }

    public static IEnumerable<KeyValuePair<string, string>> GetDetailedContent(this Dictionary<string, string> content)
    {
      foreach (var c in content.GetContent())
        yield return c;

      if (content.ContainsKey(Details))
      {
        yield return new KeyValuePair<string, string>(Details, content[Details]);
      }
    }
  }
}