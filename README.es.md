# Binnaculum
Aplicación basada en .NET MAUI para rastrear tus inversiones

# Tabla de Contenidos
- [Descripción General](#descripción-general)
- [Características](#características)
- [Arquitectura](#arquitectura)
- [Recursos](#recursos)
- [Documentacion](#documentacion)
- [Estilos XAML](#estilos-xaml)
- [Instalación](#instalación)
- [Uso](#uso)
- [Pruebas](#pruebas)
- [CI/CD](#cicd)
- [Contribuir](#contribuir)
- [Licencia](#licencia)

# Descripción General
Binnaculum es una aplicación completa multiplataforma de seguimiento de inversiones construida con .NET 9 y .NET MAUI. Proporciona gestión sofisticada de portafolios, monitoreo de cuentas bancarias y análisis financiero con una arquitectura moderna y reactiva.

Con Binnaculum, puedes:
- **Rastrear Portafolios de Inversión**: Monitorear cuentas de corredores, posiciones y métricas de rendimiento
- **Gestión de Cuentas Bancarias**: Rastrear saldos, calcular ganancias por intereses y monitorear el crecimiento de cuentas
- **Seguimiento de Dividendos**: Gestionar pagos de dividendos, implicaciones fiscales y calendarios de pagos
- **Soporte Multi-Moneda**: Manejar inversiones en diferentes monedas con conversión en tiempo real
- **Análisis Avanzado**: Sistema completo de snapshots para cálculos financieros y métricas de rendimiento
- **Integración con Calendario**: Rastrear fechas financieras importantes y eventos
- **Monitoreo de Rendimiento**: Capacidades integradas de benchmarking y pruebas de rendimiento

# Características

## 🏦 Seguimiento de Inversiones
- **Gestión de Cuentas de Corredores**: Operaciones CRUD completas para cuentas de inversión
- **Seguimiento de Posiciones**: Monitoreo en tiempo real de tenencias, precios y valoraciones
- **Historial de Transacciones**: Registro completo de compras, ventas, dividendos y otros movimientos
- **Métricas de Rendimiento**: Ganancias no realizadas, retornos acumulativos y análisis de portafolio

## 🏛️ Monitoreo de Cuentas Bancarias
- **Seguimiento de Saldos**: Monitoreo histórico de saldos a lo largo del tiempo
- **Cálculo de Intereses**: Cálculo automático de intereses ganados en monedas específicas
- **Análisis de Cuentas**: Seguimiento de crecimiento sin registro manual de transacciones
- **Soporte Multi-Cuenta**: Gestionar múltiples cuentas bancarias simultáneamente

## 💰 Gestión de Dividendos
- **Seguimiento de Dividendos**: Registrar y monitorear pagos de dividendos
- **Gestión Fiscal**: Rastrear impuestos sobre dividendos e implicaciones
- **Programación de Pagos**: Integración con calendario para fechas de pago de dividendos
- **Análisis Histórico**: Historial completo de pagos de dividendos

## 📊 Análisis Avanzado
- **Sistema de Snapshots**: Cálculos completos de snapshots financieros
- **Benchmarking de Rendimiento**: Pruebas de rendimiento integradas y optimización
- **Actualizaciones en Tiempo Real**: Actualizaciones de UI reactivas con DynamicData
- **Validación de Datos**: Validación robusta de cálculos financieros y corrección

## 🎨 Interfaz de Usuario
- **Multiplataforma**: Soporte nativo para Android, iOS, Windows y MacCatalyst
- **Diseño Moderno**: Interfaz limpia e intuitiva con controles personalizados
- **Diseño Responsivo**: UI adaptativa que funciona en todos los tamaños de pantalla
- **Temas Oscuro/Claro**: Soporte de temas con estilo consistente

# Arquitectura

## 🏗️ Stack Tecnológico
- **Frontend**: .NET MAUI con C# y XAML
- **Lógica Backend**: F# para cálculos financieros y lógica de negocio
- **Base de Datos**: SQLite con capa completa de acceso a datos
- **Programación Reactiva**: ReactiveUI y DynamicData para gestión de estado reactiva
- **Pruebas**: NUnit, xUnit y Appium para cobertura completa de pruebas

## 📁 Estructura del Proyecto
```
src/
├── Core/           # Lógica de negocio en F# y cálculos financieros
├── UI/             # Aplicación MAUI con C# y XAML
└── Tests/          # Suite completa de pruebas
    ├── Core.Tests/           # Pruebas unitarias en F#
    ├── Core.Platform.Tests/  # Pruebas específicas de plataforma
    ├── UITests/             # Pruebas de automatización de UI
    └── TestUtils/           # Utilidades y frameworks de pruebas
```

## 🔧 Tecnologías Clave
- **.NET 9**: Plataforma .NET más reciente con soporte MAUI
- **F#**: Programación funcional para cálculos financieros confiables
- **SQLite**: Base de datos local para persistencia de datos
- **ReactiveUI**: Utilidades de programación reactiva y enlace de datos
- **DynamicData**: Colecciones reactivas y gestión de datos
- **Community Toolkit**: Controles adicionales MAUI y utilidades

# Recursos
[.NET MAUI](https://github.com/dotnet/maui)

[Community Toolkit](https://github.com/CommunityToolkit/Maui)

[Plugin de Calendario](https://github.com/yurkinh/Plugin.Maui.Calendar)

[Diseño Figma](https://www.figma.com/design/ptAOT3MDa4D8TwaXkdpcFk/Binnaculum?node-id=0-1&p=f&t=MPdVDsxPwDnkYbNy-0)

Utilizando [Indiko Markdown Controls](https://github.com/0xc3u/Indiko.Maui.Controls.Markdown) para renderizar contenido Markdown

Utilizando iconos de [Ikonate Thin Interface Icons](https://www.svgrepo.com/collection/ikonate-thin-interface-icons/)

Utilizando banderas de [Country Flags](https://github.com/lipis/flag-icons)

Utilizando la fuente [Gravitas One](https://fonts.google.com/specimen/Gravitas+One?preview.text=binnaculum) para generar el icono

Puedes obtener iconos de tickers de este [Repositorio](https://github.com/davidepalazzo/ticker-logos)

# Documentacion
- [Resumen de UI](docs/UI/Overview.es.md)
- [Resumen de Core](docs/Core/Overview.es.md)
- [Resumen de pruebas](docs/Tests/Overview.es.md)

# Estilos XAML
Utilizo la extensión [XAML Styler 2022](https://marketplace.visualstudio.com/items?itemName=TeamXavalon.XAMLStyler2022) para asegurar un estilo XAML consistente en todo el proyecto. Las reglas de estilo están configuradas en el archivo `XAMLStylerConfiguration.json` ubicado en el proyecto.

# Instalación

## Prerrequisitos
- **SDK de .NET 9**: Descargar desde [Microsoft .NET](https://dotnet.microsoft.com/download/dotnet/9.0)
- **Workloads MAUI**: Instalar workloads MAUI para tus plataformas objetivo

## Pasos de Configuración
1. Clona el repositorio:
   ```bash
   git clone https://github.com/DarioAlonsoCerezo/Binnaculum.git
   cd Binnaculum
   ```

2. Instala el SDK de .NET 9:
   ```bash
   # En Windows/macOS
   winget install Microsoft.DotNet.SDK.9
   # O descargar desde https://dotnet.microsoft.com/download/dotnet/9.0
   ```

3. Instala los workloads MAUI:
   ```bash
   dotnet workload install maui-android
   # Para desarrollo en Windows (en Windows):
   dotnet workload install maui-windows
   # Para desarrollo en iOS/macOS (en macOS):
   dotnet workload install maui-ios
   dotnet workload install maui-maccatalyst
   ```

4. Restaura los paquetes NuGet:
   ```bash
   dotnet restore
   ```

5. Compila el proyecto:
   ```bash
   # Compilar para Android (funciona en todas las plataformas)
   dotnet build src/UI/Binnaculum.csproj -f net10.0-android

   # Compilar para Windows (solo Windows)
   dotnet build src/UI/Binnaculum.csproj -f net10.0-windows10.0.19041.0

   # Compilar para iOS (solo macOS)
   dotnet build src/UI/Binnaculum.csproj -f net10.0-ios
   ```

## Soporte de Plataformas
- **Android**: Disponible en Windows, macOS y Linux
- **Windows**: Disponible en Windows
- **iOS**: Disponible en macOS
- **Mac Catalyst**: Disponible en macOS

# Uso

## Primeros Pasos
1. Lanza la aplicación en tu dispositivo
2. La aplicación creará automáticamente la base de datos SQLite en la primera ejecución
3. Navega a través de las pestañas principales: Resumen, Tickers y Configuración

## Características Principales

### Pestaña de Resumen
- Ve el resumen de tu portafolio de inversiones
- Monitorea saldos de cuentas y rendimiento
- Accede a acciones rápidas para gestión de cuentas

### Pestaña de Tickers
- Explora y gestiona tickers de inversión
- Ve precios actuales y datos históricos
- Agrega nuevos tickers a tu lista de seguimiento

### Pestaña de Configuración
- Configura preferencias de moneda predeterminada
- Gestiona configuraciones de la aplicación
- Accede a funcionalidad de importación/exportación de datos

### Integración con Calendario
- Rastrea fechas de pago de dividendos
- Monitorea eventos financieros importantes
- Ve transacciones programadas

## Gestión de Datos
- **Importar/Exportar**: Portabilidad completa de datos con soporte JSON/CSV
- **Backup**: Backups automáticos locales de tus datos financieros
- **Sincronización**: Capacidades de sincronización entre dispositivos

# Pruebas

## Infraestructura de Pruebas
Binnaculum incluye cobertura completa de pruebas:

### Pruebas Unitarias
- **Lógica Core**: Pruebas unitarias en F# para cálculos financieros
- **Reglas de Negocio**: Validación de algoritmos de inversión
- **Acceso a Datos**: Operaciones SQLite e integridad de datos

### Pruebas de Integración
- **Pruebas de Plataforma**: Validación de compatibilidad multiplataforma
- **Pruebas de Base de Datos**: Persistencia de datos y pruebas de migración
- **Integración de API**: Pruebas de integración con servicios externos

### Pruebas de UI
- **Integración Appium**: Framework de pruebas automatizadas de UI
- **Inicio por Primera Vez**: Validación completa del flujo de onboarding
- **Viaje del Usuario**: Pruebas de extremo a extremo de experiencia de usuario

### Pruebas de Rendimiento
- **Benchmarking**: Monitoreo de rendimiento de cálculos financieros
- **Uso de Memoria**: Optimización para restricciones de dispositivos móviles
- **Pruebas de Carga**: Manejo eficiente de portafolios grandes

## Ejecutar Pruebas
```bash
# Ejecutar todas las pruebas
dotnet test

# Ejecutar categorías específicas de pruebas
dotnet test --filter "BrokerFinancialSnapshotManager"
dotnet test --filter "FirstTimeStartup"

# Ejecutar benchmarks de rendimiento
dotnet run --project src/Tests/Core.Tests/Core.Tests.fsproj -- --benchmark
```

# CI/CD

## Workflows de GitHub Actions
Binnaculum utiliza GitHub Actions para aseguramiento automatizado de calidad:

### Workflow de Verificación PR
- **Validación Esencial**: Retroalimentación rápida sobre cambios de código
- **Verificación de Compilación**: Asegura que todos los proyectos se compilen exitosamente
- **Ejecución de Pruebas Unitarias**: Valida lógica de negocio core
- **Calidad de Código**: Análisis automatizado de código y verificación de formato

### Pruebas de Integración de Plataforma
- **Pruebas Multiplataforma**: Valida funcionalidad en todas las plataformas objetivo
- **Integración de Base de Datos**: Asegura consistencia de datos entre plataformas
- **Automatización de UI**: Pruebas automatizadas de componentes de interfaz de usuario

### Workflow de Auto-Merge
- **Fusión Automatizada**: Agiliza el proceso de fusión para cambios aprobados
- **Compuertas de Calidad**: Asegura que todas las verificaciones pasen antes de fusionar
- **Protección de Rama**: Mantiene estándares de calidad de código

## Objetivos de Compilación
- **Android**: `net10.0-android` - Objetivo móvil principal
- **Windows**: `net10.0-windows10.0.19041.0` - Soporte de escritorio Windows
- **iOS**: `net10.0-ios` - Soporte para iPhone y iPad
- **Mac Catalyst**: `net10.0-maccatalyst` - Soporte de escritorio macOS

# Contribuir

## Configuración de Desarrollo
1. Haz un fork del repositorio
2. Crea una rama de característica: `git checkout -b feature/tu-nombre-caracteristica`
3. Realiza tus cambios siguiendo los patrones establecidos
4. Asegura que todas las pruebas pasen: `dotnet test`
5. Envía un pull request con una descripción detallada

## Estándares de Código
- **Código F#**: Sigue las mejores prácticas de programación funcional
- **Código C#**: Adhiérete a las directrices de codificación .NET
- **XAML**: Usa XAML Styler para formato consistente
- **Pruebas**: Escribe pruebas completas para nuevas características

## Directrices de Arquitectura
- **Separación de Preocupaciones**: Mantén UI, lógica de negocio y acceso a datos separados
- **Patrones Reactivos**: Usa ReactiveUI y DynamicData para gestión de estado reactiva
- **Rendimiento**: Optimiza para restricciones de dispositivos móviles
- **Multiplataforma**: Asegura comportamiento consistente en todas las plataformas

# Licencia
Este proyecto está licenciado bajo la Licencia MIT. Ver el archivo [LICENSE](LICENSE) para más detalles.