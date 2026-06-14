namespace TheAdventure.Exceptions;

public class AssetLoadException : Exception
{
    public AssetLoadException(string message) : base(message) { }

    public AssetLoadException(string message, Exception inner) : base(message, inner) { }
}
