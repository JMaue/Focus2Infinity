
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
              { "en", "##en.wikipedia.org/wiki/Arecibo_message##" },
              { "nl", "##nl.wikipedia.org/wiki/Areciboboodschap##" },
              { "fr", "##fr.wikipedia.org/wiki/Message_d%27Arecibo##"} }
          ),
          new LinkInfo(
            "##www.adastralapalma.com/tours-en/observatory-tour/##",
            new Dictionary<string, string>
            {
              { "*", "##www.adastralapalma.com/tours-en/observatory-tour/##" }
            }
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
            "###Sternentstehungsgebiet###de.wikipedia.org/wiki/Orionnebel###",
            new Dictionary<string, string> {
              { "en", "###Star forming region###en.wikipedia.org/wiki/Orion_Nebula###" },
              { "nl", "###Sterrenvormingsgebied###nl.wikipedia.org/wiki/Orionnevel###" },
              { "fr", "###région de formation stellaire###fr.wikipedia.org/wiki/N%C3%A9buleuse_d%27Orion###" }
            }),
          new LinkInfo(
            "###Wega###de.wikipedia.org/wiki/Wega###",
            new Dictionary<string, string> {
              { "en", "###Vega###en.wikipedia.org/wiki/Vega###" },
              { "nl", "###Wega###nl.wikipedia.org/wiki/Wega###" },
              { "fr", "###Véga###fr.wikipedia.org/wiki/Véga###" }
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
            }),
          new LinkInfo(
            "###Herz- und Seele Nebel###de.wikipedia.org/wiki/Herznebel###",
            new Dictionary<string, string>
            {
              { "en", "###Heart and Soul Nebula###en.wikipedia.org/wiki/Heart_Nebula###" },
              { "fr", "###Nébuleuse du Cœur###fr.wikipedia.org/wiki/IC_1805###"},
              { "nl", "###Hart- and Zielnevel###nl.wikipedia.org/wiki/Hartnevel###" }
            }),
          new LinkInfo(
            "###Hexenkopfnebel###de.wikipedia.org/wiki/IC_2118###",
            new Dictionary<string, string>
            {
              { "en", "###Witchhead Nebula###en.wikipedia.org/wiki/IC_2118###" },
              { "nl", "###Heksenkopnevel###nl.wikipedia.org/wiki/NGC_1909###"},
              { "fr", "###La nébuleuse de la Tête de Sorcière###fr.wikipedia.org/wiki/NGC_1909###" }
            }),
          new LinkInfo(
            "###Feuerradgalaxie###de.wikipedia.org/wiki/Messier_101###",
            new Dictionary<string, string>
            {
              { "en", "###Pinwheel Galaxy###en.wikipedia.org/wiki/Pinwheel_Galaxy###" },
              { "nl", "###Windmolenstelsel###nl.wikipedia.org/wiki/Windmolenstelsel###"},
              { "fr", "###Galaxie du Moulinet###fr.wikipedia.org/wiki/M101_(galaxie)###" }
            }),
          new LinkInfo(
            "###NGC 2903###de.wikipedia.org/wiki/NGC_2903###",
            new Dictionary<string, string>
            {
              { "en", "###NGC 2903###en.wikipedia.org/wiki/NGC_2903###" },
              { "nl", "###NGC 2903###nl.wikipedia.org/wiki/NGC_2903###"},
              { "fr", "###NGC 2903###fr.wikipedia.org/wiki/NGC_2903###" }
            }),
          new LinkInfo(
            "###Medusa Nebel###de.wikipedia.org/wiki/Medusanebel###",
            new Dictionary<string, string>
            {
              { "en", "###Medusa Nebula###en.wikipedia.org/wiki/Medusa_Nebula###" },
              { "nl", "###Medusanevel###nl.wikipedia.org/wiki/Medusanevel###"},
              { "fr", "###Nébuleuse de la Méduse###fr.wikipedia.org/wiki/Abell_21###" }
            }),
          new LinkInfo(
            "###MAGIC-Teleskope###de.wikipedia.org/wiki/MAGIC-Teleskope###",
            new Dictionary<string, string>
            {
              { "en", "###MAGIC-Teleskope###en.wikipedia.org/wiki/MAGIC_(telescope)###" },
              { "nl", "###MAGIC-Teleskope###en.wikipedia.org/wiki/MAGIC_(telescope)###"},
              { "fr", "###MAGIC-Teleskope###fr.wikipedia.org/wiki/Major_Atmospheric_Gamma-ray_Imaging_Cherenkov_Telescope###" }
            }),
          new LinkInfo(
            "###Gran Telescopio Canarias###de.wikipedia.org/wiki/Gran_Telescopio_Canarias###",
            new Dictionary<string, string>
            {
              { "en", "###Gran Telescopio Canarias###en.wikipedia.org/wiki/Gran_Telescopio_Canarias###" },
              { "nl", "###Gran Telescopio Canarias###nl.wikipedia.org/wiki/Gran_Telescopio_Canarias###"},
              { "fr", "###Gran Telescopio Canarias###fr.wikipedia.org/wiki/Gran_Telescopio_Canarias###" }
            }),
          new LinkInfo(
            "###Athos Star Campus~~~athos.org###",  // link via http instead of https
            new Dictionary<string, string>
            {
              { "*", "###Athos Star Campus~~~athos.org###" }
            }),     };

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

    internal static List<string> ReplaceLinkWithToken(ref string value)
    {
      var tokens = new List<string>();
      string val = value;
      var idx = _linksPerLanguage.FindIndex(x => val.Contains(x.OriginalLink));
      while (idx >= 0)
      {
        var li = _linksPerLanguage[idx];
        var token = $"##{idx}##";
        val = val.Replace (li.OriginalLink, token);
        tokens.Add(token);
        idx = _linksPerLanguage.FindIndex(x => val.Contains(x.OriginalLink));
      }
      value = val;
      return tokens;
    }

    internal static string RestoreTranslatedLinkFromToken(string? translatedValue, List<string> tokensForLink, string language)
    {
      if (translatedValue == null)
        return string.Empty;

      while (tokensForLink.Any())
      {
        var token = tokensForLink[0];
        int idx = Convert.ToInt16(token.Replace("##", ""));
        if (idx >= 0 && idx <= _linksPerLanguage.Count)
        {
          var li = _linksPerLanguage[idx];
          if (li.LinksPerLanguage.ContainsKey(language))
          {
            translatedValue = translatedValue.Replace(token, li.LinksPerLanguage[language]);
          }
          else if (li.LinksPerLanguage.ContainsKey("*"))
          {
            translatedValue = translatedValue.Replace(token, li.LinksPerLanguage["*"]);
          }
        }
        tokensForLink.RemoveAt(0);
      }

      return translatedValue;
    }
  }
}
