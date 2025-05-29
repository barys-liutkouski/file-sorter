using System.CommandLine;
using FileSorter.FileIO;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Common.Models;
using Common.Helpers;
using FileSorter.ExternalSort;
using FileSorter.ExternalSort.Interfaces;
using FileSorter.ExternalSort.Core.Interfaces;
using FileSorter.ExternalSort.Core;
using FileSorter.FileIO.Interfaces;
using Common.Formatters;

var rootCommand = new RootCommand("File Sorter - Sorts large text files with \"Number. String\" format");

var inputFileOption = new Option<string>(
    name: "--input",
    description: "The input file path to sort.")
{
    IsRequired = true
};

var outputFileOption = new Option<string>(
    name: "--output",
    description: "The output file path for the sorted result.")
{
    IsRequired = true
};

var encodingOption = new Option<string>(
    name: "--encoding",
    description: "Character encoding to use for reading and writing files (utf8, ascii, utf16, utf32, or any .NET encoding name).")
{
    IsRequired = false
};
encodingOption.SetDefaultValue("utf8");

var chunkSizeOption = new Option<int>(
    name: "--chunk-size-mb",
    description: "Maximum chunk size in MB.")
{
    IsRequired = false
};
chunkSizeOption.SetDefaultValue(512);

var maxFileHandlesOption = new Option<int>(
    name: "--max-file-handles",
    description: "Maximum number of file handles to use.")
{
    IsRequired = false
};
maxFileHandlesOption.SetDefaultValue(512);

var tempDirOption = new Option<string?>(
    name: "--temp-dir",
    description: "Optional custom directory for temporary files.");

inputFileOption.AddValidator(result =>
{
    var inputPath = result.GetValueForOption(inputFileOption);
    if (string.IsNullOrWhiteSpace(inputPath))
    {
        result.ErrorMessage = "Input file path cannot be empty.";
        return;
    }

    if (!File.Exists(inputPath))
    {
        result.ErrorMessage = $"Input file '{inputPath}' does not exist.";
        return;
    }
});

outputFileOption.AddValidator(result =>
{
    var outputPath = result.GetValueForOption(outputFileOption);
    if (string.IsNullOrWhiteSpace(outputPath))
    {
        result.ErrorMessage = "Output file path cannot be empty.";
        return;
    }

    try
    {
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            result.ErrorMessage = $"Output directory '{directory}' does not exist.";
            return;
        }
    }
    catch (Exception ex)
    {
        result.ErrorMessage = $"Invalid output file path: {ex.Message}";
        return;
    }
});

rootCommand.AddOption(inputFileOption);
rootCommand.AddOption(outputFileOption);
rootCommand.AddOption(encodingOption);
rootCommand.AddOption(chunkSizeOption);
rootCommand.AddOption(maxFileHandlesOption);
rootCommand.AddOption(tempDirOption);

rootCommand.SetHandler(async (inputPath, outputPath, encodingName, chunkSizeMb, maxFileHandles, tempDir) =>
{
    var services = new ServiceCollection();

    services.AddSingleton<IFileService<ParsedLine>, FileService<ParsedLine>>();
    services.AddSingleton<ITempFileManager>(sp =>
        new TempFileManager(sp.GetRequiredService<ILogger<TempFileManager>>(), tempDir));
    services.AddTransient<IExternalSorter, ExternalSorter<ParsedLine>>();
    services.AddSingleton<IKWayMerger, KWayMerger>();
    services.AddLogging(configure =>
    {
        configure.AddConsoleFormatter<MinimalConsoleFormatter, ConsoleFormatterOptions>();
        configure.AddConsole(options => options.FormatterName = "minimal");
    });

    var encoding = EncodingHelper.ParseEncoding(encodingName);

    await using var serviceProvider = services.BuildServiceProvider();

    var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

    logger.LogInformation("Starting file sort operation...");
    logger.LogInformation("Input file: {InputFile}", inputPath);
    logger.LogInformation("Output file: {OutputFile}", outputPath);
    logger.LogInformation("Encoding: {Encoding}", encodingName);
    logger.LogInformation("Chunk Size (MB): {ChunkSizeMb}", chunkSizeMb);

    var resultsFilePath = Path.Combine(Path.GetDirectoryName(outputPath) ?? string.Empty, "sorting_results.txt");

    try
    {
        using var sorter = serviceProvider.GetRequiredService<IExternalSorter>();

        var progressStopwatch = Stopwatch.StartNew();
        var totalStopwatch = Stopwatch.StartNew();

        var peakMemory = GC.GetTotalMemory(false);
        logger.LogInformation("Initial memory usage: {InitialMemoryMb:F2} MB", peakMemory / (1024.0 * 1024.0));

        Action<string> progressCallback = (message) =>
        {
            var currentMemory = GC.GetTotalMemory(false);
            peakMemory = Math.Max(peakMemory, currentMemory);
            logger.LogInformation("{Message}. [Time: {ElapsedMilliseconds} ms] [Memory: {MemoryUsage:F2} MB]", message, progressStopwatch.ElapsedMilliseconds, currentMemory / (1024.0 * 1024.0));
            progressStopwatch.Restart();
        };

        var sortOptions = new SortOptions(
            encoding,
            progressCallback,
            chunkSizeMb,
            maxFileHandles
        );

        await sorter.SortFileAsync(inputPath, outputPath, sortOptions);

        logger.LogInformation("File sorting completed successfully!");
        var peakMemoryMb = peakMemory / (1024.0 * 1024.0);
        var totalExecutionTimeSeconds = totalStopwatch.Elapsed.TotalSeconds;
        var finalMemoryMb = GC.GetTotalMemory(false) / (1024.0 * 1024.0);

        logger.LogInformation("Peak memory usage: {PeakMemoryMb:F2} MB", peakMemoryMb);
        logger.LogInformation("Total execution time: {TotalExecutionTimeSeconds:F2} seconds", totalExecutionTimeSeconds);
        logger.LogInformation("Final memory usage: {FinalMemoryMb:F2} MB", finalMemoryMb);

        await using var resultsWriter = new StreamWriter(resultsFilePath);
        await resultsWriter.WriteLineAsync($"File sorting completed successfully!");
        await resultsWriter.WriteLineAsync($"Input file: {inputPath}");
        await resultsWriter.WriteLineAsync($"Output file: {outputPath}");
        await resultsWriter.WriteLineAsync($"Peak memory usage: {peakMemoryMb:F2} MB");
        await resultsWriter.WriteLineAsync($"Total execution time: {totalExecutionTimeSeconds:F2} seconds");
        await resultsWriter.WriteLineAsync($"Final memory usage: {finalMemoryMb:F2} MB");
        logger.LogInformation("Results saved to: {ResultsFile}", resultsFilePath);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error during file sorting: {ErrorMessage}", ex.Message);
        Environment.Exit(1);
    }
},
    inputFileOption, outputFileOption, encodingOption, chunkSizeOption, maxFileHandlesOption, tempDirOption);

return await rootCommand.InvokeAsync(args);
