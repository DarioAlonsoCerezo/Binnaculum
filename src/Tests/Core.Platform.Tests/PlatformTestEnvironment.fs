namespace Core.Platform.Tests

open System
open NUnit.Framework

module PlatformTestEnvironment =
    
    /// Detect if we're running in GitHub Actions CI
    let isGitHubActions = Environment.GetEnvironmentVariable("GITHUB_ACTIONS") = "true"
    
    /// Detect if we're running in any CI environment  
    let isCI = 
        Environment.GetEnvironmentVariable("CI") <> null ||
        Environment.GetEnvironmentVariable("TF_BUILD") <> null ||
        isGitHubActions
    
    /// Detect if we're running in MAUI CI mode (special flag for CI testing)
    let isMauiCIMode = Environment.GetEnvironmentVariable("MAUI_CI_MODE") = "true"
    
    /// Detect if we're running in a headless environment (no display)
    let isHeadless = 
        let display = Environment.GetEnvironmentVariable("DISPLAY")
        String.IsNullOrEmpty(display)
    
    /// Enhanced MAUI platform availability check
    let isMauiPlatformAvailable() =
        try
            // For net9.0 target with MAUI.Essentials, try to access basic platform APIs
            // to determine if MAUI platform services are available
            let _ = Microsoft.Maui.ApplicationModel.MainThread.IsMainThread
            true
        with
        | :? System.NotImplementedException -> false
        | :? System.PlatformNotSupportedException -> false
        | ex when ex.GetType().Name = "NotImplementedInReferenceAssemblyException" -> false
        | _ -> false
    
    /// Initialize MAUI platform for CI testing
    let initializePlatformForCI() =
        try
            // Add CI-specific MAUI initialization here
            // This might include setting up mock platform services
            printfn "Initializing MAUI platform services for CI environment"
            // Try to access basic platform services to verify availability
            let _ = Microsoft.Maui.ApplicationModel.MainThread.IsMainThread
            printfn "Basic MAUI platform services are accessible"
        with
        | ex -> 
            printfn "Platform initialization failed in CI: %s" ex.Message
            printfn "Tests will run with reduced platform functionality"
    
    /// Skip test if MAUI platform is not available
    let requiresMauiPlatform (testAction: unit -> unit) =
        if isMauiCIMode then
            // In CI mode, try to initialize platform first
            initializePlatformForCI()
            
            try
                testAction()
            with
            | ex when ex.GetType().Name = "NotImplementedInReferenceAssemblyException" ->
                Assert.Ignore("MAUI platform services are not available in CI environment")
            | :? System.AggregateException as agEx when 
                agEx.InnerExceptions 
                |> Seq.exists (fun e -> e.GetType().Name = "NotImplementedInReferenceAssemblyException") ->
                Assert.Ignore("MAUI platform services are not available in CI environment (nested exception)")
            | ex -> 
                printfn "Test failed in CI mode: %s" ex.Message
                // In CI mode, convert failures to ignored tests for platform service issues
                if ex.Message.Contains("portable version") || 
                   ex.Message.Contains("NuGet package from your main application") ||
                   ex.Message.Contains("NotImplementedInReferenceAssemblyException") then
                    Assert.Ignore("MAUI platform service implementation not available in CI")
                else
                    reraise()
        elif isCI || isHeadless then
            Assert.Ignore("This test requires MAUI platform services and cannot run in headless CI environment")
        else
            try
                testAction()
            with
            | ex when ex.GetType().Name = "NotImplementedInReferenceAssemblyException" ->
                Assert.Ignore("MAUI platform services are not available in this test environment")
            | ex -> reraise()
    
    /// Initialize platform for testing (enhanced for CI)
    let initializePlatform() =
        try
            printfn "Environment check:"
            printfn "- isCI: %b" isCI
            printfn "- isGitHubActions: %b" isGitHubActions
            printfn "- isMauiCIMode: %b" isMauiCIMode
            printfn "- isHeadless: %b" isHeadless
            
            // Platform-specific initialization can be added here
            if isMauiCIMode then
                initializePlatformForCI()
            elif not (isMauiPlatformAvailable()) then
                printfn "Warning: MAUI platform services may not be fully available"
        with
        | ex -> 
            printfn "Platform initialization warning: %s" ex.Message