using System.CommandLine;
using Common.Helpers;
using Common.Formatters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using SortAuditor.Auditing;

var rootCommand = new RootCommand("File Verifier - Verifies that a sorted file is properly ordered and matches original line count.");

var sortedFileOption = new Option<string>(
    name: "--sorted-file",
    description: "The path to the sorted file to verify.")
{
    IsRequired = true
};

var originalFileOption = new Option<string>(
    name: "--original-file",
    description: "The path to the original file to compare line counts against.")
{
    IsRequired = true
};

var encodingOption = new Option<string>(
    name: "--encoding",
    description: "Character encoding to use for reading the files (utf8, ascii, utf16, utf32, or any .NET encoding name).")
{
    IsRequired = false
};
encodingOption.SetDefaultValue("utf8");

var verboseOption = new Option<bool>(
    name: "--verbose",
    description: "Enable verbose output (sets logger to Debug level).",
    getDefaultValue: () => false
);

sortedFileOption.AddValidator(result =>
{
    var filePath = result.GetValueForOption(sortedFileOption);
    if (string.IsNullOrWhiteSpace(filePath))
    {
        result.ErrorMessage = "Sorted file path cannot be empty.";
        return;
    }

    if (!File.Exists(filePath))
    {
        result.ErrorMessage = $"Sorted file '{filePath}' does not exist.";
        return;
    }
});

originalFileOption.AddValidator(result =>
{
    var filePath = result.GetValueForOption(originalFileOption);
    if (string.IsNullOrWhiteSpace(filePath))
    {
        result.ErrorMessage = "Original file path cannot be empty.";
        return;
    }

    if (!File.Exists(filePath))
    {
        result.ErrorMessage = $"Original file '{filePath}' does not exist.";
        return;
    }
});

rootCommand.AddOption(sortedFileOption);
rootCommand.AddOption(originalFileOption);
rootCommand.AddOption(encodingOption);
rootCommand.AddOption(verboseOption);

rootCommand.SetHandler(async (sortedFilePath, originalFilePath, encodingName, verbose) =>
{
    using var loggerFactory = LoggerFactory.Create(builder =>
    {
        builder.AddConsoleFormatter<MinimalConsoleFormatter, ConsoleFormatterOptions>();
        builder.AddConsole(options =>
        {
            options.FormatterName = "minimal";
        });
        builder.SetMinimumLevel(verbose ? LogLevel.Debug : LogLevel.Information);
    });
    var logger = loggerFactory.CreateLogger("SortAuditor");

    logger.LogInformation("Starting file verification...");
    logger.LogInformation("Sorted file: {SortedFilePath}", sortedFilePath);
    logger.LogInformation("Original file: {OriginalFilePath}", originalFilePath);
    logger.LogInformation("Encoding: {EncodingName}", encodingName);
    logger.LogInformation("Log level: {LogLevel}", verbose ? LogLevel.Debug : LogLevel.Information);

    try
    {
        var encoding = EncodingHelper.ParseEncoding(encodingName);
        var verifier = new SortOrderAuditor(encoding, logger);
        var result = await verifier.VerifyAsync(originalFilePath, sortedFilePath);

        logger.LogInformation("=== VERIFICATION RESULTS ===");
        logger.LogInformation("Sorted file lines processed: {SortedFileLinesProcessed}", result.SortedFileLinesProcessed);
        logger.LogInformation("Original file lines processed: {OriginalFileLinesProcessed}", result.OriginalFileLinesProcessed);

        if (result.SortedFileLinesProcessed == result.OriginalFileLinesProcessed)
        {
            logger.LogInformation("Line counts match between original and sorted files.");
        }
        else
        {
            logger.LogInformation("ERROR: Line count mismatch. Original: {OriginalFileLinesProcessed}, Sorted: {SortedFileLinesProcessed}", result.OriginalFileLinesProcessed, result.SortedFileLinesProcessed);
            logger.LogError("Line count mismatch. Original: {OriginalFileLinesProcessed}, Sorted: {SortedFileLinesProcessed}", result.OriginalFileLinesProcessed, result.SortedFileLinesProcessed);
        }

        logger.LogInformation("File is properly sorted: {IsProperlySorted}", result.IsProperlySorted ? "YES" : "NO");
        logger.LogInformation("Verification time: {VerificationTimeTotalSeconds} seconds", result.VerificationTime.TotalSeconds);

        if (result.FoundDuplicateStringPart)
        {
            logger.LogInformation("Found at least one instance of duplicate string parts in consecutive lines: YES");
        }
        else
        {
            logger.LogError("ERROR: Verification failed - No duplicate string parts found in consecutive lines.");
        }

        bool lineCountsMatch = result.SortedFileLinesProcessed == result.OriginalFileLinesProcessed;

        if (!result.IsProperlySorted)
        {
            logger.LogInformation("=== SORTING VIOLATION ===");
            logger.LogInformation("Violation at sorted file line: {ViolationLineNumber}", result.ViolationLineNumber);
            logger.LogInformation("Previous line ({ViolationLineNumberPrevious}): {PreviousLineContent}", result.ViolationLineNumber - 1, result.PreviousLineContent);
            logger.LogInformation("Current line  ({ViolationLineNumberCurrent}): {CurrentLineContent}", result.ViolationLineNumber, result.CurrentLineContent);
            logger.LogError("Sorting violation at sorted file line: {ViolationLineNumber}. Previous: '{PreviousLineContent}', Current: '{CurrentLineContent}'.", result.ViolationLineNumber, result.PreviousLineContent, result.CurrentLineContent);
            Environment.Exit(1);
        }
        else if (!lineCountsMatch)
        {
            Environment.Exit(1);
        }
        else if (!result.FoundDuplicateStringPart)
        {
            Environment.Exit(1);
        }
        else
        {
            logger.LogInformation("File verification completed successfully - file is properly sorted, line counts match, and duplicate string part check passed!");
            Environment.Exit(0);
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error during file verification.");
        logger.LogInformation("Error during file verification: {ErrorMessage}", ex.Message);
        if (verbose)
        {
            logger.LogInformation("{StackTrace}", ex.StackTrace);
        }
        Environment.Exit(1);
    }
},
    sortedFileOption, originalFileOption, encodingOption, verboseOption);

return await rootCommand.InvokeAsync(args);
