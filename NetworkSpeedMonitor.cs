using System.Net.NetworkInformation;

namespace NetworkSpeedMonitor;

static class NetworkSpeed
{
    // 按网卡ID缓存上次采样
    private static readonly Dictionary<string, (long sent, long received, DateTime time)> _cache = new();
    private static string? _activeInterfaceId;
    private static readonly object _lock = new();

    // 目标网卡名称
    private const string TargetInterfaceName = "以太网 2";

    public static (double downloadMbps, double uploadMbps) GetSpeed()
    {
        lock (_lock)
        {
            try
            {
                // 找到活动网卡
                var nic = ResolveActiveInterface();
                if (nic == null) return (0, 0);

                var stats = nic.GetIPv4Statistics();
                long sent = stats.BytesSent;
                long received = stats.BytesReceived;
                DateTime now = DateTime.UtcNow;

                string key = nic.Id;

                if (_cache.TryGetValue(key, out var prev) && prev.sent > 0)
                {
                    double seconds = (now - prev.time).TotalSeconds;
                    if (seconds < 0.1) seconds = 0.1;
                    double up = Math.Max(0, (sent - prev.sent) * 8.0 / seconds / 1_000_000);   // Mbps
                    double dl = Math.Max(0, (received - prev.received) * 8.0 / seconds / 1_000_000);
                    _cache[key] = (sent, received, now);
                    return (dl, up);
                }
                else
                {
                    _cache[key] = (sent, received, now);
                    return (0, 0); // 第一次采样，无变化
                }
            }
            catch
            {
                _activeInterfaceId = null;
                _cache.Clear();
                return (0, 0);
            }
        }
    }

    private static NetworkInterface? ResolveActiveInterface()
    {
        // 优先按名称精确匹配目标网卡，找不到则回退到优先有线、其次 WiFi
        var candidates = NetworkInterface.GetAllNetworkInterfaces()
            .Where(n => n.OperationalStatus == OperationalStatus.Up
                     && n.NetworkInterfaceType != NetworkInterfaceType.Loopback
                     && n.NetworkInterfaceType != NetworkInterfaceType.Tunnel
                     && n.Supports(NetworkInterfaceComponent.IPv4))
            .ToList();

        var best = candidates.FirstOrDefault(n => n.Name == TargetInterfaceName)
                ?? candidates
                    .OrderBy(n => n.NetworkInterfaceType == NetworkInterfaceType.Ethernet ? 0 : 1)
                    .FirstOrDefault();

        if (best != null)
            _activeInterfaceId = best.Id;

        return best;
    }
}
