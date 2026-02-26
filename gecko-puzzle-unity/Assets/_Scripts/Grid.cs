using UnityEngine;

public class Grid : MonoBehaviour
{
    [SerializeField] private GridCell cellPrefab;

    public void Init()
    {
        for (int row = 0; row < Data.Rows; row++)
        {
            for (int col = 0; col < Data.Cols; col++)
            {
                GridCell cell = Instantiate(cellPrefab, transform);
                cell.transform.localPosition = new Vector3(col * Data.CellSize, row * Data.CellSize, 0);
                cell.SetGridPos(new Vector2Int(col, row));
                cell.name = $"Cell_{col}_{row}";
            }
        }
    }
}
