using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CareerOS.Api.Contracts;

namespace CareerOS.Api.Services;

public class LinkedinGapAnalysisService : ILinkedinGapAnalysisService
{
    private static readonly Regex MetricRegex = new(@"\d+%\b|\$\d+|\b\d+\s*(miah|mil|milhões|k|users|usuários|clientes|membros)\b|\b\d+\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public GapAnalysisDto Analyze(ParsedCandidateProfileDto parsedProfile)
    {
        var result = new GapAnalysisDto();
        int totalPoints = 100;
        int deducted = 0;

        if (string.IsNullOrWhiteSpace(parsedProfile.ProfessionalSummary))
        {
            result.MissingFields.Add("Resumo Profissional");
            result.Recommendations.Add(new GapItemDto
            {
                Category = "Perfil",
                Severity = "high",
                Title = "Adicione um Resumo Profissional impactante",
                Description = "Seu perfil não possui um resumo preenchido no LinkedIn.",
                ActionableRecommendation = "Escreva um resumo de 3 a 5 frases destacando sua proposta de valor, conquistas principais e áreas de especialização."
            });
            deducted += 20;
        }

        if (string.IsNullOrWhiteSpace(parsedProfile.Phone))
        {
            result.MissingFields.Add("Telefone de Contato");
            result.Recommendations.Add(new GapItemDto
            {
                Category = "Contato",
                Severity = "medium",
                Title = "Informe seu Telefone de Contato com DDD/DDI",
                Description = "O PDF importado não possui número de telefone visível.",
                ActionableRecommendation = "Cadastre um telefone com WhatsApp para facilitar a abordagem direta por recrutadores."
            });
            deducted += 10;
        }

        if (string.IsNullOrWhiteSpace(parsedProfile.City) && string.IsNullOrWhiteSpace(parsedProfile.Country))
        {
            result.MissingFields.Add("Localização (Cidade/País)");
            result.Recommendations.Add(new GapItemDto
            {
                Category = "Contato",
                Severity = "low",
                Title = "Adicione sua Cidade e Estado/País",
                Description = "A localização geográfica não foi identificada.",
                ActionableRecommendation = "Muitas vagas filtram candidatos por região ou modelo de trabalho local."
            });
            deducted += 5;
        }

        if (parsedProfile.EducationHistory.Count == 0)
        {
            result.MissingFields.Add("Formação Acadêmica");
            result.Recommendations.Add(new GapItemDto
            {
                Category = "Formação",
                Severity = "medium",
                Title = "Cadastre sua Formação Acadêmica",
                Description = "Nenhum curso superior ou formação acadêmica foi localizado no perfil.",
                ActionableRecommendation = "Inclua a instituição, o curso e o ano de conclusão para preencher o requisito acadêmico das empresas."
            });
            deducted += 15;
        }

        if (parsedProfile.Certifications.Count == 0)
        {
            result.MissingFields.Add("Licenças e Certificações");
            result.Recommendations.Add(new GapItemDto
            {
                Category = "Certificações",
                Severity = "low",
                Title = "Adicione Certificações Relevantes",
                Description = "Nenhuma certificação técnica ou profissional foi encontrada.",
                ActionableRecommendation = "Certificados de cursos, metodologias ou tecnologias reforçam a sua qualificação técnica perante recrutadores."
            });
            deducted += 10;
        }

        if (parsedProfile.WorkExperiences.Count == 0)
        {
            result.MissingFields.Add("Histórico de Experiências");
            result.Recommendations.Add(new GapItemDto
            {
                Category = "Experiência",
                Severity = "high",
                Title = "Cadastre suas Experiências Profissionais",
                Description = "Nenhuma experiência profissional foi identificada no PDF.",
                ActionableRecommendation = "Registre suas empresas anteriores, cargos, períodos e principais responsabilidades exercidas."
            });
            deducted += 30;
        }
        else
        {
            bool hasMetrics = parsedProfile.WorkExperiences.Any(exp =>
                !string.IsNullOrWhiteSpace(exp.Description) && MetricRegex.IsMatch(exp.Description));

            if (!hasMetrics)
            {
                result.Recommendations.Add(new GapItemDto
                {
                    Category = "Experiência",
                    Severity = "medium",
                    Title = "Enriqueça suas experiências com métricas e resultados quantificáveis",
                    Description = "Suas descrições de cargo não apresentam números ou percentuais claros.",
                    ActionableRecommendation = "Adicione conquistas mensuráveis (ex: 'Aumentou a eficiência em 25%', 'Liderou equipe de 8 pessoas', 'Reduziu custos em R$ 50k')."
                });
                deducted += 10;
            }
        }

        if (parsedProfile.Skills.Count < 3)
        {
            result.MissingFields.Add("Competências Principais");
            result.Recommendations.Add(new GapItemDto
            {
                Category = "Competências",
                Severity = "low",
                Title = "Liste pelo menos 5 principais competências",
                Description = "Poucas ou nenhuma competência chave foi identificada no perfil.",
                ActionableRecommendation = "Inclua palavras-chave técnicas e soft skills essenciais para sua área de atuação."
            });
            deducted += 5;
        }

        result.CompletenessScore = Math.Max(0, totalPoints - deducted);
        return result;
    }
}
