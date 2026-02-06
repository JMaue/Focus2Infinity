namespace Focus2Infinity.Options;

/// <summary>
/// When false, the Overlay Editor is hidden and /editor is unavailable (e.g. for deployed production).
/// When true, the editor is available (e.g. for local use).
/// </summary>
public class OverlayEditorOptions
{
    public const string SectionName = "OverlayEditor";

    public bool Enabled { get; set; } = true;
}
