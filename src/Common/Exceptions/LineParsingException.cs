namespace Common.Exceptions;

public class LineParsingException : Exception
{
    public LineParsingException() { }
    public LineParsingException(string message) : base(message) { }
    public LineParsingException(string message, System.Exception inner) : base(message, inner) { }
}
