# Legal Content Setup Instructions

## Overview
The Impressum and Datenschutz pages load their content from external HTML files that are **NOT** committed to GitHub for privacy reasons.

## Setup Steps

### 1. Create Actual Content Files

Navigate to the `Focus2Infinity/LegalContent/` folder and create two files from the templates:

```bash
cd Focus2Infinity/LegalContent/

# Copy template files
cp impressum.html.template impressum.html
cp datenschutz.html.template datenschutz.html
```

### 2. Edit impressum.html

Replace the placeholders with your actual information:

- `[Your Street Address]` ? Your actual street address
- `[Your Postal Code] [Your City]` ? Your postal code and city
- `[Your Address]` ? Same address again

### 3. Edit datenschutz.html (Optional)

The Datenschutz content is already complete, but you can customize if needed.

### 4. File Locations

**Development (local):**
```
Focus2Infinity/LegalContent/
??? impressum.html          ? NOT in Git (your actual data)
??? impressum.html.template ? IN Git (template)
??? datenschutz.html        ? NOT in Git (your actual data)
??? datenschutz.html.template ? IN Git (template)
```

**Production (deployed):**
```
[AppBaseDirectory]/LegalContent/
??? impressum.html
??? datenschutz.html
```

### 5. Deployment

When deploying to production, manually copy the `.html` files to the server:

**Option A: Manual Copy**
```bash
# On production server
mkdir -p /path/to/app/LegalContent/
cp impressum.html /path/to/app/LegalContent/
cp datenschutz.html /path/to/app/LegalContent/
```

**Option B: Publish Profile**
Add to your `.pubxml` file to include LegalContent folder:
```xml
<ItemGroup>
  <Content Include="LegalContent\*.html">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </Content>
</ItemGroup>
```

### 6. Verify Setup

1. Start the application
2. Navigate to `/impressum`
3. Verify your address shows (not placeholders)
4. Navigate to `/datenschutz`
5. Verify content loads correctly

## How It Works

### Architecture

```
User ? /impressum
       ?
Impressum.razor
       ?
LegalContentService.GetImpressumContent()
       ?
Reads: [AppBaseDirectory]/LegalContent/impressum.html
       ?
Returns HTML content
       ?
Renders via @((MarkupString)_content)
```

### Service Registration

In `Program.cs`:
```csharp
builder.Services.AddSingleton<ILegalContentService, LegalContentService>();
```

### File Reading

The service reads from:
```csharp
Path.Combine(AppContext.BaseDirectory, "LegalContent", "impressum.html")
```

## Error Handling

If files are missing:
- ? Service logs warning
- ? Returns user-friendly error message
- ? Application doesn't crash

## Security Benefits

? **Personal data not in Git** - Your address stays private  
? **Easy updates** - Edit HTML file without code changes  
? **No recompilation** - Change content without rebuilding  
? **Flexible** - Can use different content per environment  

## Troubleshooting

### "Content file not found" error

**Cause:** Files don't exist in the expected location

**Fix:**
```bash
# Check if files exist
ls Focus2Infinity/LegalContent/

# Should show:
# impressum.html
# datenschutz.html
# impressum.html.template
# datenschutz.html.template
```

### Files exist but content not loading

**Cause:** Wrong base directory

**Check logs:**
```
[WRN] Legal content file not found: /path/to/LegalContent/impressum.html
```

The path shows where the app is looking. Ensure files are there.

### Content shows placeholders

**Cause:** You're viewing the template, not the actual file

**Fix:** Edit `impressum.html` (not `.template`)

## Git Status

These files are gitignored:
```bash
git status

# Should NOT show:
# Focus2Infinity/LegalContent/impressum.html
# Focus2Infinity/LegalContent/datenschutz.html

# SHOULD show (if modified):
# Focus2Infinity/LegalContent/impressum.html.template
# Focus2Infinity/LegalContent/datenschutz.html.template
```

---

**Last Updated:** 2024-01-15  
**Maintained by:** Focus2Infinity Team
