using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Game : MonoBehaviour
{
    [SerializeField] private LayerMask gridLayer; // assign in inspector
    [Header("References")]
    public GeckoController gecko;
    public Transform gridRoot;     // Parent of grid cells (7x7)

    public Grid gridComponent;
    public Camera mainCamera;

    private Vector2 dragStart;
    private Vector2 dragDir;

    private int rows = 7;
    private int cols = 7;
    private float cellSize = 100f;

    private Vector3 origin;

    // Movement
    private Vector2Int? activeTarget = null;
    private Vector2Int? pendingTarget = null;
    private List<Vector2Int> path = new();
    private int pathIndex = 0;
    private Vector3? targetWorldPos = null;

    private List<BaseExit> exits = new();

    void Awake()
    {
        origin = new Vector3(-cellSize / 2f, -cellSize / 2f, 0);

        InitGridVisual();
        gecko.Init();
    }

    void OnEnable()
    {
        GameEvents.OnExitCreated += AddExit;
    }

    void OnDisable()
    {
        GameEvents.OnExitCreated -= AddExit;
    }

    private void Update()
    {
        if (Mouse.current != null)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame) 
                OnTouchStart(Mouse.current.position.ReadValue());

            if (Mouse.current.leftButton.isPressed)
                OnTouchMove(Mouse.current.position.ReadValue());

            if (Mouse.current.leftButton.wasReleasedThisFrame)
                OnTouchEnd();
        }

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            var touch = Touchscreen.current.primaryTouch;

            if (touch.press.wasPressedThisFrame)
                OnTouchStart(touch.position.ReadValue());

            if (touch.press.isPressed)
                OnTouchMove(touch.position.ReadValue());

            if (touch.press.wasReleasedThisFrame)
                OnTouchEnd();
        }

        if (targetWorldPos.HasValue)
            UpdateDragGecko(Time.deltaTime);
    }

    #region Grid Setup

    void InitGridVisual()
    {
        gridComponent.Init();
        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < cols; x++)
            {
                if (Data.Grid[y, x] == 1)
                {
                    int index = y * cols + x;
                    gridRoot.GetChild(index).gameObject.SetActive(false);
                }
            }
        }
    }

    #endregion

    #region Input

    void OnTouchStart(Vector2 screenPos)
    {
        dragStart = screenPos;
        Vector2Int? point = GetPointAtScreenPos(screenPos);
        if (point.HasValue)
        {
            ChooseMoveNode(point.Value);
            //Debug.Log("Chosen move node at: " + point.Value);
            MoveGeckoToPoint(point.Value);
        }
    }

    void OnTouchMove(Vector2 screenPos)
    {
        if (gecko.IsMoving) return;

        dragDir = screenPos - dragStart;

        Vector2Int? point = GetPointAtScreenPos(screenPos);
        if (!point.HasValue) return;

        MoveGeckoToPoint(point.Value);
    }

    void OnTouchEnd()
    {
        dragDir = Vector2.zero;
        gecko.SetBackwardsMovement(false);
    }

    #endregion

    #region Movement

    void UpdateDragGecko(float deltaTime)
    {
        float remaining = Data.MoveSpeed * deltaTime;

        while (remaining > 0 && targetWorldPos.HasValue)
        {
            Vector3 current = gecko.MoveNode.position;
            Vector3 toTarget = targetWorldPos.Value - current;
            float dist = toTarget.magnitude;

            gecko.LookAt2D(targetWorldPos.Value);

            if (dist <= remaining)
            {
                gecko.MoveTo(targetWorldPos.Value);
                remaining -= dist;

                pathIndex++;
                if (pathIndex >= path.Count)
                    activeTarget = null;

                CommitPendingTargetIfAny();
                MoveToNextCell();
            }
            else
            {
                Vector3 nextPos = current + toTarget.normalized * remaining;
                gecko.MoveTo(nextPos);
                remaining = 0;
            }
        }
    }

    void CommitPendingTargetIfAny()
    {
        if (!pendingTarget.HasValue) return;

        activeTarget = pendingTarget;
        pendingTarget = null;

        MoveGeckoOnPath(gecko.MovePoint, activeTarget.Value);
    }

    void MoveGeckoToPoint(Vector2Int targetPoint)
    {
        if (Data.Grid[targetPoint.y, targetPoint.x] == 1) return;
        if (gecko.MovePoint == targetPoint) return;

        if (!activeTarget.HasValue)
        {
            activeTarget = targetPoint;
            MoveGeckoOnPath(gecko.MovePoint, targetPoint);
        }
        else
        {
            pendingTarget = targetPoint;
        }
    }

    void MoveGeckoOnPath(Vector2Int start, Vector2Int target)
    {
        AStar astar = new AStar(Data.Grid);
        List<Vector2Int> newPath = astar.FindPath(start, target);

        if (newPath.Count == 0)
        {
            targetWorldPos = null;
            activeTarget = null;
            return;
        }

        path = newPath;
        pathIndex = 0;
        MoveToNextCell();
    }

    void MoveToNextCell()
    {
        if (pathIndex >= path.Count)
        {
            targetWorldPos = null;
            activeTarget = null;
            return;
        }

        Vector2Int p = path[pathIndex];

        targetWorldPos = GridToWorld(p);

        gecko.UpdateTrail(p);
        gecko.UpdateSegmentTargets();

        // BaseExit exit = FindExitAtPoint(p);
        // if (exit != null)
        //     RemoveGeckoAtExit(exit);
    }

    void RemoveGeckoAtExit(BaseExit exit)
    {
        path.RemoveRange(pathIndex, path.Count - pathIndex);
        gecko.OnEnterExit();
        exit.OnGeckoEnter();
    }

    #endregion

    #region Grid Utilities

    Vector2Int? GetPointAtScreenPos(Vector2 screenPos)
    {
        return FindCellAt(screenPos);
    }

    Vector2Int? FindCellAt(Vector2 screenPos)
    {
        Ray ray = mainCamera.ScreenPointToRay(screenPos);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, gridLayer))
        {
            var cell = hit.collider.GetComponent<GridCell>();
            if (cell != null)
            {
                return cell.GridPos;
            }
        }

        return null;
    }

    Vector3 GridToWorld(Vector2Int p)
    {
        int index = p.y * cols + p.x;
        return gridRoot.GetChild(index).position;
    }

    #endregion

    #region Exits

    public void AddExit(BaseExit exit)
    {
        exits.Add(exit);
    }

    BaseExit FindExitAtPoint(Vector2Int point)
    {
        foreach (var exit in exits)
            if (exit.Point == point)
                return exit;

        return null;
    }

    void ChooseMoveNode(Vector2Int point)
    {
        gecko.DetermineMovementDirection(point);
    }

    #endregion
}