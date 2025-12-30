using ExoAuth.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace ExoAuth.UnitTests.Services;

public sealed class EmailTemplateServiceTests : IDisposable
{
    private readonly Mock<ILogger<EmailTemplateService>> _mockLogger;
    private readonly EmailTemplateService _service;
    private readonly string _tempDir;
    private readonly string _templatesDir;

    public EmailTemplateServiceTests()
    {
        _mockLogger = new Mock<ILogger<EmailTemplateService>>();
        _service = new EmailTemplateService(_mockLogger.Object);

        // Create temp directory for test templates
        _tempDir = Path.Combine(Path.GetTempPath(), "exoauth-tests-" + Guid.NewGuid().ToString("N"));
        _templatesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "templates", "emails");

        // Ensure template directories exist for tests that need them
        Directory.CreateDirectory(Path.Combine(_templatesDir, "en-US"));
        Directory.CreateDirectory(Path.Combine(_templatesDir, "de-DE"));
    }

    public void Dispose()
    {
        // Clean up temp directory
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    [Fact]
    public void Render_ReplacesVariablesCorrectly()
    {
        // Arrange
        var templatePath = Path.Combine(_templatesDir, "en-US", "test-template.html");
        var templateContent = "<html><body>Hello {{firstName}} {{lastName}}!</body></html>";

        try
        {
            File.WriteAllText(templatePath, templateContent);

            var variables = new Dictionary<string, string>
            {
                ["firstName"] = "John",
                ["lastName"] = "Doe"
            };

            // Act
            var result = _service.Render("test-template", variables, "en-US");

            // Assert
            result.Should().Contain("Hello John Doe!");
        }
        finally
        {
            if (File.Exists(templatePath))
                File.Delete(templatePath);
        }
    }

    [Fact]
    public void Render_WithMissingVariables_LeavesPlaceholdersIntact()
    {
        // Arrange
        var templatePath = Path.Combine(_templatesDir, "en-US", "test-missing-var.html");
        var templateContent = "<html><body>Hello {{firstName}} {{unknownVar}}!</body></html>";

        try
        {
            File.WriteAllText(templatePath, templateContent);

            var variables = new Dictionary<string, string>
            {
                ["firstName"] = "John"
            };

            // Act
            var result = _service.Render("test-missing-var", variables, "en-US");

            // Assert
            result.Should().Contain("Hello John {{unknownVar}}!");
        }
        finally
        {
            if (File.Exists(templatePath))
                File.Delete(templatePath);
        }
    }

    [Fact]
    public void Render_FallsBackToEnglishWhenLanguageNotFound()
    {
        // Arrange
        var templatePath = Path.Combine(_templatesDir, "en-US", "test-fallback.html");
        var templateContent = "<html><body>English content</body></html>";

        try
        {
            File.WriteAllText(templatePath, templateContent);

            var variables = new Dictionary<string, string>();

            // Act - request German but only English exists
            var result = _service.Render("test-fallback", variables, "de-DE");

            // Assert
            result.Should().Contain("English content");
        }
        finally
        {
            if (File.Exists(templatePath))
                File.Delete(templatePath);
        }
    }

    [Fact]
    public void Render_UsesCorrectLanguageWhenAvailable()
    {
        // Arrange
        var enPath = Path.Combine(_templatesDir, "en-US", "test-lang.html");
        var dePath = Path.Combine(_templatesDir, "de-DE", "test-lang.html");

        try
        {
            File.WriteAllText(enPath, "<html><body>English</body></html>");
            File.WriteAllText(dePath, "<html><body>Deutsch</body></html>");

            var variables = new Dictionary<string, string>();

            // Act
            var resultEn = _service.Render("test-lang", variables, "en-US");
            var resultDe = _service.Render("test-lang", variables, "de-DE");

            // Assert
            resultEn.Should().Contain("English");
            resultDe.Should().Contain("Deutsch");
        }
        finally
        {
            if (File.Exists(enPath))
                File.Delete(enPath);
            if (File.Exists(dePath))
                File.Delete(dePath);
        }
    }

    [Fact]
    public void Render_ThrowsWhenTemplateNotFoundInAnyLanguage()
    {
        // Arrange
        var variables = new Dictionary<string, string>();

        // Act & Assert
        var act = () => _service.Render("non-existent-template", variables, "en-US");
        act.Should().Throw<FileNotFoundException>();
    }

    [Fact]
    public void TemplateExists_ReturnsTrueWhenExists()
    {
        // Arrange
        var templatePath = Path.Combine(_templatesDir, "en-US", "test-exists.html");

        try
        {
            File.WriteAllText(templatePath, "<html></html>");

            // Act
            var result = _service.TemplateExists("test-exists", "en-US");

            // Assert
            result.Should().BeTrue();
        }
        finally
        {
            if (File.Exists(templatePath))
                File.Delete(templatePath);
        }
    }

    [Fact]
    public void TemplateExists_ReturnsFalseWhenNotExists()
    {
        // Act
        var result = _service.TemplateExists("definitely-not-exists-" + Guid.NewGuid(), "en-US");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Render_HandlesMultipleVariableReplacements()
    {
        // Arrange
        var templatePath = Path.Combine(_templatesDir, "en-US", "test-multi.html");
        var templateContent = @"
            <html>
                <body>
                    <p>Dear {{firstName}} {{lastName}},</p>
                    <p>Your email is {{email}}.</p>
                    <p>You were invited by {{inviterName}}.</p>
                    <p>Link: {{inviteLink}}</p>
                    <p>Expires in {{expirationHours}} hours.</p>
                    <p>© {{year}} ExoAuth</p>
                </body>
            </html>";

        try
        {
            File.WriteAllText(templatePath, templateContent);

            var variables = new Dictionary<string, string>
            {
                ["firstName"] = "Jane",
                ["lastName"] = "Smith",
                ["email"] = "jane@test.com",
                ["inviterName"] = "Admin User",
                ["inviteLink"] = "https://example.com/invite/abc123",
                ["expirationHours"] = "24",
                ["year"] = "2025"
            };

            // Act
            var result = _service.Render("test-multi", variables, "en-US");

            // Assert
            result.Should().Contain("Jane Smith");
            result.Should().Contain("jane@test.com");
            result.Should().Contain("Admin User");
            result.Should().Contain("https://example.com/invite/abc123");
            result.Should().Contain("24 hours");
            result.Should().Contain("2025 ExoAuth");
        }
        finally
        {
            if (File.Exists(templatePath))
                File.Delete(templatePath);
        }
    }

    [Fact]
    public void Render_WithEmptyVariables_ReturnsTemplateUnchanged()
    {
        // Arrange
        var templatePath = Path.Combine(_templatesDir, "en-US", "test-empty.html");
        var templateContent = "<html><body>Static content</body></html>";

        try
        {
            File.WriteAllText(templatePath, templateContent);

            var variables = new Dictionary<string, string>();

            // Act
            var result = _service.Render("test-empty", variables, "en-US");

            // Assert
            result.Should().Be(templateContent);
        }
        finally
        {
            if (File.Exists(templatePath))
                File.Delete(templatePath);
        }
    }

    [Fact]
    public void Render_HandlesSpecialCharactersInVariables()
    {
        // Arrange
        var templatePath = Path.Combine(_templatesDir, "en-US", "test-special.html");
        var templateContent = "<html><body>Name: {{name}}</body></html>";

        try
        {
            File.WriteAllText(templatePath, templateContent);

            var variables = new Dictionary<string, string>
            {
                ["name"] = "O'Connor <script>alert('xss')</script>"
            };

            // Act
            var result = _service.Render("test-special", variables, "en-US");

            // Assert
            // Note: The service does NOT escape HTML - that should be done before passing variables
            result.Should().Contain("O'Connor <script>alert('xss')</script>");
        }
        finally
        {
            if (File.Exists(templatePath))
                File.Delete(templatePath);
        }
    }

    [Fact]
    public void GetSubject_ReturnsEnglishSubject()
    {
        // Arrange
        var subjectsPath = Path.Combine(_templatesDir, "en-US", "subjects.json");
        var subjectsContent = @"{
            ""system-invite"": ""Invitation to ExoAuth"",
            ""password-reset"": ""Reset Your Password""
        }";

        try
        {
            File.WriteAllText(subjectsPath, subjectsContent);

            // Act
            var result = _service.GetSubject("system-invite", "en-US");

            // Assert
            result.Should().Be("Invitation to ExoAuth");
        }
        finally
        {
            if (File.Exists(subjectsPath))
                File.Delete(subjectsPath);
        }
    }

    [Fact]
    public void GetSubject_ReturnsGermanSubject()
    {
        // Arrange
        var subjectsPath = Path.Combine(_templatesDir, "de-DE", "subjects.json");
        var subjectsContent = @"{
            ""system-invite"": ""Einladung zu ExoAuth"",
            ""password-reset"": ""Passwort zurücksetzen""
        }";

        try
        {
            File.WriteAllText(subjectsPath, subjectsContent);

            // Act
            var result = _service.GetSubject("system-invite", "de-DE");

            // Assert
            result.Should().Be("Einladung zu ExoAuth");
        }
        finally
        {
            if (File.Exists(subjectsPath))
                File.Delete(subjectsPath);
        }
    }

    [Fact]
    public void GetSubject_FallsBackToEnglishWhenLanguageNotFound()
    {
        // Arrange
        var enSubjectsPath = Path.Combine(_templatesDir, "en-US", "subjects.json");
        var enSubjectsContent = @"{
            ""system-invite"": ""Invitation to ExoAuth""
        }";

        try
        {
            File.WriteAllText(enSubjectsPath, enSubjectsContent);

            // Act - request French but only English exists
            var result = _service.GetSubject("system-invite", "fr-FR");

            // Assert
            result.Should().Be("Invitation to ExoAuth");
        }
        finally
        {
            if (File.Exists(enSubjectsPath))
                File.Delete(enSubjectsPath);
        }
    }

    [Fact]
    public void GetSubject_FallsBackToEnglishWhenSubjectNotFoundInRequestedLanguage()
    {
        // Arrange
        var enSubjectsPath = Path.Combine(_templatesDir, "en-US", "subjects.json");
        var deSubjectsPath = Path.Combine(_templatesDir, "de-DE", "subjects.json");
        var enSubjectsContent = @"{
            ""system-invite"": ""Invitation to ExoAuth"",
            ""special-template"": ""Special Subject""
        }";
        var deSubjectsContent = @"{
            ""system-invite"": ""Einladung zu ExoAuth""
        }";

        try
        {
            File.WriteAllText(enSubjectsPath, enSubjectsContent);
            File.WriteAllText(deSubjectsPath, deSubjectsContent);

            // Act - request German but "special-template" only exists in English
            var result = _service.GetSubject("special-template", "de-DE");

            // Assert
            result.Should().Be("Special Subject");
        }
        finally
        {
            if (File.Exists(enSubjectsPath))
                File.Delete(enSubjectsPath);
            if (File.Exists(deSubjectsPath))
                File.Delete(deSubjectsPath);
        }
    }

    [Fact]
    public void GetSubject_ReturnsTemplateNameWhenSubjectNotFoundInAnyLanguage()
    {
        // Arrange
        var subjectsPath = Path.Combine(_templatesDir, "en-US", "subjects.json");
        var subjectsContent = @"{
            ""system-invite"": ""Invitation to ExoAuth""
        }";

        try
        {
            File.WriteAllText(subjectsPath, subjectsContent);

            // Act - request a template that doesn't exist in any subjects.json
            var result = _service.GetSubject("non-existent-template", "en-US");

            // Assert - should return the template name as fallback
            result.Should().Be("non-existent-template");
        }
        finally
        {
            if (File.Exists(subjectsPath))
                File.Delete(subjectsPath);
        }
    }

    [Fact]
    public void GetSubject_CachesSubjectsOnFirstLoad()
    {
        // Arrange
        var subjectsPath = Path.Combine(_templatesDir, "en-US", "subjects.json");
        var subjectsContent = @"{
            ""system-invite"": ""Invitation to ExoAuth""
        }";

        try
        {
            File.WriteAllText(subjectsPath, subjectsContent);

            // Act - call twice
            var result1 = _service.GetSubject("system-invite", "en-US");
            var result2 = _service.GetSubject("system-invite", "en-US");

            // Assert - both should work and return the same result
            result1.Should().Be("Invitation to ExoAuth");
            result2.Should().Be("Invitation to ExoAuth");
        }
        finally
        {
            if (File.Exists(subjectsPath))
                File.Delete(subjectsPath);
        }
    }
}
