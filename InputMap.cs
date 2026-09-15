using UnityEngine;

public static class InputMap
{
    public const KeyCode Left     = KeyCode.A;
    public const KeyCode Right    = KeyCode.D;
    public const KeyCode Up       = KeyCode.W;
    public const KeyCode Down     = KeyCode.S;

    public const KeyCode Jump     = KeyCode.Space;
    public const KeyCode Dash     = KeyCode.LeftShift;
    public const KeyCode Attack   = KeyCode.J;
    public const KeyCode Parry    = KeyCode.K;
    public const KeyCode Portal   = KeyCode.L;
    public const KeyCode Backflip = KeyCode.U;
    public const KeyCode Taunt    = KeyCode.T;

    public static Vector2 MoveAxis()
    {
        float x = (Input.GetKey(Right) ? 1 : 0) - (Input.GetKey(Left) ? 1 : 0);
        float y = (Input.GetKey(Up)    ? 1 : 0) - (Input.GetKey(Down) ? 1 : 0);
        return new Vector2(x, y);
    }
}
