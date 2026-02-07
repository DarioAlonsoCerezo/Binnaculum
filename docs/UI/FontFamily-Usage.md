# FontFamily Usage in Binnaculum

This document provides a comprehensive overview of how font families are used across the XAML files in the Binnaculum project.

## Font Configuration

### Font Registration (MauiProgram.cs)

Fonts are registered in the `MauiProgram.cs` file during app initialization:

```csharp
.ConfigureFonts(fonts =>
{
    fonts.AddFont("OpenSans-Bold.ttf", "OpenSansBold");
    fonts.AddFont("OpenSans-Light.ttf", "OpenSansLight");
    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
})
```

### Font Resources (Fonts.xaml)

Font families are defined as static resources in `src/UI/Resources/Styles/Fonts.xaml`:

```xaml
<x:String x:Key="Regular">OpenSansRegular</x:String>
<x:String x:Key="Bold">OpenSansBold</x:String>
<x:String x:Key="Light">OpenSansLight</x:String>
<x:String x:Key="Semibold">OpenSansSemibold</x:String>
```

**Important:** The `Fonts.xaml` resource dictionary **must** be merged **first** in `App.xaml` before any other style dictionaries that reference these font resources:

```xaml
<ResourceDictionary.MergedDictionaries>
    <ResourceDictionary Source="Resources/Styles/Fonts.xaml" />
    <ResourceDictionary Source="Resources/Styles/Colors.xaml" />
    <ResourceDictionary Source="Resources/Styles/Styles.xaml" />
    <ResourceDictionary Source="Resources/Styles/BorderedControlsStyles.xaml" />
    <ResourceDictionary Source="Resources/Styles/LabelsStyles.xaml" />
    <ResourceDictionary Source="Resources/Styles/ControlTemplates.xaml" />
</ResourceDictionary.MergedDictionaries>
```

## FontFamily Usage by File

### ? src/UI/Resources/Styles/LabelsStyles.xaml (18 usages)

All font families in this file correctly use `{StaticResource}`:

| Style Name | Line | FontFamily |
|------------|------|------------|
| BaseStyle | 13 | `{StaticResource Regular}` |
| Headline | 38 | `{StaticResource Bold}` |
| SubHeadline | 44 | `{StaticResource Bold}` |
| HeadlineSmall | 51 | `{StaticResource Regular}` |
| TitleLarge | 58 | `{StaticResource Regular}` |
| TitleMedium | 64 | `{StaticResource Regular}` |
| TitleSmall | 70 | `{StaticResource Regular}` |
| BodyLarge | 76 | `{StaticResource Regular}` |
| BodyMedium | 82 | `{StaticResource Regular}` |
| BodySmall | 88 | `{StaticResource Regular}` |
| LabelLarge | 94 | `{StaticResource Regular}` |
| LabelMedium | 100 | `{StaticResource Regular}` |
| LabelMediumSemibold | 106 | `{StaticResource Semibold}` |
| LabelSmall | 112 | `{StaticResource Regular}` |
| LabelSettingsGroup | 139 | `{StaticResource Semibold}` |
| LabelSettingsTitle | 147 | `{StaticResource Bold}` |
| SelectableItem | 162 | `{StaticResource Bold}` |
| IconText | 169 | `{StaticResource Bold}` |

### ? src/UI/Resources/Styles/Styles.xaml (9 usages - NEEDS UPDATE)

All font families in this file are **hardcoded** and should be changed to use `{StaticResource Regular}`:

| Control Type | Line | Current Value | Should Be |
|-------------|------|---------------|-----------|
| Button | 52 | `"OpenSansRegular"` | `{StaticResource Regular}` |
| DatePicker | 96 | `"OpenSansRegular"` | `{StaticResource Regular}` |
| Editor | 117 | `"OpenSansRegular"` | `{StaticResource Regular}` |
| Entry | 139 | `"OpenSansRegular"` | `{StaticResource Regular}` |
| Picker | 193 | `"OpenSansRegular"` | `{StaticResource Regular}` |
| RadioButton | 231 | `"OpenSansRegular"` | `{StaticResource Regular}` |
| SearchBar | 258 | `"OpenSansRegular"` | `{StaticResource Regular}` |
| SearchHandler | 281 | `"OpenSansRegular"` | `{StaticResource Regular}` |
| TimePicker | 361 | `"OpenSansRegular"` | `{StaticResource Regular}` |

## Summary Statistics

| File | Total FontFamily Usage | Using StaticResource | Hardcoded |
|------|------------------------|---------------------|-----------|
| **LabelsStyles.xaml** | 18 | ? 18 | 0 |
| **Styles.xaml** | 9 | 0 | ? 9 |
| **Total** | **27** | **18** | **9** |

## Best Practices

### ? DO

1. **Use StaticResource references** for font families:
   ```xaml
   <Setter Property="FontFamily" Value="{StaticResource Regular}" />
   ```

2. **Ensure Fonts.xaml is merged first** in App.xaml

3. **Keep font resources centralized** in `Fonts.xaml`

4. **Use descriptive keys** (`Regular`, `Bold`, `Light`, `Semibold`) instead of full font names

### ? DON'T

1. **Don't hardcode font family names**:
   ```xaml
   <!-- Bad -->
   <Setter Property="FontFamily" Value="OpenSansRegular" />
   
   <!-- Good -->
   <Setter Property="FontFamily" Value="{StaticResource Regular}" />
   ```

2. **Don't define fonts inline** in individual styles

3. **Don't skip font resource definitions** - always reference from Fonts.xaml

## Action Items

- [ ] Update all 9 hardcoded FontFamily values in `Styles.xaml` to use `{StaticResource Regular}`
- [ ] Verify fonts load correctly on all platforms (Android, iOS, Windows, MacCatalyst)
- [ ] Consider adding additional font weights if needed (e.g., Medium, ExtraBold)

## Troubleshooting

### Fonts not loading on device/simulator

**Symptoms:** Default system fonts are used instead of OpenSans fonts.

**Solution:** Ensure `Fonts.xaml` is merged **before** other resource dictionaries in `App.xaml`:

```xaml
<ResourceDictionary.MergedDictionaries>
    <ResourceDictionary Source="Resources/Styles/Fonts.xaml" /> <!-- Must be first -->
    <ResourceDictionary Source="Resources/Styles/LabelsStyles.xaml" />
</ResourceDictionary.MergedDictionaries>
```

### StaticResource not found error

**Symptoms:** Build error or runtime exception about missing StaticResource.

**Cause:** Font resources are defined after they are referenced.

**Solution:** Move `Fonts.xaml` to be the first merged dictionary in `App.xaml`.

## Related Files

- `src/UI/MauiProgram.cs` - Font registration
- `src/UI/Resources/Styles/Fonts.xaml` - Font resource definitions
- `src/UI/App.xaml` - Resource dictionary merging
- `src/UI/Resources/Styles/LabelsStyles.xaml` - Label styles (correct usage)
- `src/UI/Resources/Styles/Styles.xaml` - Control styles (needs update)
- `src/UI/Resources/Fonts/*.ttf` - Font files

---

**Last Updated:** 2024-11-22  
**Status:** 9 hardcoded references in Styles.xaml need to be converted to StaticResource
