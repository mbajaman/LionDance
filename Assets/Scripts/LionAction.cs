using UnityEngine;

/// <summary>
/// Discrete intent a player can express in a single press: either a step in
/// a world direction, or a jump. Used by <see cref="PlayerController"/> to
/// announce input and by <see cref="LionCoordinator"/> to require both halves
/// of the lion to agree before any action actually happens.
/// </summary>
public readonly struct LionAction
{
    public enum Kind
    {
        None,
        Step,
        Jump
    }

    public readonly Kind ActionKind;
    public readonly Vector3 Direction;

    private LionAction(Kind kind, Vector3 direction)
    {
        ActionKind = kind;
        Direction = direction;
    }

    public static LionAction Step(Vector3 direction) => new LionAction(Kind.Step, direction);
    public static LionAction Jump() => new LionAction(Kind.Jump, Vector3.zero);
    public static readonly LionAction None = new LionAction(Kind.None, Vector3.zero);

    public bool IsValid => ActionKind != Kind.None;

    public bool Matches(LionAction other)
    {
        if (ActionKind != other.ActionKind) return false;
        if (ActionKind == Kind.Step) return Direction == other.Direction;
        return true;
    }

    public override string ToString()
    {
        switch (ActionKind)
        {
            case Kind.Jump:
                return "Jump";
            case Kind.Step:
                return $"Step {FormatDirection(Direction)}";
            default:
                return "None";
        }
    }

    private static string FormatDirection(Vector3 v)
    {
        if (v == Vector3.forward) return "Forward (+Z)";
        if (v == Vector3.back) return "Back (-Z)";
        if (v == Vector3.right) return "Right (+X)";
        if (v == Vector3.left) return "Left (-X)";
        return v.ToString();
    }
}
