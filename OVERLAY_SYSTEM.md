# Overlay System Documentation

## Overview

The overlay system allows you to add annotations (lines, text labels, circles) to astronomy images without creating full-resolution overlay images. This dramatically reduces bandwidth usage (from several MB to just a few KB).

## File Format

Overlay data is stored as JSON files with the naming convention:
- `{imagename}.overlay.json` - Neutral/default overlay
- `{imagename}.overlay.{lang}.json` - Localized overlay (e.g., `image.overlay.de.json`)

**Example:** For image `Jupiter-Merkur-Widder.JPG`, create `Jupiter-Merkur-Widder.overlay.json`

## JSON Structure

```json
{
  "Lines": [
    {
      "X1": 25,
      "Y1": 30,
      "X2": 25,
      "Y2": 50,
      "Color": "#ffff00",
      "StrokeWidth": 2,
      "UsePercentage": true
    }
  ],
  "Texts": [
    {
      "X": 25,
      "Y": 25,
      "Text": "Jupiter",
      "TextKey": null,
      "Color": "#ffffff",
      "FontSize": 16,
      "FontFamily": "Arial, sans-serif",
      "UsePercentage": true
    }
  ],
  "Circles": [
    {
      "Cx": 50,
      "Cy": 50,
      "Radius": 5,
      "Color": "#ffff00",
      "StrokeWidth": 2,
      "Fill": false,
      "UsePercentage": true
    }
  ]
}
```

## Coordinate System

- **UsePercentage: true** - Coordinates are percentages (0-100) relative to image dimensions
  - X: 0 = left edge, 100 = right edge
  - Y: 0 = top edge, 100 = bottom edge
- **UsePercentage: false** - Coordinates are absolute pixels (requires knowing exact image dimensions)

**Recommendation:** Always use `UsePercentage: true` for responsive overlays that work at any image size.

## Element Types

### Lines
Draw lines between two points:
- `X1, Y1` - Start point
- `X2, Y2` - End point
- `Color` - Hex color (e.g., "#ffff00" for yellow)
- `StrokeWidth` - Line thickness in pixels
- `UsePercentage` - Whether coordinates are percentages or pixels

### Text Labels
Add text annotations:
- `X, Y` - Position of text center
- `Text` - The text to display (or use `TextKey` for localization)
- `TextKey` - Optional: Resource key for localized text (e.g., "StarName_Jupiter")
- `Color` - Text color
- `FontSize` - Font size in pixels
- `FontFamily` - Font family name
- `UsePercentage` - Whether coordinates are percentages or pixels

### Circles
Draw circles (useful for highlighting objects):
- `Cx, Cy` - Center point
- `Radius` - Circle radius
- `Color` - Stroke/fill color
- `StrokeWidth` - Line thickness
- `Fill` - If true, fills the circle; if false, only stroke
- `UsePercentage` - Whether coordinates are percentages or pixels

## Localization

To create localized overlays, use `TextKey` instead of hardcoded `Text`:

```json
{
  "Texts": [
    {
      "X": 25,
      "Y": 25,
      "Text": "",
      "TextKey": "StarName_Jupiter",
      "Color": "#ffffff",
      "FontSize": 16,
      "UsePercentage": true
    }
  ]
}
```

Then add the translation to your resource files:
- `SharedResource.en.resx`: "StarName_Jupiter" = "Jupiter"
- `SharedResource.de.resx`: "StarName_Jupiter" = "Jupiter"
- `SharedResource.fr.resx`: "StarName_Jupiter" = "Jupiter"
- `SharedResource.nl.resx`: "StarName_Jupiter" = "Jupiter"

## Example: Complete Overlay

```json
{
  "Lines": [
    {
      "X1": 25,
      "Y1": 30,
      "X2": 25,
      "Y2": 50,
      "Color": "#ffff00",
      "StrokeWidth": 2,
      "UsePercentage": true
    },
    {
      "X1": 75,
      "Y1": 40,
      "X2": 75,
      "Y2": 60,
      "Color": "#ffff00",
      "StrokeWidth": 2,
      "UsePercentage": true
    }
  ],
  "Texts": [
    {
      "X": 25,
      "Y": 25,
      "Text": "Jupiter",
      "Color": "#ffffff",
      "FontSize": 16,
      "UsePercentage": true
    },
    {
      "X": 75,
      "Y": 35,
      "Text": "Mercury",
      "Color": "#ffffff",
      "FontSize": 14,
      "UsePercentage": true
    }
  ],
  "Circles": []
}
```

## Migration from Old System

The system is **backward compatible**:
- Old `ovl_*.jpg` files continue to work
- New JSON overlays take precedence if both exist
- Gradually migrate overlays to JSON format

## Benefits

- **99% smaller file size**: JSON (~1-5 KB) vs JPG (several MB)
- **Scalable**: Vector graphics scale without quality loss
- **Localizable**: Text can be translated
- **Easy to edit**: Edit JSON instead of recreating images
- **Better performance**: No image swap, just show/hide SVG

## Tips

1. Use percentage-based coordinates for responsive overlays
2. Test overlays at different screen sizes
3. Use bright colors (yellow, white) for visibility on dark astronomy images
4. Keep text concise - long labels may overlap
5. Use circles to highlight specific objects or regions
