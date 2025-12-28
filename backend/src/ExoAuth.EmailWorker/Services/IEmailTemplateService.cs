namespace ExoAuth.EmailWorker.Services;

public interface IEmailTemplateService
{
    string Render(string templateName, Dictionary<string, string> variables, string language = "en");
    bool TemplateExists(string templateName, string language);
}
