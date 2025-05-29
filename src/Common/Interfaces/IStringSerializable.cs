using System.Text;

namespace Common.Interfaces;

public interface IStringSerializable<T>
{
    string ToString();
    static abstract bool TryParse(string value, Encoding encoding, out T result, out string parseError);
}
