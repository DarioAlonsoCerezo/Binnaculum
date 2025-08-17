# Copilot Instructions for Binnaculum

## Project Overview
- Binnaculum is a cross-platform investment tracking app built with .NET 9 and .NET MAUI.
- The solution includes F# and C# projects, focusing on investment, portfolio, and bank account management.

## Code Standards
- Use .NET 9 features and .NET MAUI best practices. Avoid Xamarin.Forms patterns unless a direct .NET MAUI equivalent is not available.
- Maintain consistent XAML styling using the XAML Styler configuration in the project.
- Use F# for core logic and C# for UI and platform-specific code.

## Development Workflow
- Always restore NuGet packages before building: `dotnet restore`
- Build the solution using Visual Studio or `dotnet build`.
- Run and test on all supported platforms (Android, iOS, MacCatalyst, Windows).
- Write and update unit tests in the Core.Tests project for any new or changed logic.

## Project Structure
- `src/Core/`: F# core logic, including database models and business rules.
- `src/UI/`: .NET MAUI UI project (multi-targeted for all supported platforms).
- `src/Tests/Core.Tests/`: F# unit tests for core logic.
- Resources (images, fonts, raw assets) are managed in the UI project under `Resources/`.

## Contribution Guidelines
- Follow idiomatic F# and C# patterns as appropriate for each project.
- Document public APIs and complex logic.
- Update or add tests for all new features and bug fixes.
- For UI changes, ensure XAML is styled according to the project’s configuration.
- Do not change the target framework unless explicitly required.

## Error Handling Guidelines
- **No Try-Catch in Core Logic**: Avoid using try-catch blocks, try-with expressions, or similar error handling constructs in F# core logic or data access layers.
- **Let Exceptions Bubble Up**: Allow exceptions to naturally propagate from the core to the UI layer where they can be properly handled.
- **Use CatchCoreError in UI**: In C# UI code, use the `CatchCoreError` extension method from `RxExtensions` to handle exceptions from core operations.
- **Fail Fast with failwith**: In F# code, use `failwith` or `failwithf` to signal errors with descriptive messages rather than returning option types for exceptional cases.
- **UI-Driven Error Display**: All error messages should be displayed through UI components (popups, alerts) rather than being swallowed or logged silently.
- **Debug vs Release Behavior**: Error popups are shown automatically in DEBUG mode but require explicit `informUser: true` parameter in Release mode.

## Additional Notes
- For database model changes, update only the relevant F# files in `src/Core/Database/`.
- Refer to the README for more details on project setup and resources.
