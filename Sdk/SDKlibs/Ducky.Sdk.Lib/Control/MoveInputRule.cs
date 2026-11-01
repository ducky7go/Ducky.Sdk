using UnityEngine;

namespace Ducky.Sdk.Control;

/// <summary>
/// A rule that replaces the move input of a character before the game receives it. A rule can stop a character with a zero
/// vector, or force a direction, even though the input script writes the input every frame. Not covered: forced velocity
/// (for example rolling by <c>SetForceMoveVelocity</c>), knockback, aim and shooting.
/// </summary>
/// <param name="input">The move input. Changing it changes what the game receives.</param>
/// <param name="context">The character and whether it is the player, read-only.</param>
public delegate void MoveInputRule(ref Vector3 input, in ControlContext context);
