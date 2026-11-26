using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using ReversiGameSystem.Server.Application.Common;

namespace ReversiGameSystem.Server.Persistence.Mail;

public sealed class RenderedEmailTemplate
{
    public RenderedEmailTemplate(string subject, string htmlBody)
    {
        Subject = subject;
        HtmlBody = htmlBody;
    }

    public string Subject { get; }

    public string HtmlBody { get; }
}

public sealed class HtmlEmailTemplateRenderer
{
    private const string ResourcePrefix = "ReversiGameSystem.Server.Persistence.Mail.Templates.";
    private const string DefaultLanguage = "en";
    private const string SpanishLanguage = "es";
    private const string LocaleValueKey = "Locale";
    private static readonly TimeSpan TitleRegexTimeout = TimeSpan.FromSeconds(1);
    private static readonly Regex TitlePattern = new Regex(
        @"<title>\s*(?<subject>.+?)\s*</title>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline,
        TitleRegexTimeout);
    private static readonly string[] RequiredTemplates =
    {
        EmailTemplateNames.VerificationLogin,
        EmailTemplateNames.VerificationRegistration,
        EmailTemplateNames.VerificationPasswordReset,
        EmailTemplateNames.MatchInvitation
    };
    private static readonly string[] SupportedLanguages =
    {
        DefaultLanguage,
        SpanishLanguage
    };

    private readonly Dictionary<string, RenderedEmailTemplate> _templates =
        new Dictionary<string, RenderedEmailTemplate>(StringComparer.Ordinal);

    public HtmlEmailTemplateRenderer()
    {
        Assembly assembly = typeof(HtmlEmailTemplateRenderer).Assembly;
        foreach (string templateName in RequiredTemplates)
        {
            foreach (string language in SupportedLanguages)
            {
                string key = BuildTemplateKey(templateName, language);
                _templates[key] = LoadEmbeddedTemplate(assembly, templateName, language);
            }
        }
    }

    public RenderedEmailTemplate Render(
        string templateName,
        IReadOnlyDictionary<string, string> templateValues)
    {
        if (string.IsNullOrWhiteSpace(templateName))
        {
            throw new ArgumentException("A template name is required.", nameof(templateName));
        }

        if (templateValues == null)
        {
            throw new ArgumentNullException(nameof(templateValues));
        }

        string language = ResolveLanguage(templateValues);
        string templateKey = BuildTemplateKey(templateName, language);
        if (!_templates.TryGetValue(templateKey, out RenderedEmailTemplate? template))
        {
            throw new InvalidOperationException(
                $"Email template '{templateKey}' is not registered.");
        }

        string renderedBody = template.HtmlBody;
        foreach (KeyValuePair<string, string> entry in templateValues)
        {
            if (string.Equals(entry.Key, LocaleValueKey, StringComparison.Ordinal))
            {
                continue;
            }

            string placeholder = "{{" + entry.Key + "}}";
            string encodedValue = WebUtility.HtmlEncode(entry.Value ?? string.Empty);
            renderedBody = renderedBody.Replace(placeholder, encodedValue);
        }

        RenderedEmailTemplate rendered = new RenderedEmailTemplate(template.Subject, renderedBody);
        return rendered;
    }

    private static string ResolveLanguage(IReadOnlyDictionary<string, string> templateValues)
    {
        if (templateValues.TryGetValue(LocaleValueKey, out string? localeCode)
            && !string.IsNullOrWhiteSpace(localeCode)
            && localeCode.Trim().StartsWith(SpanishLanguage, StringComparison.OrdinalIgnoreCase))
        {
            return SpanishLanguage;
        }

        return DefaultLanguage;
    }

    private static string BuildTemplateKey(string templateName, string language)
    {
        string key = templateName + "." + language;
        return key;
    }

    private static RenderedEmailTemplate LoadEmbeddedTemplate(
        Assembly assembly,
        string templateName,
        string language)
    {
        string resourceName = ResourcePrefix + templateName + "." + language + ".html";
        using (Stream? stream = assembly.GetManifestResourceStream(resourceName))
        {
            if (stream == null)
            {
                throw new InvalidOperationException(
                    $"Embedded email template '{resourceName}' was not found.");
            }

            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            {
                string content = reader.ReadToEnd();
                if (string.IsNullOrWhiteSpace(content))
                {
                    throw new InvalidOperationException(
                        $"Embedded email template '{resourceName}' is empty.");
                }

                string subject = ExtractSubject(content, resourceName);
                RenderedEmailTemplate loadedTemplate = new RenderedEmailTemplate(subject, content);
                return loadedTemplate;
            }
        }
    }

    private static string ExtractSubject(string htmlContent, string resourceName)
    {
        Match match = TitlePattern.Match(htmlContent);
        if (!match.Success)
        {
            throw new InvalidOperationException(
                $"Embedded email template '{resourceName}' must define a <title> element.");
        }

        string subject = WebUtility.HtmlDecode(match.Groups["subject"].Value).Trim();
        if (string.IsNullOrWhiteSpace(subject))
        {
            throw new InvalidOperationException(
                $"Embedded email template '{resourceName}' has an empty <title> element.");
        }

        return subject;
    }
}
