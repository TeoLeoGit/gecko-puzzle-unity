using UnityEngine;

public class GridCell : MonoBehaviour
{
    public Vector2Int GridPos { get; private set; }
    
    public void SetGridPos(Vector2Int pos)
    {
        GridPos = pos;
    }
}
