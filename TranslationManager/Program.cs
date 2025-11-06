using Cli;
using System.Reflection;

namespace TranslationManager
{
  internal class Program
  {
      /*  Parameters:
         1. DeleteAll : Delete all localized .(nl|fr|en).json Files
         2. DeleteLang: Delete all localized .json files of a given language
         3. Translate : Translate all missing .json files for all languages
         4. Check     : Check for completeness and list missing files    */


    private static void ShowMainPage()
    {
      var mainAsm = typeof(Program).Assembly;
      var appTitle = mainAsm?.GetCustomAttribute<AssemblyTitleAttribute>()?.Title ?? "<unknown>";
      var appVersion = mainAsm?.GetName().Version;
      Console.Title = appTitle;
      bool loop = true;
      do
      {
        try
        {
          Console.Clear();
          CliTools.WriteLine(CliTools.TitleColor, appTitle);
          Console.WriteLine($"{appVersion} {(Environment.Is64BitOperatingSystem ? "64Bit" : "32Bit")} running {(Environment.Is64BitProcess ? "64Bit" : "32Bit")} ");
          Console.WriteLine("---------------------------------------------------------------------------");

          var selectedItem = CliTools.ShowMenu("TestMenu", 1,
            new MenuItem("1", "ListAll", () => FileHelper.ListAll()),
            new MenuItem("2", "Translate", () => TranslationHelper.ShowTranslatePage() ),
            new MenuItem("3", "Delete", () => FileHelper.ShowDeletePage()),
            new MenuItem("EsC", "Cancel", () => { loop = false; })
          );
        }
        catch { }
      } while (loop);
    }


    static void Main(string[] args)
    {
      Console.WriteLine("Hello, Doing Translations for F2I");
      if (args != null && args.Length > 0)
      {
        TranslationHelper.Keyfile = args[0].ToLower();  
      }
      ShowMainPage();
    }
  }
}
