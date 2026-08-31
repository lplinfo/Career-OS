using System.IO;
using CareerOS.Api.Contracts;

namespace CareerOS.Api.Services;

public interface ILinkedinParserService
{
    ParsedCandidateProfileDto ParsePdf(Stream pdfStream);
}
