namespace Core.Platform.Tests

open System
open NUnit.Framework

module PlatformTestEnvironment =
    
    /// Detect if we're running in a CI environment
    let isCI = Environment.GetEnvironmentVariable("CI") <> null
    
    /// Detect if we're running in a headless environment (no display)
    let isHeadless = 
        let display = Environment.GetEnvironmentVariable("DISPLAY")
        String.IsNullOrEmpty(display)
    
    /// Check if MAUI platform services are available
    let isMauiPlatformAvailable() =
        try
            // Try to access MAUI platform services - this will work on platform-specific targets
            #if ANDROID || IOS || MACCATALYST || WINDOWS
            Microsoft.Maui.ApplicationModel.Platform.CurrentActivity |> ignore
            #endif
            true
        with
        | ex when ex.GetType().Name = "NotImplementedInReferenceAssemblyException" -> false
        | _ -> false
    
    /// Skip test if MAUI platform is not available
    let requiresMauiPlatform (testAction: unit -> unit) =
        if isCI || isHeadless then
            Assert.Ignore("This test requires MAUI platform services and cannot run in headless CI environment")
        else
            try
                testAction()
            with
            | ex when ex.GetType().Name = "NotImplementedInReferenceAssemblyException" ->
                Assert.Ignore("MAUI platform services are not available in this test environment")
            | ex -> reraise()
    
    /// Initialize platform for testing (if needed)
    let initializePlatform() =
        try
            // Platform-specific initialization can be added here
            // For now, we'll just verify MAUI is available
            if not (isMauiPlatformAvailable()) then
                printfn "Warning: MAUI platform services may not be fully available"
        with
        | ex -> 
            printfn "Platform initialization warning: %s" ex.Message