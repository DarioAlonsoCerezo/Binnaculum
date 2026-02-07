# Pruebas de inicio por primera vez

## Descripcion general

Este documento describe la implementacion de pruebas de UI para validar la experiencia de primer inicio en Binnaculum. Las pruebas verifican la inicializacion de la base de datos, el comportamiento de indicadores de carga y las transiciones de estado de la UI durante el primer uso.

## Objetivos de prueba

### Objetivos principales
- **Validacion de creacion de base de datos**: verificar que SQLite se inicializa correctamente en el primer arranque
- **Monitoreo de indicadores de carga**: asegurar que `CarouseIndicator` y `CollectionIndicator` muestran estados correctos
- **Evidencia visual**: capturar capturas de pantalla con estados de carga y finalizacion
- **Validacion de rendimiento**: asegurar que la configuracion inicial termina dentro de limites razonables
- **Manejo de estado vacio**: verificar la UI cuando no hay datos

## Detalles de implementacion

### Archivos creados/modificados
- `src/Tests/TestUtils/UITest.Appium.Tests/FirstTimeStartupTests.cs` - implementacion principal
- `src/Tests/TestUtils/UITest.Appium.Tests/Screenshots/` - directorio de artefactos

### Metodos de prueba

#### 1. `FirstTimeAppStartup_DatabaseCreation_LoadsOverviewWithIndicators()`

Flujo de prueba:
1. Lanza la app con `AppResetStrategy.ReinstallApp` (estado limpio)
2. Espera a que la app este lista y cargue la pagina de Overview
3. Verifica que `CarouseIndicator` y `CollectionIndicator` estan visibles
4. Guarda captura del estado de carga (`first_startup_loading.png`)
5. Espera a que termine la creacion de base de datos (desaparecen los indicadores)
6. Guarda captura del estado final (`first_startup_loaded.png`)
7. Verifica que los elementos de Overview estan cargados

#### 2. `FirstTimeAppStartup_EmptyState_ShowsEmptyViews()`

Proposito:
- Verifica que la UI muestre estados vacios cuando no existen cuentas o movimientos
- Valida la integridad de la estructura UI en instalaciones nuevas

#### 3. `FirstTimeAppStartup_Performance_CompletesWithinTimeLimit()`

Validacion:
- Mide el tiempo total desde el inicio hasta la finalizacion
- Asegura que el primer inicio termina dentro de 60 segundos

## Implementacion tecnica

### Monitoreo de indicadores de carga

Indicadores definidos en `OverviewPage.xaml`:

```xml
<ActivityIndicator 
    x:Name="CarouseIndicator"
    Grid.Column="1" 
    IsRunning="True" />

<ActivityIndicator 
    x:Name="CollectionIndicator"
    Grid.Row="1" 
    IsRunning="True" />
```

### Logica de espera

```csharp
private void WaitForIndicatorsToDisappear(IUIElement carouseIndicator, IUIElement collectionIndicator, TimeSpan timeout)
{
    // Monitoriza ambos indicadores hasta que desaparezcan
}
```

### Captura de pantallas

```csharp
private static void SaveScreenshot(byte[] screenshot, string fileName)
{
    // Guarda capturas con timestamp en Screenshots/
}
```

### Degradacion controlada

```csharp
private static bool IsAppiumServerRunning(string statusUrl)
{
    // Verifica si Appium esta disponible
}
```

## Integracion con infraestructura existente

- Usa `AppiumConfig.ForBinnaculumAndroid()`
- Reutiliza `BinnaculumAppFactory`
- Implementa `IDisposable` para limpieza de recursos
- Utiliza atributos `[Fact]` de xUnit

## Instrucciones de uso

### Prerrequisitos
1. Appium Server: `appium --address 127.0.0.1 --port 4723 --relaxed-security`
2. Dispositivo o emulador Android accesible via ADB
3. APK de Binnaculum instalado o disponible

### Ejecutar pruebas

```bash
# Ejecutar todas las pruebas de primer inicio
dotnet test --filter FirstTimeStartup

# Ejecutar prueba especifica
dotnet test --filter FirstTimeAppStartup_DatabaseCreation_LoadsOverviewWithIndicators

# Ejecutar con logs detallados
dotnet test --filter FirstTimeStartup --verbosity normal
```

## Resultados esperados

- Sin Appium: las pruebas se omiten con un mensaje claro
- Con Appium + dispositivo: se ejecuta el flujo completo
- Capturas: guardadas en `src/Tests/TestUtils/UITest.Appium.Tests/Screenshots/`

## Consideraciones futuras

- Extender pruebas a iOS, Windows y MacCatalyst
- Comparacion automatica de capturas
- Baselines de rendimiento
- Verificacion de estado de base de datos
