using System.Text;

namespace FileSorter.ExternalSort;

public record SortOptions(
    Encoding Encoding,
    Action<string> ProgressCallback,
    int MaxChunkSizeMB,
    int MaxFileHandles,
    int FileBufferSize
);
