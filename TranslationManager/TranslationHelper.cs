using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using Cli;

namespace TranslationManager
{
  internal class TranslationHelper
  {
    private static DeepLTranslator _deepl = null;
    private static DeepLTranslator Deepl
    {
      get { 
        if (_deepl == null)
        {
          var client = new HttpClient();
          var key = File.ReadAllText(Keyfile);
          _deepl = new DeepLTranslator(client, key);
        }
        return _deepl;
      }
    }

    public static string Keyfile { get; internal set; } = string.Empty;

    private static Dictionary<string, Dictionary<string, string>> _allTerms;

    private static void InitializeAllTerms()
    {
      if (_allTerms != null)
        return;

      _allTerms = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
      _allTerms["en"] = new Dictionary<string, string>(StringComparer.Ordinal);
      _allTerms["fr"] = new Dictionary<string, string>(StringComparer.Ordinal);
      _allTerms["nl"] = new Dictionary<string, string>(StringComparer.Ordinal);

      _allTerms["fr"].Add("Ort", "Lieu");
      _allTerms["fr"].Add("Kamera", "Caméra");
      _allTerms["fr"].Add("Optik", "Optique");
      _allTerms["fr"].Add("Herz- und Seele Nebel", "Nébuleuse du Cœur et de l'Âme");
      _allTerms["fr"].Add("Am Schliffkopf", "au Schliffkopf");
      _allTerms["fr"].Add("Sturmvogel", "Nébuleuse du Voile");
      _allTerms["fr"].Add("Feuerrad Galaxie", "Galaxie du Moulinet");
      _allTerms["fr"].Add("Hexenkopfnebel", "La nébuleuse de la Tête de Sorcière");
      _allTerms["fr"].Add("ATHOS Centro Astronomico, La Palma", "ATHOS Centro Astronomico, La Palma");
      _allTerms["fr"].Add("Feuerradgalaxie, Messier 101", "Galaxie du Moulinet, Messier 101");
      _allTerms["fr"].Add("Medusa Nebel, Abell 21", "La nébuleuse de la Méduse, Abell 21");

      _allTerms["nl"].Add("Ort", "Locatie");
      _allTerms["nl"].Add("Exposure Settings", "Belichtings instellingen");
      _allTerms["nl"].Add("Herz- und Seele Nebel", "Hart- and Zielnevel");
      _allTerms["nl"].Add("Am Schliffkopf", "op de Schliffkopf");
      _allTerms["nl"].Add("Feuerrad Galaxie", "Windmolenstelsel");
      _allTerms["nl"].Add("Hexenkopfnebel", "Heksenkopnevel");
      _allTerms["nl"].Add("ATHOS Centro Astronomico, La Palma", "ATHOS Centro Astronomico, La Palma");
      _allTerms["nl"].Add("Feuerradgalaxie, Messier 101", "Windmolenstelsel, Messier 101");
      _allTerms["nl"].Add("Medusa Nebel, Abell 21", "Medusanevel, Abell 21");

      _allTerms["en"].Add("Ort", "Location");
      _allTerms["en"].Add("Herz- und Seele Nebel", "Heart- and Soul nebula");
      _allTerms["en"].Add("Sturmvogel", "Witch's Broom");
      _allTerms["en"].Add("Erdbeermond", "Strawberry Moon");
      _allTerms["en"].Add("Feuerrad Galaxie", "Pinwheel Galaxy");
      _allTerms["en"].Add("Am Schliffkopf", "At the Schliffkopf");
      _allTerms["en"].Add("Hexenkopfnebel", "Witchhead Nebula");
      _allTerms["en"].Add("ATHOS Centro Astronomico, La Palma", "ATHOS Centro Astronomico, La Palma");
      _allTerms["en"].Add("Feuerradgalaxie, Messier 101", "Pinwheel, Messier 101");
      _allTerms["en"].Add("Medusa Nebel, Abell 21", "Medusa Nebula, Abell 21");
    } 

    public static void ShowTranslatePage()
    {
      //bool loop = false; //jump back to main menu after an action is done
      bool loop = true;    //stay in this submenu after an action is done
      do
      {
        Console.Clear();
        CliTools.WriteTitle("Perform translations");

        var selectedItem = CliTools.ShowMenu("Translate", 1,
           new MenuItem("e", "EN", () => TranslateAll("en")),
           new MenuItem("f", "FR", () => TranslateAll("fr")),
           new MenuItem("n", "NL", () => TranslateAll("nl")),
           new MenuItem("a", "ALL", () => TranslateAll("")),
           new MenuItem("Esc", "Cancel", () => { loop = false; })
         );

      } while (loop);
    }

