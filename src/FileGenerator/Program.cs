using System.CommandLine;
using System.Text;
using Common.Formatters;
using FileGenerator;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

var rootCommand = new RootCommand("File Generator");

var fileSizeOption = new Option<long>(
    name: "--size",
    description: "The size of the file to generate in megabytes.")
{
    IsRequired = true
};

var outputFileOption = new Option<string>(
    name: "--output",
    description: "The output file path.")
{
    IsRequired = false
};
outputFileOption.SetDefaultValue("output.txt");

var stringReuseChanceOption = new Option<double>(
    name: "--string-reuse-chance",
    description: "The chance (0.0 to 1.0) of reusing a string from the pool.");
stringReuseChanceOption.SetDefaultValue(0.01);
stringReuseChanceOption.AddValidator(result =>
{
    var value = result.GetValueForOption(stringReuseChanceOption);
    if (value < 0.0 || value > 1.0)
    {
        result.ErrorMessage = "String reuse chance must be between 0.0 and 1.0.";
    }
});

fileSizeOption.AddValidator(result =>
{
    if (result.GetValueForOption(fileSizeOption) <= 0)
    {
        result.ErrorMessage = "File size must be a positive number.";
    }
});

rootCommand.AddOption(fileSizeOption);
rootCommand.AddOption(outputFileOption);
rootCommand.AddOption(stringReuseChanceOption);

rootCommand.SetHandler(async (size, outputPath, stringReuseChance) =>
{
    using var loggerFactory = LoggerFactory.Create(builder =>
    {
        builder.AddConsoleFormatter<MinimalConsoleFormatter, ConsoleFormatterOptions>();
        builder.AddConsole(options =>
        {
            options.FormatterName = "minimal";
        });
    });
    ILogger<Program> logger = loggerFactory.CreateLogger<Program>();

    logger.LogInformation("Generating a file of size: {FileSize:N0} megabytes with string reuse chance: {StringReuseChance}", size, stringReuseChance);

    using var writer = new StreamWriter(outputPath, false, Encoding.UTF8, bufferSize: 4 * 1024 * 1024);
    ILogger<RandomFileGenerator> fileGeneratorLogger = loggerFactory.CreateLogger<RandomFileGenerator>();
    var generator = new RandomFileGenerator(fileGeneratorLogger, writer, stringReuseChance);
    await generator.GenerateFile(size * 1000000);

    logger.LogInformation("File generation completed. Output file: {OutputFile}", outputPath);
},
    fileSizeOption, outputFileOption, stringReuseChanceOption);

return rootCommand.Invoke(args);
