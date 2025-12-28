using System.Net;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Common.Models;
using MaxMind.GeoIP2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ExoAuth.Infrastructure.Services;

/// <summary>
/// GeoIP service using MaxMind GeoLite2 database.
/// </summary>
public sealed class GeoIpService : IGeoIpService, IDisposable
{
    private readonly ILogger<GeoIpService> _logger;
    private readonly DatabaseReader? _reader;
    private readonly bool _isEnabled;

    public GeoIpService(IConfiguration configuration, ILogger<GeoIpService> logger)
    {
        _logger = logger;

        var databasePath = configuration.GetValue<string>("GeoIp:DatabasePath");
        _isEnabled = configuration.GetValue("GeoIp:Enabled", true);

        if (!_isEnabled)
        {
            _logger.LogInformation("GeoIP service is disabled");
            return;
        }

        if (string.IsNullOrEmpty(databasePath))
        {
            _logger.LogWarning("GeoIP database path is not configured");
            return;
        }

        try
        {
            if (File.Exists(databasePath))
            {
                _reader = new DatabaseReader(databasePath);
                _logger.LogInformation("GeoIP database loaded from {Path}", databasePath);
            }
            else
            {
                _logger.LogWarning("GeoIP database file not found at {Path}", databasePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load GeoIP database from {Path}", databasePath);
        }
    }

    public bool IsAvailable => _isEnabled && _reader is not null;

    public GeoLocation GetLocation(string? ipAddress)
    {
        if (string.IsNullOrEmpty(ipAddress) || _reader is null)
            return GeoLocation.Empty;

        // Skip private/local IP addresses
        if (IsPrivateIpAddress(ipAddress))
            return GeoLocation.Empty;

        try
        {
            if (!IPAddress.TryParse(ipAddress, out var ip))
                return GeoLocation.Empty;

            if (_reader.TryCity(ip, out var response) && response is not null)
            {
                return new GeoLocation(
                    Country: response.Country?.Name,
                    CountryCode: response.Country?.IsoCode,
                    City: response.City?.Name,
                    Latitude: response.Location?.Latitude,
                    Longitude: response.Location?.Longitude
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to get location for IP address {IpAddress}", ipAddress);
        }

        return GeoLocation.Empty;
    }

    private static bool IsPrivateIpAddress(string ipAddress)
    {
        if (!IPAddress.TryParse(ipAddress, out var ip))
            return true;

        // Check for loopback
        if (IPAddress.IsLoopback(ip))
            return true;

        var bytes = ip.GetAddressBytes();

        // IPv4 private ranges
        if (bytes.Length == 4)
        {
            // 10.0.0.0/8
            if (bytes[0] == 10)
                return true;

            // 172.16.0.0/12
            if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                return true;

            // 192.168.0.0/16
            if (bytes[0] == 192 && bytes[1] == 168)
                return true;

            // 169.254.0.0/16 (link-local)
            if (bytes[0] == 169 && bytes[1] == 254)
                return true;
        }

        // IPv6 private ranges
        if (bytes.Length == 16)
        {
            // fe80::/10 (link-local)
            if (bytes[0] == 0xfe && (bytes[1] & 0xc0) == 0x80)
                return true;

            // fc00::/7 (unique local)
            if ((bytes[0] & 0xfe) == 0xfc)
                return true;
        }

        return false;
    }

    public void Dispose()
    {
        _reader?.Dispose();
    }
}
