namespace Ducky.Sdk.Control;

/// <summary>
/// A rule that replaces the run state of a character before the game receives it. A rule can stop running or force it.
/// Not covered: forced velocity (for example rolling), knockback, aim and shooting.
/// </summary>
/// <param name="run">Whether the character runs. Changing it changes what the game receives.</param>
/// <param name="context">The character and whether it is the player, read-only.</param>
public delegate void RunInputRule(ref bool run, in ControlContext context);
