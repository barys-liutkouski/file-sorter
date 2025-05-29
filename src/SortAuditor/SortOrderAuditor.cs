using System.Text;
using System.Diagnostics;
using Common.Models;
using Microsoft.Extensions.Logging;

namespace SortAuditor.Auditing;

public class SortOrderAuditor
{
    private readonly Encoding _encoding;
    private readonly ILogger _logger;

    public SortOrderAuditor(Encoding encoding, ILogger logger)
    {
        _encoding = encoding ?? throw new ArgumentNullException(nameof(encoding));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<VerificationResult> VerifyAsync(string originalFilePath, string sortedFilePath)
    {
        var stopwatch = Stopwatch.StartNew();
        long originalFileLines = 0;
        long sortedFileLines = 0;
        bool isProperlySorted = true;
        long violationLineNumber = -1;
        string? previousLineContent = null;
        string? currentLineContent = null;
        bool foundDuplicateStringPart = false;

        _logger.LogInformation("Counting lines in original file: {OriginalFilePath}", originalFilePath);
        try
        {
            using var originalStreamReader = new StreamReader(originalFilePath, _encoding);
            while (await originalStreamReader.ReadLineAsync() != null)
            {
                originalFileLines++;
            }
            _logger.LogInformation("Original file contains {OriginalFileLines} lines.", originalFileLines);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading original file {OriginalFilePath}.", originalFilePath);
            throw;
        }

        _logger.LogInformation("Verifying sorted file: {SortedFilePath}", sortedFilePath);
        ParsedLine? previousParsedLine = null;

        try
        {
            using var streamReader = new StreamReader(sortedFilePath, _encoding);
            string? line;
            while ((line = await streamReader.ReadLineAsync()) != null)
            {
                sortedFileLines++;
                if (string.IsNullOrWhiteSpace(line))
                {
                    _logger.LogWarning("Encountered empty line at sorted file line {SortedFileLines}. Skipping.", sortedFileLines);
                    continue;
                }

                if (ParsedLine.TryParse(line, _encoding, out var currentParsedLine, out var error))
                {
                    if (sortedFileLines % 100000 == 0)
                    {
                        _logger.LogDebug("Processed {SortedFileLines} lines from sorted file...", sortedFileLines);
                    }

                    if (previousParsedLine.HasValue)
                    {
                        if (currentParsedLine.CompareTo(previousParsedLine.Value) < 0)
                        {
                            isProperlySorted = false;
                            violationLineNumber = sortedFileLines;
                            previousLineContent = previousParsedLine.Value.ToString();
                            currentLineContent = currentParsedLine.ToString();
                            _logger.LogError("Sorting violation at line {ViolationLineNumber}. Previous: {PreviousLineContent}, Current: {CurrentLineContent}.", violationLineNumber, previousLineContent, currentLineContent);

                            break;
                        }
                        if (currentParsedLine.Text == previousParsedLine.Value.Text)
                        {
                            foundDuplicateStringPart = true;
                        }
                    }
                    previousParsedLine = currentParsedLine;
                }
                else
                {

                    isProperlySorted = false;
                    violationLineNumber = sortedFileLines;
                    previousLineContent = previousParsedLine?.ToString() ?? "N/A (first or previous was invalid)";
                    currentLineContent = line;
                    var errorMessage = error;
                    _logger.LogError("Could not parse line {SortedFileLines} in sorted file: {LineContent}. Reason: {ErrorMessage}", sortedFileLines, line, errorMessage);
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading or processing sorted file {SortedFilePath}.", sortedFilePath);
            throw;
        }

        stopwatch.Stop();
        _logger.LogInformation("Verification completed in {ElapsedSeconds:F2} seconds.", stopwatch.Elapsed.TotalSeconds);

        return new VerificationResult(
            OriginalFileLinesProcessed: originalFileLines,
            SortedFileLinesProcessed: sortedFileLines,
            IsProperlySorted: isProperlySorted,
            VerificationTime: stopwatch.Elapsed,
            ViolationLineNumber: violationLineNumber,
            PreviousLineContent: previousLineContent,
            CurrentLineContent: currentLineContent,
            FoundDuplicateStringPart: foundDuplicateStringPart
        );
    }
}

public record VerificationResult(
    long OriginalFileLinesProcessed,
    long SortedFileLinesProcessed,
    bool IsProperlySorted,
    TimeSpan VerificationTime,
    long ViolationLineNumber = -1,
    string? PreviousLineContent = null,
    string? CurrentLineContent = null,
    bool FoundDuplicateStringPart = false
);
