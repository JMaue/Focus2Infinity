namespace Focus2Infinity.Data;

public class OverlayData
{
    public List<OverlayLine> Lines { get; set; } = new();
    public List<OverlayText> Texts { get; set; } = new();
    public List<OverlayCircle> Circles { get; set; } = new();
}

public class OverlayLine
{
    public double X1 { get; set; }  // Percentage (0-100) or absolute pixels
    public double Y1 { get; set; }
    public double X2 { get; set; }
    public double Y2 { get; set; }
    public string Color { get; set; } = "#ffff00";  // Yellow default
    public double StrokeWidth { get; set; } = 2;
    public bool UsePercentage { get; set; } = true;  // If false, use absolute pixels
}

public class OverlayText
{
    public double X { get; set; }  // Percentage (0-100) or absolute pixels
    public double Y { get; set; }
    public string Text { get; set; } = string.Empty;
    public string? TextKey { get; set; }  // For localization (e.g., "StarName")
    public string Color { get; set; } = "#ffffff";
    public int FontSize { get; set; } = 14;
    public string FontFamily { get; set; } = "Arial, sans-serif";
    public bool UsePercentage { get; set; } = true;
}

public class OverlayCircle
{
    public double Cx { get; set; }  // Center X
    public double Cy { get; set; }  // Center Y
    public double Radius { get; set; }
    public string Color { get; set; } = "#ffff00";
    public double StrokeWidth { get; set; } = 2;
    public bool Fill { get; set; } = false;
    public bool UsePercentage { get; set; } = true;
}
