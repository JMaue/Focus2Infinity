using System.Runtime.CompilerServices;

// Runs when the assembly is loaded, before Main(). If you never see this in the temp log,
// the process is failing before our code runs (wrong runtime, host, or missing dependency).
internal static class BootstrapDiagnostics
{
    [ModuleInitializer]
    public static void Init()
    {
        try
        {
            var path = Path.Combine(Path.GetTempPath(), "Focus2Infinity-bootstrap.log");
            var line = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}Z [ModuleInit] Assembly loaded. BaseDirectory={AppDomain.CurrentDomain.BaseDirectory} ProcessId={Environment.ProcessId}";
            File.AppendAllText(path, line + Environment.NewLine);
        }
        catch
        {
            // No way to report; avoid throwing from module initializer
        }
    }
}
