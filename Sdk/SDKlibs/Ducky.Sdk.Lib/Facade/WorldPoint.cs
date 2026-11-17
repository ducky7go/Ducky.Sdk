namespace Ducky.Sdk.Facade;

/// <summary>A point in the game world. Maps the game's <c>Vector3</c> (x, y, z) one to one.</summary>
public readonly record struct WorldPoint(float X, float Y, float Z);
