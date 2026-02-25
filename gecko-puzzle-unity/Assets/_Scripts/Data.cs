public class Data
{
    // 0 = empty
    // 1 = blocked / occupied
    public static int[,] Grid =
    {
        {0, 0, 0, 0, 0, 0, 0},
        {0, 0, 0, 1, 0, 0, 0},
        {0, 0, 0, 1, 0, 0, 0},
        {0, 1, 0, 1, 0, 1, 0},
        {0, 0, 0, 1, 0, 0, 0},
        {0, 0, 0, 1, 0, 0, 0},
        {0, 0, 0, 0, 0, 0, 0},
    };

    public static float CellSize = 100f;
    public static float MoveSpeed = 800f;

    public static int Rows => Grid.GetLength(0);
    public static int Cols => Grid.GetLength(1);
}
