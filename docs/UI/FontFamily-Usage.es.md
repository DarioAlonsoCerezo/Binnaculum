# Uso de FontFamily en Binnaculum

Este documento proporciona una vista general de como se usan las familias tipograficas en los archivos XAML del proyecto Binnaculum.

## Configuracion de fuentes

### Registro de fuentes (MauiProgram.cs)

Las fuentes se registran en el archivo `MauiProgram.cs` durante la inicializacion de la app:

```csharp
.ConfigureFonts(fonts =>
{
    fonts.AddFont("OpenSans-Bold.ttf", "OpenSansBold");
    fonts.AddFont("OpenSans-Light.ttf", "OpenSansLight");
    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
})
```

### Recursos de fuentes (Fonts.xaml)

Las familias tipograficas se definen como recursos estaticos en `src/UI/Resources/Styles/Fonts.xaml`:

```xaml
<x:String x:Key="Regular">OpenSansRegular</x:String>
<x:String x:Key="Bold">OpenSansBold</x:String>
<x:String x:Key="Light">OpenSansLight</x:String>
<x:String x:Key="Semibold">OpenSansSemibold</x:String>
```

**Importante:** El diccionario de recursos `Fonts.xaml` **debe** fusionarse **primero** en `App.xaml` antes de cualquier otro diccionario que referencie estos recursos:

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

## Uso de FontFamily por archivo

### ? src/UI/Resources/Styles/LabelsStyles.xaml (18 usos)

Todas las familias tipograficas en este archivo usan correctamente `{StaticResource}`:

| Nombre de estilo | Linea | FontFamily |
|------------------|-------|------------|
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

### ? src/UI/Resources/Styles/Styles.xaml (9 usos - REQUIERE ACTUALIZACION)

Todas las familias tipograficas en este archivo estan **hardcoded** y deben cambiarse a `{StaticResource Regular}`:

| Tipo de control | Linea | Valor actual | Debe ser |
|-----------------|-------|--------------|----------|
| Button | 52 | "OpenSansRegular" | `{StaticResource Regular}` |
| DatePicker | 96 | "OpenSansRegular" | `{StaticResource Regular}` |
| Editor | 117 | "OpenSansRegular" | `{StaticResource Regular}` |
| Entry | 139 | "OpenSansRegular" | `{StaticResource Regular}` |
| Picker | 193 | "OpenSansRegular" | `{StaticResource Regular}` |
| RadioButton | 231 | "OpenSansRegular" | `{StaticResource Regular}` |
| SearchBar | 258 | "OpenSansRegular" | `{StaticResource Regular}` |
| SearchHandler | 281 | "OpenSansRegular" | `{StaticResource Regular}` |
| TimePicker | 361 | "OpenSansRegular" | `{StaticResource Regular}` |

## Estadisticas de resumen

| Archivo | Uso total de FontFamily | Usando StaticResource | Hardcoded |
|---------|--------------------------|-----------------------|-----------|
| **LabelsStyles.xaml** | 18 | ? 18 | 0 |
| **Styles.xaml** | 9 | 0 | ? 9 |
| **Total** | **27** | **18** | **9** |

## Buenas practicas

### ? HAZ

1. **Usa referencias StaticResource** para familias tipograficas:
   ```xaml
   <Setter Property="FontFamily" Value="{StaticResource Regular}" />
   ```

2. **Asegura que Fonts.xaml se fusione primero** en App.xaml

3. **Mantén los recursos de fuentes centralizados** en `Fonts.xaml`

4. **Usa claves descriptivas** (`Regular`, `Bold`, `Light`, `Semibold`) en lugar de nombres completos de fuentes

### ? NO HAGAS

1. **No hardcodees nombres de familias tipograficas**:
   ```xaml
   <!-- Mal -->
   <Setter Property="FontFamily" Value="OpenSansRegular" />
   
   <!-- Bien -->
   <Setter Property="FontFamily" Value="{StaticResource Regular}" />
   ```

2. **No definas fuentes inline** en estilos individuales

3. **No omitas definiciones de recursos**: referencia siempre desde Fonts.xaml

## Tareas pendientes

- [ ] Actualizar los 9 valores hardcoded de FontFamily en `Styles.xaml` a `{StaticResource Regular}`
- [ ] Verificar que las fuentes cargan correctamente en todas las plataformas (Android, iOS, Windows, MacCatalyst)
- [ ] Considerar pesos adicionales si se necesitan (p. ej., Medium, ExtraBold)

## Solucion de problemas

### Las fuentes no cargan en dispositivo o simulador

**Sintomas:** Se usan fuentes del sistema en lugar de OpenSans.

**Solucion:** Asegura que `Fonts.xaml` se fusione **antes** que otros diccionarios en `App.xaml`:

```xaml
<ResourceDictionary.MergedDictionaries>
    <ResourceDictionary Source="Resources/Styles/Fonts.xaml" /> <!-- Debe ir primero -->
    <ResourceDictionary Source="Resources/Styles/LabelsStyles.xaml" />
</ResourceDictionary.MergedDictionaries>
```

### Error de StaticResource no encontrado

**Sintomas:** Error de compilacion o excepcion en tiempo de ejecucion sobre StaticResource faltante.

**Causa:** Los recursos de fuentes se definen despues de ser referenciados.

**Solucion:** Mueve `Fonts.xaml` para que sea el primer diccionario fusionado en `App.xaml`.

## Archivos relacionados

- `src/UI/MauiProgram.cs` - Registro de fuentes
- `src/UI/Resources/Styles/Fonts.xaml` - Definicion de recursos de fuentes
- `src/UI/App.xaml` - Fusion de diccionarios de recursos
- `src/UI/Resources/Styles/LabelsStyles.xaml` - Estilos de etiquetas (uso correcto)
- `src/UI/Resources/Styles/Styles.xaml` - Estilos de controles (requiere actualizacion)
- `src/UI/Resources/Fonts/*.ttf` - Archivos de fuentes

---

**Ultima actualizacion:** 2024-11-22  
**Estado:** 9 referencias hardcoded en Styles.xaml deben convertirse a StaticResource
