using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Linq;
using CareerOS.Api.Domain;

namespace CareerOS.Api.Services;

public static class ExportService
{
    static ExportService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    private class LocalizedStrings
    {
        public string Summary { get; set; } = string.Empty;
        public string Skills { get; set; } = string.Empty;
        public string Experience { get; set; } = string.Empty;
        public string Education { get; set; } = string.Empty;
        public string Certifications { get; set; } = string.Empty;
        public string Present { get; set; } = string.Empty;
    }

    private static LocalizedStrings GetLocalization(string language)
    {
        return (language.ToLower()) switch
        {
            "en" => new LocalizedStrings
            {
                Summary = "Professional Summary",
                Skills = "Core Skills",
                Experience = "Professional Experience",
                Education = "Education",
                Certifications = "Certifications",
                Present = "Present"
            },
            "it" => new LocalizedStrings
            {
                Summary = "Riepilogo Professionale",
                Skills = "Competenze Principali",
                Experience = "Esperienza Professionale",
                Education = "Istruzione e Formazione",
                Certifications = "Certificazioni",
                Present = "Attuale"
            },
            _ => new LocalizedStrings // default is "pt"
            {
                Summary = "Resumo Profissional",
                Skills = "Principais Competências",
                Experience = "Experiência Profissional",
                Education = "Formação Acadêmica",
                Certifications = "Certificações",
                Present = "Atual"
            }
        };
    }

