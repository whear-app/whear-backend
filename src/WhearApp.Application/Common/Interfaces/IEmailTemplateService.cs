namespace WhearApp.Application.Common.Interfaces;

public interface IEmailTemplateService
{
    string RenderTemplate(string templateName, object model);
}