# Appium Server Management - Working Example

This demonstrates the automatic Appium server management implementation.

## How it works

The implementation provides:

1. **AppiumServerManager** - Process-based server lifecycle management
2. **AppiumServerFixture** - xUnit collection fixture for shared server instances
3. **BinnaculumAppFactory** - Enhanced factory with automatic server detection

## Manual test

To test the server management manually:

```csharp
var serverManager = new AppiumServerManager();

// Start server (finds available port automatically)
var started = await serverManager.StartServerAsync(TimeSpan.FromMinutes(1));
Console.WriteLine($"Server started: {started}");
Console.WriteLine($"Server URL: {serverManager.ServerUrl}");

// Check health
var healthy = await serverManager.IsHealthyAsync();
Console.WriteLine($"Server healthy: {healthy}");

// Stop server
serverManager.StopServer();
```

## Test Integration

Tests now use:

```csharp
[Collection("AppiumServer")]
public class InvestmentWorkflowTests : IDisposable
{
    private readonly AppiumServerFixture _serverFixture;
    
    public InvestmentWorkflowTests(AppiumServerFixture serverFixture)
    {
        _serverFixture = serverFixture;
        
        try 
        {
            _app = BinnaculumAppFactory.CreateAppWithAutoServer(_config, _serverFixture);
        }
        catch (Exception ex)
        {
            Skip.If(true, $"Appium server not available: {ex.Message}");
            _app = null!;
        }
    }
    
    [Fact] 
    public void SomeTest()
    {
        // No more Skip.If calls needed - handled at collection level
        // Server is automatically managed
    }
}
```

## Configuration

Can be configured via environment variables:

```bash
export APPIUM_HOST=127.0.0.1
export APPIUM_PORT=4723
dotnet test
```

## Benefits

- ✅ No manual `appium` command needed
- ✅ Automatic port detection
- ✅ Health monitoring
- ✅ Proper cleanup (no zombie processes)
- ✅ CI/CD ready
- ✅ Cross-platform support