# HeatWatch

A minimalist, always-on-top hardware monitor for Windows. HeatWatch sits in the corner of your screen and shows you exactly the sensor data you care about, nothing more. No charts, no tabs, no bloat. Just clean, real-time readings in a compact dark-themed overlay.

## Concept

Most hardware monitors are built around exploration, you open them when something feels wrong and dig through dozens of tabs to find what you're looking for. HeatWatch is built for the opposite use case: persistent, ambient awareness. You configure it once, pin it to your screen, and it stays out of the way while always being readable at a glance.

The core idea is the **node**. Each node is a single sensor reading, a temperature, a clock speed, a fan RPM, displayed as a compact card with a color-coded accent strip. You choose which nodes are visible and in what order. Nothing is shown unless you ask for it.

## Features

- **Real-time sensor polling** at 1-second intervals
- **Fully customizable node list**, enable, disable, reorder, and rename any sensor
- **Color-coded temperature indicators**, green, yellow, orange, and red accent strips that reflect heat at a glance
- **Always-on-top toggle**, pin the window above all other applications
- **Persistent window position**, remembers where you left it across launches
- **Smart first-run defaults**, automatically enables your CPU package temp and GPU core temp on first launch
- **Drag to reposition**, click and drag the title bar anywhere on screen

## Supported Sensors

| Type | Unit | Example |
|------|------|---------|
| Temperature | °C | CPU Package, GPU Core |
| Clock Speed | MHz / GHz | CPU Core #1, GPU Core |
| Load | % | CPU Total, GPU Core |
| Power | W | GPU Package |
| Fan Speed | RPM | GPU Fan 1 |
| Memory | GB / MB | Memory Used, GPU Memory Used |

## Built With

- **[.NET 8](https://dotnet.microsoft.com/)**, runtime and application framework
- **[WPF](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/)**, UI framework with full XAML styling
- **[LibreHardwareMonitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor)**, open-source hardware sensor library that reads CPU, GPU, motherboard, and memory data via Windows APIs and kernel-level drivers
- **[CommunityToolkit.Mvvm](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/)**, source-generated MVVM bindings and observable properties

---

## CPU Temperatures Not Showing?

### Why this happens

To read CPU temperatures, HeatWatch uses a kernel-mode driver called **WinRing0** (provided by LibreHardwareMonitor). This driver needs low-level hardware access to read the CPU's internal temperature registers, the same approach used by HWiNFO, Core Temp, and other monitoring tools.

Windows Defender sometimes flags and blocks WinRing0 because the same driver has historically been abused by malware. This is a false positive, HeatWatch only uses it to read sensor data.

When the driver is blocked, CPU temperatures will be unavailable. GPU temperatures and all other sensors are unaffected, as they don't require the driver.

### How to fix it

1. Open **Windows Security**
2. Go to **Virus & threat protection** → **Protection history**
3. Find the blocked item related to `WinRing0` or `HeatWatch`
4. Click it and select **Allow**
5. Restart HeatWatch

HeatWatch will detect the fix automatically on next launch and the warning will disappear.

---
