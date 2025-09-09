# Simple validation script to test workflows locally
#!/bin/bash

set -e

echo "🧪 Testing Binnaculum CI/CD validation locally..."

# Test core functionality
echo "📦 Setting up .NET..."
if ! dotnet --version | grep -q "9.0"; then
  echo "❌ .NET 9 not found. Please install .NET 9 SDK"
  exit 1
fi

echo "🔧 Restoring Core dependencies..."
dotnet restore src/Core/Core.fsproj

echo "🏗️  Building Core project..."
dotnet build src/Core/Core.fsproj --configuration Release --no-restore

echo "🔧 Restoring Core.Tests dependencies..."
dotnet restore src/Tests/Core.Tests/Core.Tests.fsproj

echo "🏗️  Building Core.Tests..."
dotnet build src/Tests/Core.Tests/Core.Tests.fsproj --configuration Release --no-restore

echo "🧪 Running Core Tests..."
# Run tests - using exit code for validation (standard practice)
if dotnet test src/Tests/Core.Tests/Core.Tests.fsproj \
  --configuration Release --no-build --verbosity normal \
  --logger "trx;LogFileName=local-test-results.trx"; then
  
  echo "✅ Core validation passed locally!"
  echo "   All Core business logic tests passing"
  echo "   (MAUI-dependent tests are in separate Core.Platform.Tests project)"
else
  echo "❌ Core tests failed"
  echo "   Check test output above for details"
  exit 1
fi

# Quick MAUI workload check
echo ""
echo "📱 Checking MAUI setup..."
if dotnet workload list | grep -q "maui"; then
  echo "✅ MAUI workloads already installed"
else
  echo "⚠️  MAUI workloads not installed"
  echo "   Run: dotnet workload install maui-android"
  echo "   This is optional for core development"
fi

echo ""
echo "🎉 Local validation completed successfully!"
echo ""
echo "Next steps:"
echo "- Core business logic is working ✅"
echo "- Ready for PR submission" 
echo "- PR Check workflow will validate this automatically"
echo "- Comprehensive testing runs on schedule"