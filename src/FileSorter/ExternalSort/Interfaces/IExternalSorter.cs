namespace FileSorter.ExternalSort.Interfaces;

public interface IExternalSorter : IDisposable
{
    Task SortFileAsync(string inputFilePath, string outputFilePath, SortOptions sortOptions);
}


