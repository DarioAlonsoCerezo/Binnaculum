# Binnaculum
App based in .NET MAUI to track your investments

> **Español**: [README en Español](README.es.md)

# Table of Contents
- [Overview](#overview)
- [Features](#features)
- [Architecture](#architecture)
- [Resources](#resources)
- [XAML Styling](#xaml-styling)
- [Installation](#installation)
- [Usage](#usage)
- [Testing](#testing)
- [CI/CD](#cicd)
- [Contributing](#contributing)
- [License](#license)

# Overview
Binnaculum is a comprehensive cross-platform investment tracking application built with .NET 9 and .NET MAUI. It provides sophisticated portfolio management, bank account monitoring, and financial analytics with a modern, reactive architecture.

With Binnaculum, you can:
- **Track Investment Portfolios**: Monitor broker accounts, positions, and performance metrics
- **Bank Account Management**: Track balances, calculate interest earnings, and monitor account growth
- **Dividend Tracking**: Manage dividend payments, tax implications, and payout schedules
- **Multi-Currency Support**: Handle investments across different currencies with real-time conversion
- **Advanced Analytics**: Comprehensive snapshot system for financial calculations and performance metrics
- **Calendar Integration**: Track important financial dates and events
- **Performance Monitoring**: Built-in benchmarking and performance testing capabilities

# Features

## 🏦 Investment Tracking
- **Broker Account Management**: Full CRUD operations for investment accounts
- **Position Tracking**: Real-time monitoring of holdings, prices, and valuations
- **Transaction History**: Complete record of buys, sells, dividends, and other movements
- **Performance Metrics**: Unrealized gains, cumulative returns, and portfolio analytics

## 🏛️ Bank Account Monitoring
- **Balance Tracking**: Historical balance monitoring over time
- **Interest Calculation**: Automatic calculation of interest earned in specific currencies
- **Account Analytics**: Growth tracking without manual transaction logging
- **Multi-Account Support**: Manage multiple bank accounts simultaneously

## 💰 Dividend Management
- **Dividend Tracking**: Record and monitor dividend payments
- **Tax Management**: Track dividend taxes and implications
- **Payment Scheduling**: Calendar integration for dividend payout dates
- **Historical Analysis**: Complete dividend payment history

## 📊 Advanced Analytics
- **Snapshot System**: Comprehensive financial snapshot calculations
- **Performance Benchmarking**: Built-in performance testing and optimization
- **Real-time Updates**: Reactive UI updates with DynamicData
- **Data Validation**: Robust financial calculation validation and correction

## 🎨 User Interface
- **Cross-Platform**: Native Android, iOS, Windows, and MacCatalyst support
- **Modern Design**: Clean, intuitive interface with custom controls
- **Responsive Layout**: Adaptive UI that works across all screen sizes
- **Dark/Light Themes**: Theme support with consistent styling

# Architecture

## 🏗️ Technical Stack
- **Frontend**: .NET MAUI with C# and XAML
- **Backend Logic**: F# for financial calculations and business logic
- **Database**: SQLite with comprehensive data access layer
- **Reactive Programming**: ReactiveUI and DynamicData for reactive state management
- **Testing**: NUnit, xUnit, and Appium for comprehensive test coverage

## 📁 Project Structure
```
src/
├── Core/           # F# business logic and financial calculations
├── UI/             # MAUI application with C# and XAML
└── Tests/          # Comprehensive testing suite
    ├── Core.Tests/           # F# unit tests
    ├── Core.Platform.Tests/  # Platform-specific tests
    ├── UITests/             # UI automation tests
    └── TestUtils/           # Testing utilities and frameworks
```

## 🔧 Key Technologies
- **.NET 9**: Latest .NET platform with MAUI support
- **F#**: Functional programming for reliable financial calculations
- **SQLite**: Local database for data persistence
- **ReactiveUI**: Reactive programming and data binding utilities
- **DynamicData**: Reactive collections and data management
- **Community Toolkit**: Additional MAUI controls and utilities

# Resources
[.NET MAUI](https://github.com/dotnet/maui)

[Community Toolkit](https://github.com/CommunityToolkit/Maui)

[Calendar plugin](https://github.com/yurkinh/Plugin.Maui.Calendar)

[Figma Design](https://www.figma.com/design/ptAOT3MDa4D8TwaXkdpcFk/Binnaculum?node-id=0-1&p=f&t=MPdVDsxPwDnkYbNy-0)

Using [Indiko Markdown Controls](https://github.com/0xc3u/Indiko.Maui.Controls.Markdown) for rendering Markdown content

Using icons from [Ikonate Thin Interface Icons](https://www.svgrepo.com/collection/ikonate-thin-interface-icons/)

Using flags from [Country Flags](https://github.com/lipis/flag-icons)

Using font [Gravitas One](https://fonts.google.com/specimen/Gravitas+One?preview.text=binnaculum) to generate the icon

You can get ticker icons from this [Repository](https://github.com/davidepalazzo/ticker-logos)

# XAML Styling
I use the [XAML Styler 2022](https://marketplace.visualstudio.com/items?itemName=TeamXavalon.XAMLStyler2022) extension to ensure consistent XAML styling across the project. The styling rules are configured in the `XAMLStylerConfiguration.json` file located in the project.

# Installation

## Prerequisites
- **.NET 9 SDK**: Download from [Microsoft .NET](https://dotnet.microsoft.com/download/dotnet/9.0)
- **MAUI Workloads**: Install MAUI workloads for your target platforms

## Setup Steps
1. Clone the repository:
   ```bash
   git clone https://github.com/DarioAlonsoCerezo/Binnaculum.git
   cd Binnaculum
   ```

2. Install .NET 9 SDK:
   ```bash
   # On Windows/macOS
   winget install Microsoft.DotNet.SDK.9
   # Or download from https://dotnet.microsoft.com/download/dotnet/9.0
   ```

3. Install MAUI workloads:
   ```bash
   dotnet workload install maui-android
   # For Windows development (on Windows):
   dotnet workload install maui-windows
   # For iOS/macOS development (on macOS):
   dotnet workload install maui-ios
   dotnet workload install maui-maccatalyst
   ```

4. Restore NuGet packages:
   ```bash
   dotnet restore
   ```

5. Build the project:
   ```bash
   # Build for Android (works on all platforms)
   dotnet build src/UI/Binnaculum.csproj -f net10.0-android

   # Build for Windows (Windows only)
   dotnet build src/UI/Binnaculum.csproj -f net10.0-windows10.0.19041.0

   # Build for iOS (macOS only)
   dotnet build src/UI/Binnaculum.csproj -f net10.0-ios
   ```

## Platform Support
- **Android**: Available on Windows, macOS, and Linux
- **Windows**: Available on Windows
- **iOS**: Available on macOS
- **Mac Catalyst**: Available on macOS

# Usage

## Getting Started
1. Launch the application on your device
2. The app will automatically create the SQLite database on first run
3. Navigate through the main tabs: Overview, Tickers, and Settings

## Main Features

### Overview Tab
- View your investment portfolio summary
- Monitor account balances and performance
- Access quick actions for account management

### Tickers Tab
- Browse and manage investment tickers
- View current prices and historical data
- Add new tickers to your watchlist

### Settings Tab
- Configure default currency preferences
- Manage application settings
- Access data import/export functionality

### Calendar Integration
- Track dividend payment dates
- Monitor important financial events
- View scheduled transactions

## Data Management
- **Import/Export**: Full data portability with JSON/CSV support
- **Backup**: Automatic local backups of your financial data
- **Sync**: Cross-device synchronization capabilities

# Testing

## Test Infrastructure
Binnaculum includes comprehensive testing coverage:

### Unit Tests
- **Core Logic**: F# unit tests for financial calculations
- **Business Rules**: Validation of investment algorithms
- **Data Access**: SQLite operations and data integrity

### Integration Tests
- **Platform Tests**: Cross-platform compatibility validation
- **Database Tests**: Data persistence and migration testing
- **API Integration**: External service integration testing

### UI Tests
- **Appium Integration**: Automated UI testing framework
- **First-Time Startup**: Complete onboarding flow validation
- **User Journey**: End-to-end user experience testing

### Performance Tests
- **Benchmarking**: Financial calculation performance monitoring
- **Memory Usage**: Optimization for mobile device constraints
- **Load Testing**: Handling large portfolios efficiently

## Running Tests
```bash
# Run all tests
dotnet test

# Run specific test categories
dotnet test --filter "BrokerFinancialSnapshotManager"
dotnet test --filter "FirstTimeStartup"

# Run performance benchmarks
dotnet run --project src/Tests/Core.Tests/Core.Tests.fsproj -- --benchmark
```

# CI/CD

## GitHub Actions Workflows
Binnaculum uses GitHub Actions for automated quality assurance:

### PR Check Workflow
- **Essential Validation**: Fast feedback on code changes
- **Build Verification**: Ensures all projects compile successfully
- **Unit Test Execution**: Validates core business logic
- **Code Quality**: Automated code analysis and formatting checks

### Platform Integration Tests
- **Cross-Platform Testing**: Validates functionality across all target platforms
- **Database Integration**: Ensures data consistency across platforms
- **UI Automation**: Automated testing of user interface components

### Auto-Merge Workflow
- **Automated Merging**: Streamlines the merge process for approved changes
- **Quality Gates**: Ensures all checks pass before merging
- **Branch Protection**: Maintains code quality standards

## Build Targets
- **Android**: `net10.0-android` - Primary mobile target
- **Windows**: `net10.0-windows10.0.19041.0` - Desktop Windows support
- **iOS**: `net10.0-ios` - iPhone and iPad support
- **Mac Catalyst**: `net10.0-maccatalyst` - macOS desktop support

# Contributing

## Development Setup
1. Fork the repository
2. Create a feature branch: `git checkout -b feature/your-feature-name`
3. Make your changes following the established patterns
4. Ensure all tests pass: `dotnet test`
5. Submit a pull request with a detailed description

## Code Standards
- **F# Code**: Follow functional programming best practices
- **C# Code**: Adhere to .NET coding guidelines
- **XAML**: Use XAML Styler for consistent formatting
- **Testing**: Write comprehensive tests for new features

## Architecture Guidelines
- **Separation of Concerns**: Keep UI, business logic, and data access separate
- **Reactive Patterns**: Use ReactiveUI and DynamicData for reactive state management
- **Performance**: Optimize for mobile device constraints
- **Cross-Platform**: Ensure consistent behavior across all platforms

# License
This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.