    private static (List<WorkExperience> Experiences, List<Education> Educations, List<Certification> Certifications) GetResolvedCollections(Resume resume, CandidateProfile profile)
    {
        List<WorkExperience> experiences = [];
        List<Education> educations = [];
        List<Certification> certifications = [];

        try
        {
            if (!string.IsNullOrEmpty(resume.CustomizedExperiencesJson))
            {
                experiences = JsonSerializer.Deserialize<List<WorkExperience>>(resume.CustomizedExperiencesJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
            }
            else
            {
                experiences = profile.Experiences;
            }
        }
        catch { experiences = profile.Experiences; }

        try
        {
            if (!string.IsNullOrEmpty(resume.CustomizedEducationsJson))
            {
                educations = JsonSerializer.Deserialize<List<Education>>(resume.CustomizedEducationsJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
            }
            else
            {
                educations = profile.Educations;
            }
        }
        catch { educations = profile.Educations; }

        try
        {
            if (!string.IsNullOrEmpty(resume.CustomizedCertificationsJson))
            {
                certifications = JsonSerializer.Deserialize<List<Certification>>(resume.CustomizedCertificationsJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
            }
            else
            {
                certifications = profile.Certifications;
            }
        }
        catch { certifications = profile.Certifications; }

        return (
            experiences.OrderBy(x => x.Order).ToList(),
            educations.OrderBy(x => x.Order).ToList(),
            certifications.OrderBy(x => x.Order).ToList()
        );
    }

    private static string BuildContactString(Resume resume, CandidateProfile profile)
    {
        var parts = new List<string>();
        if (resume.ShowEmail && !string.IsNullOrEmpty(profile.Email)) parts.Add(profile.Email);
        if (resume.ShowPhone && !string.IsNullOrEmpty(profile.Phone)) parts.Add(profile.Phone);
        if (resume.ShowLocation)
        {
            var locParts = new List<string>();
            if (!string.IsNullOrEmpty(profile.City)) locParts.Add(profile.City);
            if (!string.IsNullOrEmpty(profile.Region)) locParts.Add(profile.Region);
            if (!string.IsNullOrEmpty(profile.Country)) locParts.Add(profile.Country);
            if (locParts.Any()) parts.Add(string.Join(", ", locParts));
        }
        return string.Join(" | ", parts);
    }

    public static string GenerateAtsText(Resume resume, CandidateProfile profile)
    {
        var loc = GetLocalization(resume.Language);
        var (experiences, educations, certifications) = GetResolvedCollections(resume, profile);
        var contact = BuildContactString(resume, profile);

        var sb = new StringBuilder();
        sb.AppendLine(profile.FullName.ToUpper());
        if (!string.IsNullOrEmpty(contact)) sb.AppendLine(contact);
        sb.AppendLine();

        var title = string.IsNullOrEmpty(resume.CustomizedTitle) ? profile.ProfessionalTitle : resume.CustomizedTitle;
        sb.AppendLine(title);
        sb.AppendLine(new string('=', title.Length));
        sb.AppendLine();

        var summary = string.IsNullOrEmpty(resume.CustomizedSummary) ? profile.ProfessionalSummary : resume.CustomizedSummary;
        if (!string.IsNullOrEmpty(summary))
        {
            sb.AppendLine(loc.Summary.ToUpper());
            sb.AppendLine(new string('-', loc.Summary.Length));
            sb.AppendLine(summary);
            sb.AppendLine();
        }

        if (!string.IsNullOrEmpty(resume.Skills))
        {
            sb.AppendLine(loc.Skills.ToUpper());
            sb.AppendLine(new string('-', loc.Skills.Length));
            sb.AppendLine(resume.Skills);
            sb.AppendLine();
        }

        if (experiences.Any())
        {
            sb.AppendLine(loc.Experience.ToUpper());
            sb.AppendLine(new string('-', loc.Experience.Length));
            foreach (var exp in experiences)
            {
                var startStr = exp.StartDate.ToString("MM/yyyy");
                var endStr = exp.IsCurrent ? loc.Present : (exp.EndDate?.ToString("MM/yyyy") ?? loc.Present);
                sb.AppendLine($"- {exp.JobTitle} | {exp.CompanyName} ({startStr} - {endStr})");
                if (!string.IsNullOrEmpty(exp.Description))
                {
                    sb.AppendLine($"  {exp.Description}");
                }
                sb.AppendLine();
            }
        }

        if (educations.Any())
        {
            sb.AppendLine(loc.Education.ToUpper());
            sb.AppendLine(new string('-', loc.Education.Length));
            foreach (var edu in educations)
            {
                var startStr = edu.StartDate.ToString("MM/yyyy");
                var endStr = edu.IsCurrent ? loc.Present : (edu.EndDate?.ToString("MM/yyyy") ?? loc.Present);
                sb.AppendLine($"- {edu.Degree} em {edu.FieldOfStudy}");
                sb.AppendLine($"  {edu.Institution} ({startStr} - {endStr})");
                sb.AppendLine();
            }
        }

        if (certifications.Any())
        {
            sb.AppendLine(loc.Certifications.ToUpper());
            sb.AppendLine(new string('-', loc.Certifications.Length));
            foreach (var cert in certifications)
            {
                var dateStr = cert.IssueDate?.ToString("MM/yyyy") ?? "";
                var datePart = string.IsNullOrEmpty(dateStr) ? "" : $" ({dateStr})";
                sb.AppendLine($"- {cert.Name} | {cert.IssuingOrganization}{datePart}");
                if (!string.IsNullOrEmpty(cert.CredentialId))
                {
                    sb.AppendLine($"  ID: {cert.CredentialId}");
                }
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }

    public static byte[] GenerateDocx(Resume resume, CandidateProfile profile)
    {
        var loc = GetLocalization(resume.Language);
        var (experiences, educations, certifications) = GetResolvedCollections(resume, profile);
        var contact = BuildContactString(resume, profile);

        using var memStream = new MemoryStream();
        using (var wordDocument = WordprocessingDocument.Create(memStream, WordprocessingDocumentType.Document))
        {
            var mainPart = wordDocument.AddMainDocumentPart();
            mainPart.Document = new DocumentFormat.OpenXml.Wordprocessing.Document();
            var body = new Body();
            mainPart.Document.Append(body);

            // Style setup / helpers
            var titleParagraph = new Paragraph(new Run(new Text(profile.FullName))
            {
                RunProperties = new RunProperties(new Bold(), new FontSize { Val = "48" }) // 24pt
            });
            body.Append(titleParagraph);

            if (!string.IsNullOrEmpty(contact))
            {
                var contactPara = new Paragraph(new Run(new Text(contact))
                {
                    RunProperties = new RunProperties(new Italic(), new FontSize { Val = "20" }) // 10pt
                });
                body.Append(contactPara);
            }

            var profTitle = string.IsNullOrEmpty(resume.CustomizedTitle) ? profile.ProfessionalTitle : resume.CustomizedTitle;
            if (!string.IsNullOrEmpty(profTitle))
            {
                var titlePara = new Paragraph(new Run(new Text(profTitle))
                {
                    RunProperties = new RunProperties(new Bold(), new FontSize { Val = "28" }) // 14pt
                });
                body.Append(titlePara);
            }

            body.Append(new Paragraph(new Run(new Text("")))); // Spacing

            // Summary Section
            var summary = string.IsNullOrEmpty(resume.CustomizedSummary) ? profile.ProfessionalSummary : resume.CustomizedSummary;
            if (!string.IsNullOrEmpty(summary))
            {
                body.Append(CreateHeading(loc.Summary));
                body.Append(CreateParagraph(summary));
            }

            // Skills Section
            if (!string.IsNullOrEmpty(resume.Skills))
            {
                body.Append(CreateHeading(loc.Skills));
                body.Append(CreateParagraph(resume.Skills));
            }

            // Experience Section
            if (experiences.Any())
            {
                body.Append(CreateHeading(loc.Experience));
                foreach (var exp in experiences)
                {
                    var startStr = exp.StartDate.ToString("MM/yyyy");
                    var endStr = exp.IsCurrent ? loc.Present : (exp.EndDate?.ToString("MM/yyyy") ?? loc.Present);
                    var headerPara = new Paragraph(new Run(new Text($"{exp.JobTitle} - {exp.CompanyName} ({startStr} - {endStr})"))
                    {
                        RunProperties = new RunProperties(new Bold(), new FontSize { Val = "22" }) // 11pt bold
                    });
                    body.Append(headerPara);

                    if (!string.IsNullOrEmpty(exp.Description))
                    {
                        body.Append(CreateParagraph(exp.Description));
                    }
                    body.Append(new Paragraph(new Run(new Text("")))); // Spacing
                }
            }

            // Education Section
            if (educations.Any())
            {
                body.Append(CreateHeading(loc.Education));
                foreach (var edu in educations)
                {
                    var startStr = edu.StartDate.ToString("MM/yyyy");
                    var endStr = edu.IsCurrent ? loc.Present : (edu.EndDate?.ToString("MM/yyyy") ?? loc.Present);
                    var eduPara = new Paragraph(new Run(new Text($"{edu.Degree} em {edu.FieldOfStudy}"))
                    {
                        RunProperties = new RunProperties(new Bold(), new FontSize { Val = "22" }) // 11pt bold
                    });
                    body.Append(eduPara);
                    body.Append(CreateParagraph($"{edu.Institution} ({startStr} - {endStr})"));
                    body.Append(new Paragraph(new Run(new Text("")))); // Spacing
                }
            }

            // Certifications Section
            if (certifications.Any())
            {
                body.Append(CreateHeading(loc.Certifications));
                foreach (var cert in certifications)
                {
                    var dateStr = cert.IssueDate?.ToString("MM/yyyy") ?? "";
                    var datePart = string.IsNullOrEmpty(dateStr) ? "" : $" ({dateStr})";
                    var certPara = new Paragraph(new Run(new Text($"{cert.Name} - {cert.IssuingOrganization}{datePart}"))
                    {
                        RunProperties = new RunProperties(new FontSize { Val = "22" }) // 11pt
                    });
                    body.Append(certPara);
                    if (!string.IsNullOrEmpty(cert.CredentialId))
                    {
                        body.Append(CreateParagraph($"ID: {cert.CredentialId}"));
                    }
                }
            }
        }

        return memStream.ToArray();
    }

    private static Paragraph CreateHeading(string text)
    {
        return new Paragraph(new Run(new Text(text))
        {
            RunProperties = new RunProperties(new Bold(), new FontSize { Val = "28" }) // 14pt
        });
    }

    private static Paragraph CreateParagraph(string text)
    {
        return new Paragraph(new Run(new Text(text))
        {
            RunProperties = new RunProperties(new FontSize { Val = "22" }) // 11pt
        });
    }

    public static byte[] GeneratePdf(Resume resume, CandidateProfile profile)
    {
        var loc = GetLocalization(resume.Language);
        var (experiences, educations, certifications) = GetResolvedCollections(resume, profile);
        var contact = BuildContactString(resume, profile);
        var profTitle = string.IsNullOrEmpty(resume.CustomizedTitle) ? profile.ProfessionalTitle : resume.CustomizedTitle;
        var summary = string.IsNullOrEmpty(resume.CustomizedSummary) ? profile.ProfessionalSummary : resume.CustomizedSummary;

        var document = QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(1, QuestPDF.Infrastructure.Unit.Inch);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(11));

                page.Content().Column(col =>
                {
                    // Full Name
                    col.Item().Text(profile.FullName).FontSize(24).Bold();

                    // Contact Info
                    if (!string.IsNullOrEmpty(contact))
                    {
                        col.Item().Text(contact).FontSize(10).Italic();
                    }

                    // Professional Title
                    if (!string.IsNullOrEmpty(profTitle))
                    {
                        col.Item().PaddingTop(5).Text(profTitle).FontSize(14).Bold().FontColor(Colors.Grey.Darken3);
                    }

                    col.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                    // Professional Summary
                    if (!string.IsNullOrEmpty(summary))
                    {
                        col.Item().PaddingTop(15).Text(loc.Summary).FontSize(14).Bold();
                        col.Item().PaddingTop(2).Text(summary).FontSize(11);
                    }

                    // Core Skills
                    if (!string.IsNullOrEmpty(resume.Skills))
                    {
                        col.Item().PaddingTop(15).Text(loc.Skills).FontSize(14).Bold();
                        col.Item().PaddingTop(2).Text(resume.Skills).FontSize(11);
                    }

                    // Experiences
                    if (experiences.Any())
                    {
                        col.Item().PaddingTop(15).Text(loc.Experience).FontSize(14).Bold();
                        foreach (var exp in experiences)
                        {
                            var startStr = exp.StartDate.ToString("MM/yyyy");
                            var endStr = exp.IsCurrent ? loc.Present : (exp.EndDate?.ToString("MM/yyyy") ?? loc.Present);

                            col.Item().PaddingTop(5).Row(row =>
                            {
                                row.RelativeItem().Text($"{exp.JobTitle} | {exp.CompanyName}").Bold();
                                row.ConstantItem(150).AlignRight().Text($"{startStr} - {endStr}").Italic();
                            });

                            if (!string.IsNullOrEmpty(exp.Description))
                            {
                                col.Item().PaddingTop(2).Text(exp.Description).FontSize(10);
                            }
                        }
                    }

                    // Education
                    if (educations.Any())
                    {
                        col.Item().PaddingTop(15).Text(loc.Education).FontSize(14).Bold();
                        foreach (var edu in educations)
                        {
                            var startStr = edu.StartDate.ToString("MM/yyyy");
                            var endStr = edu.IsCurrent ? loc.Present : (edu.EndDate?.ToString("MM/yyyy") ?? loc.Present);

                            col.Item().PaddingTop(5).Row(row =>
                            {
                                row.RelativeItem().Text($"{edu.Degree} em {edu.FieldOfStudy}").Bold();
                                row.ConstantItem(150).AlignRight().Text($"{startStr} - {endStr}").Italic();
                            });
                            col.Item().Text(edu.Institution).FontSize(10);
                        }
                    }

                    // Certifications
                    if (certifications.Any())
                    {
                        col.Item().PaddingTop(15).Text(loc.Certifications).FontSize(14).Bold();
                        foreach (var cert in certifications)
                        {
                            var dateStr = cert.IssueDate?.ToString("MM/yyyy") ?? "";
                            var datePart = string.IsNullOrEmpty(dateStr) ? "" : $" ({dateStr})";

                            col.Item().PaddingTop(3).Text($"{cert.Name} | {cert.IssuingOrganization}{datePart}");
                            if (!string.IsNullOrEmpty(cert.CredentialId))
                            {
                                col.Item().Text($"ID: {cert.CredentialId}").FontSize(9).FontColor(Colors.Grey.Darken1);
                            }
                        }
                    }
                });
            });
        });

        return document.GeneratePdf();
    }
}
