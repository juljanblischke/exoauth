using System.Net;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Common.Models;
using ExoAuth.Domain.Entities;
using ExoAuth.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ExoAuth.Infrastructure.Services;

/// <summary>
/// Service for managing IP whitelist/blacklist restrictions with CIDR support.
/// </summary>
public sealed class IpRestrictionService : IIpRestrictionService
{
    private readonly IAppDbContext _dbContext;
    private readonly ICacheService _cache;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly RateLimitSettings _settings;
    private readonly ILogger<IpRestrictionService> _logger;

    private const string WhitelistCacheKey = "ip:whitelist";
    private const string BlacklistCacheKey = "ip:blacklist";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public IpRestrictionService(
        IAppDbContext dbContext,
        ICacheService cache,
        IDateTimeProvider dateTimeProvider,
        IOptions<RateLimitSettings> settings,
        ILogger<IpRestrictionService> logger)
    {
        _dbContext = dbContext;
        _cache = cache;
        _dateTimeProvider = dateTimeProvider;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IpRestrictionCheckResult> CheckIpAsync(string ipAddress, CancellationToken cancellationToken = default)
    {
        if (!IPAddress.TryParse(ipAddress, out var parsedIp))
        {
            _logger.LogWarning("Invalid IP address format: {IpAddress}", ipAddress);
            return new IpRestrictionCheckResult(false, false, null, null);
        }

        var now = _dateTimeProvider.UtcNow;

        // Check blacklist first (blacklist takes priority)
        var blacklistEntries = await GetCachedRestrictionsAsync(IpRestrictionType.Blacklist, cancellationToken);
        foreach (var entry in blacklistEntries)
        {
            if (!entry.IsActive(now))
                continue;

            if (IpMatchesCidr(parsedIp, entry.IpAddress))
            {
                _logger.LogInformation("IP {IpAddress} is blacklisted: {Reason}", ipAddress, entry.Reason);
                return new IpRestrictionCheckResult(true, false, entry.Reason, entry.ExpiresAt);
            }
        }

        // Check whitelist
        var whitelistEntries = await GetCachedRestrictionsAsync(IpRestrictionType.Whitelist, cancellationToken);
        foreach (var entry in whitelistEntries)
        {
            if (!entry.IsActive(now))
                continue;

            if (IpMatchesCidr(parsedIp, entry.IpAddress))
            {
                _logger.LogDebug("IP {IpAddress} is whitelisted: {Reason}", ipAddress, entry.Reason);
                return new IpRestrictionCheckResult(false, true, entry.Reason, entry.ExpiresAt);
            }
        }

        return new IpRestrictionCheckResult(false, false, null, null);
    }

    /// <inheritdoc />
    public async Task AutoBlacklistAsync(
        string ipAddress,
        string reason,
        int durationMinutes,
        CancellationToken cancellationToken = default)
    {
        var now = _dateTimeProvider.UtcNow;
        var expiresAt = now.AddMinutes(durationMinutes);

        // Check if IP is already blacklisted
        var existing = await _dbContext.IpRestrictions
            .FirstOrDefaultAsync(x =>
                x.IpAddress == ipAddress &&
                x.Type == IpRestrictionType.Blacklist &&
                (x.ExpiresAt == null || x.ExpiresAt > now),
                cancellationToken);

        if (existing != null)
        {
            _logger.LogDebug("IP {IpAddress} is already blacklisted", ipAddress);
            return;
        }

        var restriction = IpRestriction.CreateAuto(ipAddress, IpRestrictionType.Blacklist, reason, expiresAt);

        _dbContext.IpRestrictions.Add(restriction);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogWarning(
            "Auto-blacklisted IP {IpAddress} until {ExpiresAt}: {Reason}",
            ipAddress, expiresAt, reason);

        // Invalidate cache
        await InvalidateCacheAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task InvalidateCacheAsync(CancellationToken cancellationToken = default)
    {
        await _cache.RemoveAsync(WhitelistCacheKey, cancellationToken);
        await _cache.RemoveAsync(BlacklistCacheKey, cancellationToken);
        _logger.LogDebug("IP restriction cache invalidated");
    }

    /// <summary>
    /// Gets cached IP restrictions of the specified type.
    /// </summary>
    private async Task<List<CachedIpRestriction>> GetCachedRestrictionsAsync(
        IpRestrictionType type,
        CancellationToken cancellationToken)
    {
        var cacheKey = type == IpRestrictionType.Whitelist ? WhitelistCacheKey : BlacklistCacheKey;

        var cached = await _cache.GetAsync<List<CachedIpRestriction>>(cacheKey, cancellationToken);
        if (cached != null)
        {
            return cached;
        }

        // Load from database
        var restrictions = await _dbContext.IpRestrictions
            .Where(x => x.Type == type)
            .Select(x => new CachedIpRestriction(
                x.IpAddress,
                x.Reason,
                x.ExpiresAt))
            .ToListAsync(cancellationToken);

        await _cache.SetAsync(cacheKey, restrictions, CacheDuration, cancellationToken);

        return restrictions;
    }

    /// <summary>
    /// Checks if an IP address matches a CIDR notation or exact IP.
    /// </summary>
    private static bool IpMatchesCidr(IPAddress ip, string cidrOrIp)
    {
        // Check for CIDR notation
        if (cidrOrIp.Contains('/'))
        {
            var parts = cidrOrIp.Split('/');
            if (parts.Length != 2 || !IPAddress.TryParse(parts[0], out var networkAddress) || !int.TryParse(parts[1], out var prefixLength))
            {
                return false;
            }

            return IsInSubnet(ip, networkAddress, prefixLength);
        }

        // Exact IP match
        if (IPAddress.TryParse(cidrOrIp, out var exactIp))
        {
            return ip.Equals(exactIp);
        }

        return false;
    }

    /// <summary>
    /// Checks if an IP address is within a subnet.
    /// </summary>
    private static bool IsInSubnet(IPAddress ip, IPAddress networkAddress, int prefixLength)
    {
        // Ensure both addresses are the same family
        if (ip.AddressFamily != networkAddress.AddressFamily)
        {
            return false;
        }

        var ipBytes = ip.GetAddressBytes();
        var networkBytes = networkAddress.GetAddressBytes();

        // Calculate the number of full bytes and remaining bits
        var fullBytes = prefixLength / 8;
        var remainingBits = prefixLength % 8;

        // Check full bytes
        for (var i = 0; i < fullBytes; i++)
        {
            if (ipBytes[i] != networkBytes[i])
            {
                return false;
            }
        }

        // Check remaining bits if any
        if (remainingBits > 0 && fullBytes < ipBytes.Length)
        {
            var mask = (byte)(0xFF << (8 - remainingBits));
            if ((ipBytes[fullBytes] & mask) != (networkBytes[fullBytes] & mask))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Cached representation of an IP restriction.
    /// </summary>
    private sealed record CachedIpRestriction(string IpAddress, string Reason, DateTime? ExpiresAt)
    {
        public bool IsActive(DateTime utcNow) => ExpiresAt == null || ExpiresAt > utcNow;
    }
}
