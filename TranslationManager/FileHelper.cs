using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cli;

namespace TranslationManager
{
  internal class FileHelper
  {
    protected static string[] _supportedLanguages = new string[] {"en", "fr", "nl" };

    public static void ListAll()
    {
      var root = FindImgRoot();
      if (root is null)
      {
        Console.Error.WriteLine("wwwroot\\img not found. Pass its path as the first argument.");
        Environment.Exit(1);
      }

      var allFiles = Directory.EnumerateFiles(root, "*.json", SearchOption.AllDirectories).ToList();
      allFiles.RemoveAll(fn => isTranslatedFile(fn));
      var processedDirectories = new HashSet<string>();
      var missingTranslations = new List<string>();
      foreach (var origFile in allFiles)
      {
        var folder = Path.GetDirectoryName(origFile);
        var file = Path.GetFileName(origFile);
        if (!processedDirectories.Contains(folder))
        {
          Console.WriteLine();
          Console.WriteLine($"Directory: {folder}");
          processedDirectories.Add(folder);
        }
        Console.WriteLine($"\t{file}");

        // list the translated files:
        foreach (var lang in _supportedLanguages)
        {
          var translatedFile = Path.Combine(folder, Path.GetFileNameWithoutExtension(file) + $".{lang}.json");
          if (File.Exists(translatedFile))
            Console.WriteLine($"\t\t{Path.GetFileName(translatedFile)}");
          else
            missingTranslations.Add(translatedFile);
        }
      }
      Console.WriteLine();
      bool loop = true;    //stay in this submenu after an action is done
      do
      {
        CliTools.WriteTitle("Missing Translations");

        var menuItems = new List<MenuItem>();
        var idx = 1;
        foreach (var missingFile in missingTranslations)
        {
          menuItems.Add(new MenuItem($"{idx++}", missingFile, () => TranslationHelper.TranslateFile (missingFile, true) ));
        }
        menuItems.Add(new MenuItem("EsC", "Cancel", () => { loop = false; }));
        var selectedItem = CliTools.ShowMenu("Translate", 1,menuItems.ToArray());

      } while (loop);

      bool isTranslatedFile (string fn)
      {
        fn = fn.ToLower();
        foreach (var lang in _supportedLanguages)
        {
          if (fn.EndsWith($".{lang}.json"))
            return true;
        }
        return false;
      }
    }

    public static void ShowDeletePage()
    {
      //bool loop = false; //jump back to main menu after an action is done
      bool loop = true;    //stay in this submenu after an action is done
      do
      {
        Console.Clear();
        CliTools.WriteTitle("Delete exisiting Translations");

        var selectedItem = CliTools.ShowMenu("Delete", 1,
           new MenuItem("e", "EN", () => FileHelper.DeleteAll("en")),
           new MenuItem("f", "FR", () => FileHelper.DeleteAll("fr")),
           new MenuItem("n", "NL", () => FileHelper.DeleteAll("nl")),
           new MenuItem("EsC", "Cancel", () => { loop = false; })
         );

      } while (loop);
    }

    private static void DeleteAll(string lang)
    {
      if (!CliTools.ConfirmWith1OrBack($"Do you really want to delete all {lang} files?"))
      {
        return;
      }

      var rootPath = FindImgRoot();
      if (rootPath == null)
      {
        Console.Error.WriteLine("wwwroot\\img not found.");
        return;
      }

      var allFiles = Directory.EnumerateFiles(rootPath, $"*.{lang}.json", SearchOption.AllDirectories).ToList();
      foreach (var origFile in allFiles)
      {
        File.Delete(origFile);
        Console.WriteLine($"Deleted: {origFile}");
      }
    }

    #region File Operations
    public static string? FindImgRoot()
    {
      // Try common relative location from the console app's bin folder
      var candidate = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Focus2Infinity", "wwwroot", "img"));
      if (Directory.Exists(candidate))
        return candidate;

      // Walk up to find a wwwroot/img folder
      var dir = new DirectoryInfo(AppContext.BaseDirectory);
      while (dir is not null)
      {
        var tryPath = Path.Combine(dir.FullName, "wwwroot", "img");
        if (Directory.Exists(tryPath))
          return tryPath;
        dir = dir.Parent;
      }

      return null;
    }

    #endregion
  }
}
