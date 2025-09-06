global using NUnit.Framework;
global using OpenQA.Selenium;
global using OpenQA.Selenium.Appium;
global using OpenQA.Selenium.Appium.Android;
global using System.Diagnostics;
global using System.Runtime.CompilerServices;
global using OpenQA.Selenium.Appium.Service;

// Assembly-level attributes to control test execution
[assembly: NonParallelizable] // Ensure UI tests don't run in parallel due to shared APK building and device usage[assembly: SetUpFixture(typeof(UI.Tests.TestSetupFixture))] // Global setup for all UI tests