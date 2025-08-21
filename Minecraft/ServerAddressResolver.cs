using PCL.Core.Logging;

namespace PCL.Core.Net;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

public static class ServerAddressResolver {
    /// <summary>
    /// 解析服务器地址并获取其可达的IP和端口。
    /// </summary>
    /// <param name="address">服务器地址，可以是IP、IP:端口或域名。</param>
    /// <returns>包含IP和端口的元组。</returns>
    /// <exception cref="ArgumentException">地址为空或无效。</exception>
    /// <exception cref="FormatException">端口格式无效或SRV记录格式无效。</exception>
    public static async Task<(string Ip, int Port)> GetReachableAddressAsync(string address)
    {
        // 输入验证
        if (string.IsNullOrWhiteSpace(address))
        {
            throw new ArgumentException("服务器地址不能为空", nameof(address));
        }

        // 移除协议头（如果存在）
        address = address.Replace("http://", string.Empty).Replace("https://", string.Empty);

        // 情况1: IP:端口
        if (address.Contains(":"))
        {
            var parts = address.Split(':');
            if (parts.Length != 2 || !int.TryParse(parts[1], out int port))
            {
                throw new FormatException("无效的端口格式");
            }
            return (parts[0], port);
        }

        // 情况2: 纯IP (IPv4/IPv6)
        if (IPAddress.TryParse(address, out _))
        {
            return (address, 25565);
        }

        // 情况3: 域名 (尝试SRV查询)
        try
        {
            LogWrapper.Info($"尝试SRV查询: _minecraft._tcp.{address}");
            var srvRecords = await ResolveSrvRecordsAsync(address);

            if (srvRecords.Any())
            {
                var ret = ParseSrvRecord(srvRecords.First());
                return ret;
            }
        }
        catch (SocketException ex)
        {
            LogWrapper.Warn(ex, "SRV查询失败 (网络错误)");
        }
        catch (Exception ex)
        {
            LogWrapper.Warn(ex, "SRV查询异常");
        }

        // 默认: 直接使用域名+默认端口
        return (address, 25565);
    }

    private static async Task<IEnumerable<string>> ResolveSrvRecordsAsync(string domain)
    {
        return await Task.Run(() =>
        {
            try
            {
                return NDnsQuery.GetSRVRecords($"_minecraft._tcp.{domain}");
            }
            catch
            {
                return Enumerable.Empty<string>();
            }
        });
    }

    private static (string Host, int Port) ParseSrvRecord(string record)
    {
        // 标准SRV格式: 优先级 权重 端口 主机
        // 按原VB.NET逻辑，先尝试按空格分割
        var parts = record.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 4 && int.TryParse(parts[2], out int port))
        {
            return (parts[3], port);
        }

        // 回退到原VB.NET处理逻辑，按冒号分割
        parts = record.Split(':');
        if (parts.Length == 2 && int.TryParse(parts[1], out int fallbackPort))
        {
            return (parts[0], fallbackPort);
        }
        else if (parts.Length == 1)
        {
            return (parts[0], 25565);
        }
        else
        {
            throw new FormatException("无效的SRV记录格式");
        }
    }
}