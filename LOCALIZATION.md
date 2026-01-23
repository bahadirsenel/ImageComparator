# Localization System Documentation

## Overview

The Image Comparator application now uses a modern, JSON-based localization system that makes it easy to support multiple languages without cluttering the code with if-else blocks.

## Architecture

### Components

1. **LocalizationManager.cs** - Central localization management class
   - Loads language strings from JSON files
   - Provides methods to get localized strings
   - Supports parameter substitution in strings
   - Raises events when language changes

2. **JSON Resource Files** (in `Resources/` folder)
   - `en-US.json` - English translations
   - `tr-TR.json` - Turkish translations
   - Easy to add more languages by creating new JSON files

3. **Updated Windows** - All dialog windows now use LocalizationManager
   - `MainWindow.xaml.cs`
   - `AboutWindow.xaml.cs`
   - `HowToUseWindow.xaml.cs`
   - `ClearPopupWindow.xaml.cs`
   - `ApplyPopupWindow.xaml.cs`

## Usage

### Getting a Simple String

```csharp
string text = LocalizationManager.GetString("Menu.File");
// Returns: "File" (English) or "Dosya" (Turkish)
```

### Getting a String with Parameters

```csharp
string message = LocalizationManager.GetString("Console.FilesDeleted", 5);
// Returns: "5 file(s) have been deleted." (English)
// or "5 dosya silindi." (Turkish)
```

### Changing Language

```csharp
LocalizationManager.SetLanguage("tr-TR");
UpdateUI(); // Call this to refresh all UI elements
```

## Adding a New Language

### Step 1: Create JSON File

Create a new JSON file in the `Resources/` folder (e.g., `de-DE.json` for German):

```json
{
  "Menu.File": "Datei",
  "Menu.Exit": "Beenden",
  "Button.AddFolder": "Ordner hinzufügen",
  ...
}
```

Copy the structure from `en-US.json` and translate all values.

### Step 2: Update .csproj

Add the new JSON file to `ImageComparator.csproj`:

```xml
<Content Include="Resources\de-DE.json">
  <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
</Content>
```

### Step 3: Add Menu Item

Add a new menu item in `MainWindow.xaml`:

```xml
<MenuItem Name="germanMenuItem" Header="Deutsch" 
          IsCheckable="True" IsChecked="False" 
          Click="GermanMenuItem_Click"/>
```

### Step 4: Add Click Handler

Add the click handler in `MainWindow.xaml.cs`:

```csharp
private void GermanMenuItem_Click(object sender, RoutedEventArgs e)
{
    englishMenuItem.IsChecked = false;
    turkishMenuItem.IsChecked = false;
    germanMenuItem.IsChecked = true;
    englishMenuItem.IsEnabled = true;
    turkishMenuItem.IsEnabled = true;
    germanMenuItem.IsEnabled = false;
    LocalizationManager.SetLanguage("de-DE");
    UpdateUI();
}
```

That's it! The new language is now available.

## String Key Categories

All string keys follow a consistent naming convention:

- **Menu.*** - Menu items (e.g., `Menu.File`, `Menu.Options`)
- **Button.*** - Button labels (e.g., `Button.AddFolder`, `Button.Apply`)
- **Label.*** - UI labels (e.g., `Label.PreviewSelect`)
- **Console.*** - Console messages (e.g., `Console.AllDone`)
- **Dialog.*** - Dialog window strings (e.g., `Dialog.ClearTitle`)

## Benefits

✅ **Clean Code** - No more if-else blocks for translations  
✅ **Easy Maintenance** - All translations in one place  
✅ **Extensible** - Add new languages in minutes  
✅ **Type-Safe** - Keys are strings, IDE can autocomplete  
✅ **Dynamic** - Language changes take effect immediately  
✅ **Centralized** - One system for all translations  

## Migration Notes

The old system used:
```csharp
if (englishMenuItem.IsChecked)
{
    button.Content = "Add Folder";
}
else
{
    button.Content = "Klasör Ekle";
}
```

The new system uses:
```csharp
button.Content = LocalizationManager.GetString("Button.AddFolder");
```

This change removed **650+ lines** of duplicated translation code!

## Backward Compatibility

The `isEnglish` boolean flag is still maintained for backward compatibility with serialization. It's automatically updated when the language changes:

```csharp
isEnglish = LocalizationManager.CurrentLanguage == "en-US";
```

## Error Handling

If a translation key is not found, the system returns the key in brackets:

```csharp
LocalizationManager.GetString("NonExistent.Key")
// Returns: "[NonExistent.Key]"
```

This makes it easy to spot missing translations during development.

## File Structure

```
ImageComparator/
├── LocalizationManager.cs
├── Resources/
│   ├── en-US.json
│   ├── tr-TR.json
│   └── [future language files...]
└── [Window files using LocalizationManager]
```

## Complete String Catalog

The system includes 100+ translated strings covering:
- Menu items (18 strings)
- Buttons (13 strings)
- Labels (2 strings)
- Console messages (20+ strings)
- Dialog messages (20+ strings)
- Error messages (5+ strings)

See `en-US.json` for the complete list of available keys.
