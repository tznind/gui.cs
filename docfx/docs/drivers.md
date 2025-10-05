# Cross-Platform Driver Model

## Overview

The driver model is the mechanism by which Terminal.Gui can support multiple platforms. Windows, Mac, Linux, and even (eventually) web browsers are supported.

## Drivers

Terminal.Gui provides three console drivers optimized for different scenarios:

- **DotNetDriver (`dotnet`)** - A cross-platform driver that uses the .NET `System.Console` API. Works on all platforms (Windows, macOS, Linux).
- **WindowsDriver (`windows`)** - A Windows-optimized driver that uses Windows Console APIs for better performance and features on Windows.
- **UnixDriver (`unix`)** - A Unix-optimized driver for macOS and Linux systems.

The appropriate driver is automatically selected based on the platform. You can also explicitly specify a driver using `Application.ForceDriver` or by passing the driver name to `Application.Init()`.

Example:
```csharp
// Let Terminal.Gui choose the best driver for the platform
Application.Init();

// Or explicitly specify a driver
Application.ForceDriver = "dotnet";
Application.Init();
```
