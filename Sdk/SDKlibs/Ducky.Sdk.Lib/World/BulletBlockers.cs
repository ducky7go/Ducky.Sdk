using System;
using UnityEngine;

namespace Ducky.Sdk.World;

/// <summary>
/// Spawns a wall that stops bullets according to the game's team rules: a bullet is stopped when its team is hostile to the
/// wall's team.
/// </summary>
public static class BulletBlockers
{
    /// <summary>
    /// Spawns a bullet-blocking wall of <paramref name="width"/> × <paramref name="height"/> × <paramref name="thickness"/> at
    /// <paramref name="position"/> with <paramref name="rotation"/>, on the game's BlockBullet layer, with the given team. Returns the
    /// object; the caller owns its lifetime and must destroy it, for example on hot reload.
    /// </summary>
    /// <remarks>
    /// The wall has no health and cannot be destroyed by shooting; a bullet that hits it disappears without a damage notification.
    /// It is an ordinary scene object. A wall of an enemy team stops the bullets of the player and of other hostile teams, and a
    /// wall of the player team stops enemy bullets. Whether it blocks character movement is <b>unverified</b>. Main thread only.
    /// </remarks>
    /// <exception cref="ArgumentException">A dimension is not a number.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A dimension is infinite or not positive.</exception>
    /// <exception cref="InvalidOperationException">The game has no BlockBullet layer; nothing is spawned.</exception>
    public static GameObject Spawn(Vector3 position, Quaternion rotation, float width, Teams team,
        float height = 2f, float thickness = 0.3f)
    {
        BlockerGeometry.Validate(width, height, thickness);

        var layer = LayerMask.NameToLayer("BlockBullet");
        if (layer < 0)
        {
            throw new InvalidOperationException("The game has no BlockBullet layer, so the bullet blocker cannot be spawned.");
        }

        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.transform.SetPositionAndRotation(position, rotation);
        wall.transform.localScale = new Vector3(width, height, thickness);
        wall.layer = layer;

        var blocker = wall.AddComponent<BulletBlocker>();
        blocker.team = team;
        return wall;
    }
}
