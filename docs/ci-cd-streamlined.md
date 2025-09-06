# Binnaculum CI/CD System - Streamlined Approach

## 🎯 Overview

The Binnaculum CI/CD system has been redesigned to provide **fast feedback** for developers while ensuring comprehensive quality through scheduled testing. The system prioritizes development velocity over exhaustive PR validation.

## ⚡ Quick Start

### For Pull Requests
- **Primary Workflow**: `PR Check` runs automatically on all PRs
- **Runtime**: ~5 minutes total
- **Focus**: Essential validation only (core logic + basic build)

### For Comprehensive Testing
- **Scheduled Workflows**: Run 3x per week automatically
- **Manual Triggers**: Available via workflow_dispatch
- **Runtime**: Extended (15-45 minutes)
- **Focus**: Full platform matrix and performance testing

## 🚀 Workflow Architecture

### 1. PR Check (`.github/workflows/pr-check.yml`)
**Triggers**: Every PR and push to main
**Timeout**: 10 minutes total
**Purpose**: Fast feedback loop

#### Essential Validation (5 min)
- ✅ .NET 9 setup
- ✅ Core project build
- ✅ Core.Tests build  
- ✅ Core logic tests (accepts 80/87 with MAUI failures)
- ✅ Test result reporting

#### Android Build Check (3 min)
- ✅ MAUI workload installation
- ✅ Android compilation test
- ⚠️ Non-blocking (warns only)

### 2. Auto-Merge (`.github/workflows/auto-merge.yml`)
**Triggers**: PR review approval
**Timeout**: 10 minutes  
**Purpose**: Enable automatic merge after approval and validation

#### Features
- Core business logic validation before enabling auto-merge
- Only activates on approved, non-draft PRs
- Ensures Core.Tests pass before merge

## 🧪 Expected Test Results

### Core Tests (81 total)
- **Headless Environment**: 81/81 pass ✅ (MAUI-dependent tests moved to Core.Platform.Tests)
- **With MAUI Runtime**: 81/81 pass ✅ (all tests pass consistently)

The CI system expects **81/81 tests to pass** as MAUI-dependent functionality has been separated into the Core.Platform.Tests project which runs separately when platform services are available.

## 🛠️ Developer Workflow

### For Regular Development
1. **Create feature branch**
2. **Make changes**
3. **Push to GitHub** → PR Check runs automatically (~5 min)
4. **Address failures** if any (core logic issues only)
5. **Merge when green** ✅

### For Local Testing
```bash
# Quick validation
./validate-local.sh

# Expected output:
✅ Core validation passed locally!
   Expected MAUI failures in headless environment are acceptable

🎉 Local validation completed successfully!
```

### For Comprehensive Testing
- **Automatic**: Scheduled workflows run 3x per week
- **Manual**: Trigger via GitHub Actions UI
  ```
  Actions → Device Tests → Run workflow
  Actions → Smart Test Selection → Run workflow
  ```

## 📊 Performance Targets

### PR Check Workflow
- **Essential Checks**: < 8 minutes
- **Android Build**: < 10 minutes  
- **Total Pipeline**: < 15 minutes

### Scheduled Workflows
- **Device Tests**: < 30 minutes per platform
- **Smart Tests**: < 45 minutes total
- **E2E Tests**: < 60 minutes

## ⚙️ Configuration

### Timeouts
All jobs have aggressive timeout controls to prevent runaway processes:
- PR checks: 10-15 minutes
- Platform builds: 30 minutes
- Comprehensive tests: 45-60 minutes

### Concurrency
- `cancel-in-progress: true` on all workflows
- Only one instance per ref (branch/PR)

### Error Handling
- **Core test failures**: Block merge
- **Build failures**: Warning only (non-blocking)
- **Platform-specific issues**: Continue with other platforms

## 🚀 Migration from Complex System

### What Changed
❌ **Removed workflows** (referenced missing projects):
- Device tests workflow
- E2E test infrastructure  
- Smart test selection
- Performance monitoring
- Flaky test detection
- Complex TestUtils infrastructure

✅ **Streamlined to essential workflows**:
- PR check (Core validation + basic Android build)
- Auto-merge (approval-based merge)

### Benefits
- **⚡ Faster PR feedback** (~5 minutes for essential checks)
- **🎯 Focused validation** (Core business logic + basic MAUI build)
- **🔧 Reliable CI/CD** (no missing project references)
- **💡 Clear expectations** (81/81 tests must pass)

## 🔧 Troubleshooting

### PR Check Failures

#### Core Test Failures
```
❌ Unexpected test results: X/81
Expected 81/81
```
**Solution**: Fix core business logic issues. These indicate real problems.

#### Android Build Failures
```
⚠️ Android build check failed
MAUI setup issues - check build logs
```
**Solution**: Check MAUI workload installation, but **PR can still be merged**.

### Scheduled Workflow Failures
Currently, no scheduled workflows are active. All validation happens in the PR workflow for fast feedback.

## 📝 Manual Testing

For comprehensive testing, use the local validation script:

```bash
# Core validation
./validate-local.sh

# MAUI Android (optional, requires workloads)
dotnet workload install maui-android
dotnet build src/UI/Binnaculum.csproj -f net9.0-android

# Platform-specific tests (requires platform workloads)  
dotnet test src/Tests/Core.Platform.Tests/Core.Platform.Tests.fsproj
```

## 💡 Best Practices

### For Contributors
1. **Focus on core logic** - PR checks validate what matters most
2. **Don't worry about platform builds** - warnings are informational
3. **Use local validation** - catch issues before pushing
4. **Monitor scheduled results** - address quality issues when convenient

### For Maintainers
1. **Review scheduled workflow results** weekly
2. **Address flaky test reports** monthly
3. **Update performance baselines** as needed
4. **Adjust platform matrix** based on usage

---

This streamlined CI/CD system balances **developer productivity** with **quality assurance**, ensuring that critical issues are caught quickly while comprehensive testing happens in the background without blocking development flow.