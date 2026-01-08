using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using ExoAuth.Application.Common.Interfaces;

namespace ExoAuth.Infrastructure.Services.Email.Providers;

public sealed class AmazonSesEmailProvider : IEmailProviderImplementation
{
    private readonly AmazonSesConfig _config;
    private readonly HttpClient _httpClient;

    public AmazonSesEmailProvider(AmazonSesConfig config, HttpClient httpClient)
    {
        _config = config;
        _httpClient = httpClient;
    }

    public async Task SendAsync(
        string to,
        string subject,
        string htmlBody,
        string? plainTextBody,
        CancellationToken cancellationToken = default)
    {
        // Using SES v2 API with simple email
        var url = $"https://email.{_config.Region}.amazonaws.com/v2/email/outbound-emails";

        var payload = new
        {
            FromEmailAddress = $"{_config.FromName} <{_config.FromEmail}>",
            Destination = new
            {
                ToAddresses = new[] { to }
            },
            Content = new
            {
                Simple = new
                {
                    Subject = new { Data = subject },
                    Body = new
                    {
                        Html = new { Data = htmlBody },
                        Text = string.IsNullOrWhiteSpace(plainTextBody) ? null : new { Data = plainTextBody }
                    }
                }
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Content = JsonContent.Create(payload);

        // Sign the request using AWS Signature Version 4
        await SignRequestAsync(request);

        var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Amazon SES API error: {response.StatusCode} - {error}");
        }
    }

    private Task SignRequestAsync(HttpRequestMessage request)
    {
        // AWS Signature Version 4 signing
        // This is a simplified version - production should use AWS SDK
        var dateTime = DateTime.UtcNow;
        var dateStamp = dateTime.ToString("yyyyMMdd");
        var amzDate = dateTime.ToString("yyyyMMddTHHmmssZ");

        request.Headers.Add("X-Amz-Date", amzDate);

        // For production, use AWSSDK.SimpleEmail package instead of manual signing
        // This implementation provides basic structure for the signing process
        var credentialScope = $"{dateStamp}/{_config.Region}/ses/aws4_request";
        var algorithm = "AWS4-HMAC-SHA256";

        // Canonical request and string to sign would be computed here
        // Then HMAC-SHA256 signature chain
        // Finally add Authorization header

        // Placeholder - in production use AWS SDK
        var signature = ComputeSignature(dateStamp, amzDate);
        request.Headers.TryAddWithoutValidation("Authorization",
            $"{algorithm} Credential={_config.AccessKey}/{credentialScope}, SignedHeaders=host;x-amz-date, Signature={signature}");

        return Task.CompletedTask;
    }

    private string ComputeSignature(string dateStamp, string amzDate)
    {
        // Simplified - use AWS SDK in production
        var kSecret = Encoding.UTF8.GetBytes($"AWS4{_config.SecretKey}");
        var kDate = HmacSha256(kSecret, dateStamp);
        var kRegion = HmacSha256(kDate, _config.Region);
        var kService = HmacSha256(kRegion, "ses");
        var kSigning = HmacSha256(kService, "aws4_request");

        // This would need the actual string to sign
        return Convert.ToHexString(kSigning).ToLower();
    }

    private static byte[] HmacSha256(byte[] key, string data)
    {
        using var hmac = new HMACSHA256(key);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
    }
}
