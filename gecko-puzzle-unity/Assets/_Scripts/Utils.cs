using UnityEngine;

public static class Utils
{
    public static float NormalizeAngle(float a)
    {
        a %= 360f;
        return a < 0 ? a + 360f : a;
    }

    public static float LerpAngle(float a, float b, float t)
    {
        float delta = ((b - a + 540f) % 360f) - 180f;
        return NormalizeAngle(a + delta * t);
    }

    public static float FlipAngle180(float angle)
    {
        angle += 180f;
        angle %= 360f;
        if (angle > 180f)
            angle -= 360f;

        return angle;
    }

    public static int Manhattan(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }
}