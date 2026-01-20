using System.Text;
using Microsoft.Extensions.Logging;
using WhearApp.Application.Common.Interfaces;

namespace WhearApp.Infrastructure.Common.Services;

public class EmailTemplateService(ILogger<EmailTemplateService> logger) : IEmailTemplateService
{
    private readonly string _templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "EmailTemplates");

    public string RenderTemplate(string templateName, object model)
    {
        try
        {
            var templateFile = Path.Combine(_templatePath, $"{templateName}.html");
            
            if (!File.Exists(templateFile))
            {
                logger.LogWarning("Template not found: {TemplateName}", templateName);
                throw new FileNotFoundException($"Template '{templateName}' not found");
            }

            var template = File.ReadAllText(templateFile);
            return ReplaceTokens(template, model);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error rendering template: {TemplateName}", templateName);
            throw;
        }
    }

    private string ReplaceTokens(string template, object model)
    {
        var result = new StringBuilder(template);
        var properties = model.GetType().GetProperties();

        foreach (var prop in properties)
        {
            var value = prop.GetValue(model)?.ToString() ?? string.Empty;
            result.Replace($"{{{{{prop.Name}}}}}", value);
        }

        return result.ToString();
    }
}