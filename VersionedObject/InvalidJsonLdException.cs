namespace VersionedObject;
/// <summary>
/// Thrown when GraphEntityComparer sees invalid input
/// </summary>
public class InvalidJsonLdException : Exception
{
    public InvalidJsonLdException(string message) : base(message)
    {
    }
}