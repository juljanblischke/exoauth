namespace ExoAuth.Application.Common.Interfaces;

/// <summary>
/// Service for rendering email templates.
/// </summary>
public interface IEmailTemplateService
{
    /// <summary>
    /// Renders an email template with the given variables.
    /// </summary>
    /// <param name="templateName">The name of the template (without extension).</param>
    /// <param name="variables">Variables to replace in the template.</param>
    /// <param name="language">The language for the template (default: "en").</param>
    /// <returns>The rendered template content.</returns>
    string Render(string templateName, Dictionary<string, string> variables, string language = "en");

    /// <summary>
    /// Checks if a template exists for the given language.
    /// </summary>
    /// <param name="templateName">The name of the template.</param>
    /// <param name="language">The language to check.</param>
    /// <returns>True if the template exists, false otherwise.</returns>
    bool TemplateExists(string templateName, string language);
}
