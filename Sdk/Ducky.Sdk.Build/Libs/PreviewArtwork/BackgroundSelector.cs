using System;
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

/// <summary>
/// Chooses one of the ten bundled preview backgrounds from the project name.
/// The choice uses SHA-256 rather than string.GetHashCode, which is randomised per process on .NET.
/// </summary>
internal static class BackgroundSelector
{
    /// <summary>Number of bundled backgrounds (Assets/PreviewBackgrounds/background-00.png to background-09.png).</summary>
    public const int Count = 10;

    /// <summary>Background index (0 to Count - 1) for a project name. The same name always gives the same index.</summary>
    public static int IndexFor(string name)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(name));
        return (int)(BinaryPrimitives.ReadUInt32BigEndian(hash) % (uint)Count);
    }

    /// <summary>Logical manifest resource name of a background, as set by the EmbeddedResource item in the csproj.</summary>
    public static string ResourceNameFor(int index) => $"PreviewBackgrounds/background-{index:00}.png";
}
