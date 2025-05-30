using System.Text;
using Microsoft.Extensions.Logging;

namespace FileGenerator;

public class RandomFileGenerator
{
    private readonly Random _random = new Random();
    private readonly List<StringBuilder> _stringPool = new();
    private readonly string[] _words =
    {
        "apple",
        "banana",
        "cherry",
        "date",
        "elderberry",
        "fig",
        "grape",
        "honeydew",
        "kiwi",
        "lemon",
        "mango",
        "nectarine",
        "orange",
        "papaya",
        "quince",
        "raspberry",
        "strawberry",
        "tangerine",
        "ugli",
        "vanilla",
        "watermelon",
        "yam",
        "zucchini",
        "amazing",
        "brilliant",
        "creative",
        "dynamic",
        "energetic",
        "fantastic",
        "gorgeous",
        "helpful",
        "incredible",
        "joyful",
        "kind",
        "lovely",
        "magnificent",
        "natural",
        "outstanding",
        "powerful",
        "quick",
        "radiant",
        "stunning",
        "tremendous",
        "unique",
        "vibrant",
        "wonderful",
        "excellent",
        "beautiful",
        "charming",
        "delightful",
        "elegant",
        "fabulous",
        "graceful",
        "harmonious",
        "inspiring",
    };
    private readonly char[] chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray();

    private const int StringPoolSize = 1000;
    private readonly double _stringReuseChance;
    private readonly StreamWriter _writer;
    private readonly ILogger<RandomFileGenerator> _logger;

    public RandomFileGenerator(ILogger<RandomFileGenerator> logger, StreamWriter writer, double stringReuseChance)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        _stringReuseChance = stringReuseChance;
    }

    public async Task GenerateFile(long targetSize)
    {
        long currentSize = 0;
        var lines = 0;

        while (currentSize < targetSize)
        {
            var line = GenerateLine();
            lines++;
            await _writer.WriteLineAsync(line);

            currentSize += line.Length + Environment.NewLine.Length;

            if (lines % 5000000 == 0)
            {
                _logger.LogInformation(
                    "Generated {CurrentSize:N0}mb of {TargetSize:N0}",
                    currentSize / 1024 / 1024,
                    targetSize / 1024 / 1024
                );
            }
        }
    }

    public StringBuilder GenerateLine()
    {
        var number = GenerateNumber();
        var text = GenerateString();
        return new StringBuilder($"{number}").Append('.').Append(' ').Append(text);
    }

    private long GenerateNumber()
    {
        var value = _random.NextInt64();
        if (_random.NextDouble() < 0.01)
        {
            value *= -1;
        }
        return value;
    }

    private StringBuilder GenerateString()
    {
        if (_stringPool.Count > 0 && GetRandomDouble() < _stringReuseChance)
        {
            return _stringPool[GetRandomInt(_stringPool.Count)];
        }

        var newString = CreateRandomString();

        if (_stringPool.Count < StringPoolSize)
        {
            _stringPool.Add(newString);
        }

        return newString;
    }

    private StringBuilder CreateRandomString()
    {
        var length = GetRandomInt(10, 1025);
        var sb = new StringBuilder(length);

        var wordCount = GetRandomInt(1, Math.Min(10, length / 5 + 1));

        for (int i = 0; i < wordCount && sb.Length < length; i++)
        {
            if (i > 0 && sb.Length < length - 1)
            {
                sb.Append(' ');
            }

            var word = _words[GetRandomInt(_words.Length)];
            var remainingLength = length - sb.Length;

            if (word.Length <= remainingLength)
            {
                sb.Append(word);
            }
            else
            {
                sb.Append(word.AsSpan(0, remainingLength));
                break;
            }
        }

        while (sb.Length < length)
        {
            sb.Append(chars[GetRandomInt(chars.Length)]);
        }

        return sb;
    }

    private double GetRandomDouble()
    {
        return _random.NextDouble();
    }

    private int GetRandomInt(int maxValue)
    {
        if (maxValue <= 0)
        {
            return 0;
        }
        return _random.Next(maxValue);
    }

    private int GetRandomInt(int minValue, int maxValue)
    {
        if (minValue >= maxValue)
        {
            return minValue;
        }
        return _random.Next(minValue, maxValue);
    }
}
