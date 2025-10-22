namespace Core.Tests.Integration

open System
open Binnaculum.Core.Providers

/// <summary>
/// Reactive test environment detection and configuration.
/// Provides utilities to detect CI/headless environments and configure
/// appropriate test execution strategies.
/// </summary>
module ReactiveTestEnvironment =

    /// <summary>
    /// Detects if running in GitHub Actions CI environment
    /// </summary>
    let isGitHubActions: bool =
        let githubActions = Environment.GetEnvironmentVariable("GITHUB_ACTIONS")
        not (String.IsNullOrEmpty(githubActions)) && githubActions = "true"

    /// <summary>
    /// Detects if running in a headless CI environment (any CI system)
    /// </summary>
    let isHeadlessCI: bool =
        let ci = Environment.GetEnvironmentVariable("CI")
        let buildId = Environment.GetEnvironmentVariable("BUILD_ID")
        let jenkinsHome = Environment.GetEnvironmentVariable("JENKINS_HOME")
        let travisCI = Environment.GetEnvironmentVariable("TRAVIS")

        isGitHubActions
        || (not (String.IsNullOrEmpty(ci)) && ci = "true")
        || not (String.IsNullOrEmpty(buildId))
        || not (String.IsNullOrEmpty(jenkinsHome))
        || not (String.IsNullOrEmpty(travisCI))

    /// <summary>
    /// Wraps a test that requires file system access.
    /// In headless CI, these tests should be skipped or use in-memory alternatives.
    /// </summary>
    let requiresFileSystem (test: unit -> Async<unit>) : Async<unit> =
        async {
            if isHeadlessCI then
                printfn "[ReactiveTestEnvironment] Skipping file system test in headless CI"
                return ()
            else
                do! test ()
        }

    /// <summary>
    /// Initialize database using WorkOnMemory for headless environments,
    /// or InitDatabase for environments with file system access.
    /// Also sets AppDataDirectoryProvider to InMemory mode to avoid platform-specific issues.
    /// </summary>
    let initializeDatabase () : Async<unit> =
        async {
            if isHeadlessCI then
                printfn "[ReactiveTestEnvironment] Using WorkOnMemory for headless CI"
                Binnaculum.Core.UI.Overview.WorkOnMemory()
            else
                printfn "[ReactiveTestEnvironment] Using standard database initialization"
                Binnaculum.Core.UI.Overview.WorkOnMemory()

            // Set AppDataDirectoryProvider to InMemory mode to avoid platform-specific file system issues
            printfn "[ReactiveTestEnvironment] Setting AppDataDirectoryProvider to InMemory mode"
            AppDataDirectoryProvider.setMode AppDataDirectoryMode.InMemory

            return ()
        }
