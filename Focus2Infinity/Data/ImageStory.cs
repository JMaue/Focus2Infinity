using Microsoft.Extensions.Localization;
using System.Text.Json;

namespace Focus2Infinity.Data
{
  public class ImageStory
  {
    Dictionary<string, string>? _metaDescription;

    public ImageStory(string descriptionAsJson)
    {
      // Deserialisieren in eine Liste von Dictionary-Einträgen
      _metaDescription = JsonSerializer.Deserialize<Dictionary<string, string>>(descriptionAsJson);
    }

    private static string C_Headline = "Headline";
    private static string C_Details = "Details";
    private static string C_Date = "Datum";

    public string GetHeadline()
    {
      if (_metaDescription != null && _metaDescription.ContainsKey(C_Headline))
      {
        return _metaDescription[C_Headline];
      }
      else
        return "";
    }


    public DateTime GetDateTaken (IStringLocalizer localizer)
    {
      if (_metaDescription != null)
      {
        DateTime dt = DateTime.MinValue;
        try
        {
        var lkey = localizer[C_Date].Value;
        if (_metaDescription.ContainsKey(lkey))
          dt = DateTime.Parse(_metaDescription[lkey]);
        }
        catch (Exception e)
        {

        }
        return dt;
      }
      return DateTime.MinValue;
    }

    public IEnumerable<KeyValuePair<string, string>> Content
    {
      get
      {
        if (_metaDescription == null)
          yield break;

        foreach (var key in _metaDescription.Keys)
        {
          if (key == "Headline" || key == C_Details)
            continue;

          yield return new KeyValuePair<string, string>(key, _metaDescription[key]);
        }
      }
    }

    public IEnumerable<KeyValuePair<string, string>> DetailedContent
    {
      get
      {
        if (_metaDescription == null)
          yield break;

        foreach (var c in Content)
          yield return c;

        if (_metaDescription.ContainsKey(C_Details))
        {
          yield return new KeyValuePair<string, string>(C_Details, _metaDescription[C_Details]);
        }
      }
    }
  }
}