    internal static void TranslateAll(string language)
    {
      InitializeAllTerms();

      var rootPath = FileHelper.FindImgRoot();
      if (rootPath == null)
      {
        Console.Error.WriteLine("wwwroot\\img not found.");
        return;
      }

      bool overrideExisting = CliTools.ConfirmWith1OrBack($"Do you want to override any existing files?");

      int totalNumberOfFiles = 0;
      int noOfProcessedFiles = 0;

      string[] languages = language.Length > 0 ? new string[] { language } : new string[] { "fr", "nl", "en" };

      var allTerms = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
      foreach (var lang in languages)
      {
        allTerms[lang] = new Dictionary<string, string>(StringComparer.Ordinal);
      }

      var allFiles = Directory.EnumerateFiles(rootPath, "*.json", SearchOption.AllDirectories).ToList();
      foreach (var file in allFiles)
      {
        // do not start translation on already translated files
        if (file.Contains("en.json") || file.Contains("fr.json") || file.Contains("nl.json"))
          continue;

        totalNumberOfFiles++;
        try
        {
          // translate it into all target languages
          foreach (var lang in languages)
          {
            var fileNameTranslation = file.Replace(".json", $".{lang}.json");
            TranslateFile(fileNameTranslation, overrideExisting);
            Console.WriteLine($"{fileNameTranslation}: processed. ({++noOfProcessedFiles})");
          }
        }
        catch (Exception ex)
        {
          Console.Error.WriteLine($"Skipped invalid JSON: {file} ({ex.Message})");
          Console.ReadLine();
        }
      }

      // save the whole vocabulary
      foreach (var lang in languages)
      {
        var translatedJson = JsonSerializer.Serialize(allTerms[lang], new JsonSerializerOptions { WriteIndented = true });
        var translatedfile = Path.Combine(rootPath, $"AllTerms.{lang}.json");
        File.WriteAllText(translatedfile, translatedJson);
      }

      Console.WriteLine($"Scanned {totalNumberOfFiles} JSON file(s) under: {rootPath}");
      Console.WriteLine($"Processed {noOfProcessedFiles} file(s).");
    }

    public static void TranslateFile(string fileNameTranslation, bool overrideExisting)
    {
      InitializeAllTerms();

      if (fileNameTranslation == null || fileNameTranslation.Length == 0)
        return;

      // extract language code from file name
      var fileName = Path.GetFileName(fileNameTranslation);
      var parts = fileName.Split('.');
      if (parts.Length < 3)
      {
        Console.WriteLine($"File name {fileName} does not contain language code.");
        return;
      }
      var language = parts[parts.Length - 2];
      var origFile = fileNameTranslation.Replace($".{language}.json", ".json");

      // read the original json dictionary (assuming german)
      string jsonString = File.ReadAllText(origFile);
      var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonString);

      // var fileNameTranslation = file.Replace(".json", $".{language}.json");
      if (File.Exists(fileNameTranslation))
      {
        if (!overrideExisting && !CliTools.ConfirmOverwrite(fileNameTranslation))
        {
          return;
        }
      }

      // do translation
      var translatedDict = new Dictionary<string, string>();
      foreach (var (key, val) in dict!)
      {
        string value = val;
        string? translatedKey = key;
        if (key != "Headline")
        {
          translatedKey = LookupString(_allTerms[language],  key);
          if (translatedKey == null)
            translatedKey = TranslateString(language, key);
        }

        List<string> tokensForLink = null;
        if (LinkCorrector.ContainsKey(value))
        {
          tokensForLink = LinkCorrector.ReplaceLinkWithToken(ref value);
        }
        string? translatedValue = LookupString(_allTerms[language], value);
        if (translatedValue == null)
          translatedValue = TranslateString(language, value);

        if (tokensForLink != null && tokensForLink.Any())
        {
          translatedValue = LinkCorrector.RestoreTranslatedLinkFromToken(translatedValue, tokensForLink, language);
        }

        if (translatedKey != null && translatedValue != null)
          translatedDict.Add(translatedKey, translatedValue);
      }

      var translatedJson = JsonSerializer.Serialize(translatedDict, new JsonSerializerOptions { WriteIndented = true });
      File.WriteAllText(fileNameTranslation, translatedJson);
    }

    static string? LookupString(Dictionary<string, string> allTerms, string key)
    {
      string? translatedKey = null;
      if (allTerms.ContainsKey(key))
      {
        translatedKey = allTerms[key];
      }

      return translatedKey;
    }

    static string? TranslateString(string lang, string txt)
    {
      try
      {
        string? translatedString = Deepl.TranslateAsync(txt, lang.ToUpperInvariant()).Result;
        if (translatedString != null)
          _allTerms[lang].Add(txt, translatedString);
        return translatedString;
      }
      catch (Exception ex)
      {
        Console.WriteLine(ex.ToString());
        Console.ReadLine();
      }
      return "";
    }
  }
}
