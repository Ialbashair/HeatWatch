# HeatWatch
A minimalist, high-performance hardware monitor for Windows. Built with C# and LibreHardwareMonitor, focusing on a sleek UI, customizable data nodes, and zero bloat.

---

## CPU Temperatures Not Showing?

### Why this happens

To read CPU temperatures, HeatWatch uses a kernel-mode driver called **WinRing0** (provided by LibreHardwareMonitor). This driver needs low-level hardware access to read the CPU's internal temperature registers — the same approach used by HWiNFO, Core Temp, and other monitoring tools.

Windows Defender sometimes flags and blocks WinRing0 because the same driver has historically been abused by malware. This is a false positive — HeatWatch only uses it to read sensor data.

When the driver is blocked, CPU temperatures will be unavailable. GPU temperatures and all other sensors are unaffected, as they don't require the driver.

### How to fix it

1. Open **Windows Security**
2. Go to **Virus & threat protection** → **Protection history**
3. Find the blocked item related to `WinRing0` or `HeatWatch`
4. Click it and select **Allow**
5. Restart HeatWatch

HeatWatch will detect the fix automatically on next launch and the warning will disappear.

---
