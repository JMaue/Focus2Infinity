
using System.Text.RegularExpressions;

namespace TranslationManager
{
  public class LinkCorrector
  {
    private class LinkInfo
    {
      public string OriginalLink { get; set; }
      public Dictionary<string, string> LinksPerLanguage { get; set; }

      public LinkInfo(string orig, Dictionary<string, string> replacements)
      {
        OriginalLink = orig;
        LinksPerLanguage = replacements;
      }
    }

    // <a href="https://www.abenteuer-sterne.de" target="_blank">Abenteuer-Sterne</a>      
    private static string pattern1 = @"###(.*?)###(.*?)###";
    private static string pattern2 = @"##(.*?)##";

    private static List<LinkInfo> _linksPerLanguage = new List<LinkInfo>
      {
          new LinkInfo(
            "##de.wikipedia.org/wiki/Arecibo-Botschaft##",
            new Dictionary<string, string> {
              { "en", "##en.wikipedia.org/wiki/Arecibo message##" },
              { "nl", "##nl.wikipedia.org/wiki/Areciboboodschap##" },
              { "fr", "##fr.wikipedia.org/wiki/Message_d%27Arecibo##"} }
          ),
          new LinkInfo(
            "##en.wikipedia.org/wiki/Pinwheel_Galaxy#/media/File:M101_hires_STScI-PRC2006-10a.jpg##",
            new Dictionary<string, string> {
              { "*", "##en.wikipedia.org/wiki/Pinwheel_Galaxy#/media/File:M101_hires_STScI-PRC2006-10a.jpg##" }
            }
          ),
          new LinkInfo(
            "##science.nasa.gov/mission/hubble/science/explore-the-night-sky/hubble-messier-catalog/messier-51/##",
            new Dictionary<string, string> {
              { "*", "##science.nasa.gov/mission/hubble/science/explore-the-night-sky/hubble-messier-catalog/messier-51/##" }
            }),
          new LinkInfo(
            "##www.abenteuer-sterne.de##",
            new Dictionary<string, string> {
              { "*", "##www.abenteuer-sterne.de##" }
            }),
          new LinkInfo(
            "###Deneb###de.wikipedia.org/wiki/Deneb###",
            new Dictionary<string, string> {
              { "en", "###Deneb###en.wikipedia.org/wiki/Deneb###" },
              { "nl", "###Deneb###nl.wikipedia.org/wiki/Deneb###" },
              { "fr", "###Deneb###fr.wikipedia.org/wiki/Deneb###" }
            }),
          new LinkInfo(
            "###Altair###de.wikipedia.org/wiki/Altair###",
            new Dictionary<string, string> {
              { "en", "###Altair###en.wikipedia.org/wiki/Altair###" },
              { "nl", "###Altair###nl.wikipedia.org/wiki/Altair_(ster)###" },
              { "fr", "###Altaïr###fr.wikipedia.org/wiki/Altaïr###" }
            }),
          new LinkInfo(
            "###Wega###de.wikipedia.org/wiki/Wega###",
            new Dictionary<string, string> {
              { "en", "###Vega###en.wikipedia.org/wiki/Vega###" },
              { "nl", "###Wega###nl.wikipedia.org/wiki/Wega###" },
              { "fr", "###V%C3%A9ga###fr.wikipedia.org/wiki/V%C3%A9ga###" }
            }),
          new LinkInfo(
            "###Planit Pro###www.planitphoto.com###",
            new Dictionary<string, string> {
              { "*", "###Planit Pro###www.planitphoto.com###" }
            }),
          new LinkInfo(
            "###Cirrus-Nebels###de.wikipedia.org/wiki/Cirrusnebel###",
            new Dictionary<string, string> {
              { "en", "###Veil Nebula###en.wikipedia.org/wiki/Veil_Nebula###" },
              { "fr", "###Nébuleuse du Voile###fr.wikipedia.org/wiki/N%C3%A9buleuse_du_Voile###" },
              { "nl", "###heksenbezemnevel###nl.wikipedia.org/wiki/NGC_6960###" }
            }),
          new LinkInfo(
            "###H-alpha Linie###de.wikipedia.org/wiki/H-alpha###",
            new Dictionary<string, string> {
              { "en", "###H-alpha Line###en.wikipedia.org/wiki/Hydrogen-alpha###" },
              { "fr", "###Ligne H-alpha###fr.wikipedia.org/wiki/H%CE%B1###" },
              { "nl", "###H-alpha-lijn###en.wikipedia.org/wiki/Hydrogen-alpha###" }
            }),
          new LinkInfo(
            "###h und ? (chi) Persei###www.spektrum.de/alias/wunder-des-weltalls/h-und-chi-persei/1430186###",
            new Dictionary<string, string> {
              { "*", "###h und ? (chi) Persei###www.spektrum.de/alias/wunder-des-weltalls/h-und-chi-persei/1430186###" }
            })
      };

    public static bool MatchesPattern(string inputTxt)
    {
      if (Regex.IsMatch(inputTxt, pattern1))
        return true;

      if (Regex.IsMatch(inputTxt, pattern2))
        return true;

      return false;
    }

    internal static bool ContainsKey(string value)
    {
      return _linksPerLanguage.Any(x => value.Contains (x.OriginalLink));
    }

    internal static string? ReplaceLinkWithToken(ref string value)
    {
      string val = value;
      var idx = _linksPerLanguage.FindIndex(x => val.Contains(x.OriginalLink));
      if (idx >= 0)
      {
        var li = _linksPerLanguage[idx];
        var token = $"##{idx}##";
        value = value.Replace (li.OriginalLink, token);
        return token;
      }
      return null;
    }

    internal static string RestoreTranslatedLinkFromToken(string? translatedValue, string tokenForLink, string language)
    {
      if (translatedValue == null)
        return string.Empty;

      int idx = Convert.ToInt16(tokenForLink.Replace("##", ""));
      if (idx >= 0 && idx <= _linksPerLanguage.Count)
      {
        var li = _linksPerLanguage[idx];
        if (li.LinksPerLanguage.ContainsKey(language))
        {
          return translatedValue.Replace(tokenForLink, li.LinksPerLanguage[language]);
        }
        else if (li.LinksPerLanguage.ContainsKey("*"))
        {
          return translatedValue.Replace(tokenForLink, li.LinksPerLanguage["*"]);
        }
      }

      return translatedValue;
    }
  }
}
