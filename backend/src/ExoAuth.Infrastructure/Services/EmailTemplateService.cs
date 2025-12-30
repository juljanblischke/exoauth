using System.Collections.Concurrent;
using System.Text.Json;
using ExoAuth.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace ExoAuth.Infrastructure.Services;

public sealed class EmailTemplateService : IEmailTemplateService
{
    private readonly ILogger<EmailTemplateService> _logger;
    private readonly string _templatesBasePath;
    private readonly ConcurrentDictionary<string, Dictionary<string, string>> _subjectsCache = new();

    public EmailTemplateService(ILogger<EmailTemplateService> logger)
    {
        _logger = logger;
        _templatesBasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "templates", "emails");
    }

    public string Render(string templateName, Dictionary<string, string> variables, string language = "en-US")
    {
        var templatePath = GetTemplatePath(templateName, language);

        if (!File.Exists(templatePath))
        {
            _logger.LogWarning("Email template not found: {TemplatePath}, falling back to English", templatePath);
            templatePath = GetTemplatePath(templateName, "en-US");
        }

        if (!File.Exists(templatePath))
        {
            throw new FileNotFoundException($"Email template not found: {templateName}");
        }

        var content = File.ReadAllText(templatePath);

        // Simple variable replacement using {{variable}} syntax
        foreach (var (key, value) in variables)
        {
            content = content.Replace($"{{{{{key}}}}}", value);
        }

        return content;
    }

    public bool TemplateExists(string templateName, string language)
    {
        var templatePath = GetTemplatePath(templateName, language);
        return File.Exists(templatePath);
    }

    private string GetTemplatePath(string templateName, string language)
    {
        return Path.Combine(_templatesBasePath, language, $"{templateName}.html");
    }

    public string GetSubject(string templateName, string language = "en-US")
    {
        var subjects = LoadSubjects(language);

        if (subjects.TryGetValue(templateName, out var subject))
        {
            return subject;
        }

        // Fallback to en-US if subject not found in requested language
        if (language != "en-US")
        {
            _logger.LogWarning(
                "Email subject for template {TemplateName} not found in {Language}, falling back to en-US",
                templateName, language);

            var englishSubjects = LoadSubjects("en-US");
            if (englishSubjects.TryGetValue(templateName, out var englishSubject))
            {
                return englishSubject;
            }
        }

        _logger.LogError("Email subject for template {TemplateName} not found in any language", templateName);
        return templateName; // Return template name as fallback
    }

    private Dictionary<string, string> LoadSubjects(string language)
    {
        return _subjectsCache.GetOrAdd(language, lang =>
        {
            var subjectsPath = Path.Combine(_templatesBasePath, lang, "subjects.json");

            if (!File.Exists(subjectsPath))
            {
                _logger.LogWarning("Subjects file not found: {SubjectsPath}", subjectsPath);
                return new Dictionary<string, string>();
            }

            try
            {
                var json = File.ReadAllText(subjectsPath);
                return JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                    ?? new Dictionary<string, string>();
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse subjects.json for language {Language}", lang);
                return new Dictionary<string, string>();
            }
        });
    }
}
