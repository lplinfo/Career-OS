using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CareerOS.Api.Contracts;
using UglyToad.PdfPig;

namespace CareerOS.Api.Services;

public class LinkedinParserService : ILinkedinParserService
{
    private static readonly Regex EmailRegex = new(@"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}", RegexOptions.Compiled);
    private static readonly Regex DateRangeRegex = new(@"(?<start>(?:Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec|Jan|Fev|Mar|Abr|Mai|Jun|Jul|Ago|Set|Out|Nov|Dez|[A-Za-z]+)?\s?\d{4})\s*[-–—]\s*(?<end>Present|Presente|Atual|\b(?:Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec|Jan|Fev|Mar|Abr|Mai|Jun|Jul|Ago|Set|Out|Nov|Dez|[A-Za-z]+)?\s?\d{4})?", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public ParsedCandidateProfileDto ParsePdf(Stream pdfStream)
    {
        var result = new ParsedCandidateProfileDto();
        var fullTextLines = new List<string>();

        using (var pdf = PdfDocument.Open(pdfStream))
        {
            foreach (var page in pdf.GetPages())
            {
                var text = page.Text;
                if (!string.IsNullOrWhiteSpace(text))
                {
                    var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                                    .Select(l => l.Trim())
                                    .Where(l => !string.IsNullOrWhiteSpace(l));
                    fullTextLines.AddRange(lines);
                }
            }
        }

        if (fullTextLines.Count == 0) return result;

        var joinedContent = string.Join("\n", fullTextLines);
        var emailMatch = EmailRegex.Match(joinedContent);
        if (emailMatch.Success)
        {
            result.Email = emailMatch.Value;
        }

        var sections = ExtractSections(fullTextLines);
        ExtractHeaderInfo(fullTextLines, sections, result);

        if (sections.TryGetValue("SUMMARY", out var summaryLines))
        {
            result.ProfessionalSummary = string.Join(" ", summaryLines).Trim();
        }

        if (sections.TryGetValue("EXPERIENCE", out var expLines))
        {
            result.WorkExperiences = ParseExperiences(expLines);
        }

        if (sections.TryGetValue("EDUCATION", out var eduLines))
        {
            result.EducationHistory = ParseEducation(eduLines);
        }

        if (sections.TryGetValue("CERTIFICATIONS", out var certLines))
        {
            result.Certifications = ParseCertifications(certLines);
        }

        if (sections.TryGetValue("SKILLS", out var skillLines))
        {
            result.Skills = skillLines.SelectMany(l => l.Split(new[] { ',', ';', '•', '|' }, StringSplitOptions.RemoveEmptyEntries))
                                      .Select(s => s.Trim())
                                      .Where(s => s.Length > 1)
                                      .Distinct()
                                      .ToList();
        }

        if (sections.TryGetValue("LANGUAGES", out var langLines))
        {
            result.Languages = langLines.SelectMany(l => l.Split(new[] { ',', ';', '•', '|' }, StringSplitOptions.RemoveEmptyEntries))
                                        .Select(s => s.Trim())
                                        .Where(s => s.Length > 1)
                                        .Distinct()
                                        .ToList();
        }

        return result;
    }

    private static void ExtractHeaderInfo(List<string> lines, Dictionary<string, List<string>> sections, ParsedCandidateProfileDto profile)
    {
        var headerLines = lines.TakeWhile(l => !IsSectionHeader(l)).ToList();
        if (headerLines.Count > 0)
        {
            profile.FullName = headerLines[0];
        }
        if (headerLines.Count > 1)
        {
            profile.ProfessionalTitle = headerLines[1];
        }
        if (headerLines.Count > 2)
        {
            var loc = headerLines[2];
            if (!EmailRegex.IsMatch(loc) && !loc.StartsWith("Page ", StringComparison.OrdinalIgnoreCase))
            {
                var parts = loc.Split(new[] { ',', '-' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 1) profile.City = parts[0].Trim();
                if (parts.Length >= 2) profile.Region = parts[1].Trim();
                if (parts.Length >= 3) profile.Country = parts[2].Trim();
            }
        }
    }

    private static Dictionary<string, List<string>> ExtractSections(List<string> lines)
    {
        var sections = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        string currentSection = "HEADER";
        sections[currentSection] = new List<string>();

        foreach (var line in lines)
        {
            var normalizedHeader = ClassifyHeader(line);
            if (normalizedHeader != null)
            {
                currentSection = normalizedHeader;
                if (!sections.ContainsKey(currentSection))
                {
                    sections[currentSection] = new List<string>();
                }
            }
            else
            {
                sections[currentSection].Add(line);
            }
        }

        return sections;
    }

    private static bool IsSectionHeader(string line) => ClassifyHeader(line) != null;

    private static string? ClassifyHeader(string line)
    {
        var l = line.Trim().ToLowerInvariant();
        if (l is "resumo" or "summary" or "sobre" or "about") return "SUMMARY";
        if (l is "experiência" or "experiencia" or "experiências" or "experience" or "work experience") return "EXPERIENCE";
        if (l is "formação acadêmica" or "formação académica" or "formação" or "education") return "EDUCATION";
        if (l is "licenças e certificações" or "certificações" or "certificacoes" or "certifications" or "licenses & certifications") return "CERTIFICATIONS";
        if (l is "principais competências" or "competências" or "competencias" or "skills" or "core skills") return "SKILLS";
        if (l is "idiomas" or "languages" or "línguas") return "LANGUAGES";
        if (l is "contato" or "contact") return "CONTACT";
        return null;
    }

    private static List<ParsedWorkExperienceDto> ParseExperiences(List<string> lines)
    {
        var list = new List<ParsedWorkExperienceDto>();
        ParsedWorkExperienceDto? current = null;
        int order = 0;

        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            var dateMatch = DateRangeRegex.Match(line);

            if (dateMatch.Success)
            {
                if (current != null && !string.IsNullOrWhiteSpace(current.Company))
                {
                    current.DisplayOrder = order++;
                    list.Add(current);
                }

                current = new ParsedWorkExperienceDto();
                if (i >= 2)
                {
                    current.Company = lines[i - 2];
                    current.Role = lines[i - 1];
                }
                else if (i >= 1)
                {
                    current.Company = lines[i - 1];
                    current.Role = "Cargo não especificado";
                }

                current.StartDate = ParseDate(dateMatch.Groups["start"].Value);
                var endStr = dateMatch.Groups["end"].Value;
                if (!string.IsNullOrWhiteSpace(endStr) && (endStr.Equals("Present", StringComparison.OrdinalIgnoreCase) || endStr.Equals("Presente", StringComparison.OrdinalIgnoreCase) || endStr.Equals("Atual", StringComparison.OrdinalIgnoreCase)))
                {
                    current.IsCurrent = true;
                    current.EndDate = null;
                }
                else if (!string.IsNullOrWhiteSpace(endStr))
                {
                    current.EndDate = ParseDate(endStr);
                }
            }
            else if (current != null)
            {
                if (string.IsNullOrWhiteSpace(current.Description))
                    current.Description = line;
                else
                    current.Description += "\n" + line;
            }
            else
            {
                if (lines.Count - i >= 2 && current == null)
                {
                    current = new ParsedWorkExperienceDto
                    {
                        Role = line,
                        Company = lines.ElementAtOrDefault(i + 1) ?? "Empresa",
                        DisplayOrder = order++
                    };
                    i++;
                }
            }
        }

        if (current != null && !string.IsNullOrWhiteSpace(current.Company))
        {
            current.DisplayOrder = order;
            list.Add(current);
        }

        return list;
    }

    private static List<ParsedEducationDto> ParseEducation(List<string> lines)
    {
        var list = new List<ParsedEducationDto>();
        int order = 0;
        ParsedEducationDto? current = null;

        foreach (var line in lines)
        {
            var l = line.Trim();
            if (string.IsNullOrWhiteSpace(l)) continue;

            if (DateRangeRegex.IsMatch(l) || Regex.IsMatch(l, @"^\d{4}(\s*[-–—]\s*\d{4})?$"))
            {
                continue;
            }

            if (current == null)
            {
                current = new ParsedEducationDto
                {
                    Institution = l,
                    DisplayOrder = order++
                };
            }
            else if (string.IsNullOrWhiteSpace(current.Course) || current.Course == "Curso Geral")
            {
                current.Course = l;
            }
            else
            {
                list.Add(current);
                current = new ParsedEducationDto
                {
                    Institution = l,
                    DisplayOrder = order++
                };
            }
        }

        if (current != null)
        {
            if (string.IsNullOrWhiteSpace(current.Course))
            {
                current.Course = "Curso Geral";
            }
            list.Add(current);
        }

        return list;
    }

    private static List<ParsedCertificationDto> ParseCertifications(List<string> lines)
    {
        var list = new List<ParsedCertificationDto>();
        int order = 0;
        ParsedCertificationDto? current = null;

        foreach (var line in lines)
        {
            var l = line.Trim();
            if (string.IsNullOrWhiteSpace(l)) continue;

            if (DateRangeRegex.IsMatch(l) || Regex.IsMatch(l, @"^(Emitido|Issued|Credential|ID\b)", RegexOptions.IgnoreCase))
            {
                continue;
            }

            if (current == null)
            {
                current = new ParsedCertificationDto
                {
                    Name = l,
                    DisplayOrder = order++
                };
            }
            else if (string.IsNullOrWhiteSpace(current.Issuer))
            {
                current.Issuer = l;
            }
            else
            {
                list.Add(current);
                current = new ParsedCertificationDto
                {
                    Name = l,
                    DisplayOrder = order++
                };
            }
        }

        if (current != null)
        {
            list.Add(current);
        }

        return list;
    }

    private static DateOnly? ParseDate(string str)
    {
        if (string.IsNullOrWhiteSpace(str)) return null;
        var parts = str.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1 && int.TryParse(parts[0], out int yearOnly))
        {
            return new DateOnly(yearOnly, 1, 1);
        }
        if (parts.Length >= 2 && int.TryParse(parts[1], out int year))
        {
            int month = ParseMonth(parts[0]);
            return new DateOnly(year, month, 1);
        }
        return null;
    }

    private static int ParseMonth(string str)
    {
        var m = str.ToLowerInvariant();
        if (m.StartsWith("jan")) return 1;
        if (m.StartsWith("fev") || m.StartsWith("feb")) return 2;
        if (m.StartsWith("mar")) return 3;
        if (m.StartsWith("abr") || m.StartsWith("apr")) return 4;
        if (m.StartsWith("mai") || m.StartsWith("may")) return 5;
        if (m.StartsWith("jun")) return 6;
        if (m.StartsWith("jul")) return 7;
        if (m.StartsWith("ago") || m.StartsWith("aug")) return 8;
        if (m.StartsWith("set") || m.StartsWith("sep")) return 9;
        if (m.StartsWith("out") || m.StartsWith("oct")) return 10;
        if (m.StartsWith("nov")) return 11;
        if (m.StartsWith("dez") || m.StartsWith("dec")) return 12;
        return 1;
    }
}
