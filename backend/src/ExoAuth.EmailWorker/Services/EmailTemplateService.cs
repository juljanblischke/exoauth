namespace ExoAuth.EmailWorker.Services;

public sealed class EmailTemplateService : IEmailTemplateService
{
    private readonly ILogger<EmailTemplateService> _logger;
    private readonly string _templatesBasePath;

    public EmailTemplateService(IConfiguration configuration, ILogger<EmailTemplateService> logger)
    {
        _logger = logger;

        var configuredPath = configuration.GetValue<string>("Templates:BasePath");

        if (!string.IsNullOrEmpty(configuredPath) && Path.IsPathRooted(configuredPath))
        {
            _templatesBasePath = configuredPath;
        }
        else
        {
            _templatesBasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, configuredPath ?? "templates/emails");
        }

        _logger.LogInformation("Email templates base path: {Path}", _templatesBasePath);
    }

    public string Render(string templateName, Dictionary<string, string> variables, string language = "en-US")
    {
        var templatePath = GetTemplatePath(templateName, language);

        if (!File.Exists(templatePath))
        {
            _logger.LogWarning("Email template not found: {TemplatePath}, falling back to en-US", templatePath);
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
}
