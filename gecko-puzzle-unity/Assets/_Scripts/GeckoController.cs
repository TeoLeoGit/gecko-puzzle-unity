using System.Collections.Generic;
using UnityEngine;
using System;

public class GeckoController : MonoBehaviour
{
    [Header("Body Parts")]
    public List<Transform> parts = new();
    public Transform headNode;
    public Transform tailNode;
    public Transform legsNode;

    [Serializable]
    struct SegmentTarget
    {
        public Vector3 pos;
        public float angle;

        public SegmentTarget(Vector3 p, float a)
        {
            pos = p;
            angle = a;
        }
    }

    private Vector2Int headPoint = new(0, 0);
    private Vector2Int tailPoint = new(0, 3);

    private Vector2Int movePoint;
    private Vector2Int endPoint;

    private Vector2Int backwardPoint1;
    private Vector2Int backwardPoint2;

    private List<Vector2Int> trail = new()
    {
        new Vector2Int(0,0),
        new Vector2Int(0,1),
        new Vector2Int(0,2),
        new Vector2Int(0,3),
    };

    [SerializeField] private List<Transform> segments = new();
    //private List<SkeletonAnimation> legSkeletons = new();
    [SerializeField] private List<SegmentTarget> segmentTargets = new();

    private Transform moveNode;
    private Transform endNode;
    private Transform nodeSegments;

    private bool isMovingHead = true;
    private bool isMoving = false;
    private bool isBackwards = false;
    private bool isEnterExit = false;

    private const int segmentsEachPart = 7;

    public Vector2Int MovePoint => movePoint;
    public Transform MoveNode => moveNode;
    public bool IsBackwards => isBackwards;
    public bool IsMoving => isMoving;

    #region Unity Lifecycle

    public void Init()
    {
        MarkOccupiedOnTrail();

        nodeSegments = transform.Find("Segments");

        if (nodeSegments == null)
        {
            Debug.LogError("segments object not found!");
            return;
        }

        // Collect segments from parts (excluding first & last)
        for (int i = 1; i < parts.Count - 1; i++)
        {
            foreach (Transform segment in parts[i])
            {
                segments.Add(segment);
            }
        }

        // Re-parent segments to nodeSegments (keep world position)
        foreach (var segment in segments)
        {
            segment.SetParent(nodeSegments, true);
        }

        // Reverse sibling order (like Cocos setSiblingIndex)
        for (int i = 0; i < segments.Count; i++)
        {
            segments[i].SetSiblingIndex(segments.Count - 1 - i);
        }
        InitSegmentTargets();
    }

    void Start()
    {
        movePoint = headPoint;
        endPoint = tailPoint;
        moveNode = headNode;
        endNode = tailNode;
    }

    void Update()
    {
        if (isEnterExit) return;
        if (segmentTargets.Count <= segments.Count) return;

        bool done = MoveSegmentsToTargets(Data.MoveSpeed, Time.deltaTime);

        if (done)
        {
            isMoving = false;
            ConsumeTailTargets(segmentsEachPart);
        }

        AnimateLegs();
    }

    #endregion

    #region Movement

    public void MoveTo(Vector3 worldPos)
    {
        if (isEnterExit) return;

        isMoving = true;
        moveNode.position = worldPos;
        MoveEndNode(Data.MoveSpeed, Time.deltaTime);
    }

    void MoveEndNode(float speed, float dt)
    {
        int n = parts.Count;
        Vector3 curPos = endNode.position;
        Vector3 tgtPos = new(
            trail[n - 1].x * Data.CellSize,
            trail[n - 1].y * Data.CellSize,
            0
        );

        float dist = Vector3.Distance(curPos, tgtPos);

        if (dist > 0.001f)
        {
            float t = Mathf.Min(1, speed * dt / dist);
            endNode.position = Vector3.Lerp(curPos, tgtPos, t);

            float curAngle = endNode.eulerAngles.z;
            float delta = endNode == tailNode ? 0 : 180;
            float targetAngle = segments[^1].eulerAngles.z + delta;

            float nextAngle = Mathf.LerpAngle(curAngle, targetAngle, t);
            endNode.rotation = Quaternion.Euler(0, 0, nextAngle);
        }
        else
        {
            endNode.position = tgtPos;
        }
    }

    public void UpdateSegmentTargets()
    {
        if (trail[2].x != trail[0].x &&
            trail[2].y != trail[0].y)
        {
            var newTargets = BendLCurveTargets(
                trail[0],
                trail[1],
                trail[2],
                segmentsEachPart,
                parts[1]
            );

            segmentTargets.InsertRange(0, newTargets);
        }
        else
        {
            List<SegmentTarget> newTargets = new List<SegmentTarget>();

            if (trail[2].x == trail[0].x)
            {
                float dy = trail[0].y - trail[1].y;

                for (int i = 0; i < segmentsEachPart; i++)
                {
                    Vector3 localOffset = new Vector3(
                        0,
                        dy > 0 ? (0.45f - i * 0.15f) : (-0.45f + i * 0.15f),
                        0
                    );

                    Vector3 worldPos = parts[1].TransformPoint(localOffset);

                    newTargets.Add(new SegmentTarget
                    {
                        pos = worldPos,
                        angle = segments[0].rotation.eulerAngles.z
                    });
                }
            }
            else
            {
                float dx = trail[0].x - trail[1].x;

                for (int i = 0; i < segmentsEachPart; i++)
                {
                    Vector3 localOffset = new Vector3(
                        dx > 0 ? (0.45f - i * 0.15f) : (-0.45f + i * 0.15f),
                        0,
                        0
                    );

                    Vector3 worldPos = parts[1].TransformPoint(localOffset);

                    newTargets.Add(new SegmentTarget
                    {
                        pos = worldPos,
                        angle = segments[0].rotation.eulerAngles.z
                    });
                }
            }

            segmentTargets.InsertRange(0, newTargets);
        }
    }

    private List<SegmentTarget> BendLCurveTargets(
        Vector2Int from,
        Vector2Int curr,
        Vector2Int target,
        int N,
        Transform parent
    )
    {
        float half = 0.5f * Data.CellSize;
        List<SegmentTarget> targets = new List<SegmentTarget>();

        float dx = target.x - from.x;
        float dy = target.y - from.y;
        float dx_fc = from.x - curr.x;

        float cx = 0f, cy = 0f;
        float startAngle = 0f;
        float delta90 = 0f;
        bool visualFlip = false;

        // ---- determine corner + rotation ----
        if (dx > 0 && dy > 0) // 4
        {
            if (dx_fc == 0)
            {
                cx = half; cy = -half;
                startAngle = 180f;
                delta90 = -90f;
                visualFlip = true;
            }
            else
            {
                cx = -half; cy = half;
                startAngle = 270f;
                delta90 = 90f;
            }
        }
        else if (dx < 0 && dy > 0) // 1
        {
            if (dx_fc == 0)
            {
                cx = -half; cy = -half;
                startAngle = 0f;
                delta90 = 90f;
            }
            else
            {
                cx = half; cy = half;
                startAngle = 270f;
                delta90 = -90f;
                visualFlip = true;
            }
        }
        else if (dx > 0 && dy < 0) // 3
        {
            if (dx_fc == 0)
            {
                cx = half; cy = half;
                startAngle = 180f;
                delta90 = 90f;
            }
            else
            {
                cx = -half; cy = -half;
                startAngle = 90f;
                delta90 = -90f;
                visualFlip = true;
            }
        }
        else if (dx < 0 && dy < 0) // 2
        {
            if (dx_fc == 0)
            {
                cx = -half; cy = half;
                startAngle = 0f;
                delta90 = -90f;
                visualFlip = true;
            }
            else
            {
                cx = half; cy = -half;
                startAngle = 90f;
                delta90 = 90f;
            }
        }
        else
        {
            return targets;
        }

        startAngle = Utils.NormalizeAngle(startAngle);

        for (int i = 0; i < N; i++)
        {
            float t = (N == 1) ? 0f : (float)i / (N - 1);

            float angleDeg = Utils.NormalizeAngle(
                startAngle + delta90 * t
            );

            float rad = angleDeg * Mathf.Deg2Rad;

            float x = cx + Mathf.Cos(rad) * half;
            float y = cy + Mathf.Sin(rad) * half;

            float visAngle = angleDeg;

            if (visualFlip)
                visAngle = Utils.NormalizeAngle(visAngle + 180f);

            Vector3 worldPos = parent.TransformPoint(new Vector3(x, y, 0f));

            targets.Add(new SegmentTarget
            {
                pos = worldPos,
                angle = visAngle
            });
        }

        return targets;
    }

    #endregion

    #region Trail

    public void UpdateTrail(Vector2Int targetPoint)
    {
        if (targetPoint == movePoint) return;

        Vector2Int freePoint = trail[^1];
        Data.Grid[freePoint.y, freePoint.x] = 0;

        for (int i = trail.Count - 1; i > 0; i--)
        {
            trail[i] = trail[i - 1];

            if (i < trail.Count - 1)
            {
                parts[i].position = new Vector3(
                    trail[i].x * Data.CellSize,
                    trail[i].y * Data.CellSize
                );
            }
            else
            {
                endPoint = trail[i];
            }
        }

        movePoint = targetPoint;
        trail[0] = targetPoint;
        if (isMovingHead)
        {
            headPoint = trail[0];
            tailPoint = trail[^1];
            
        }
        else {
            tailPoint = trail[0];
            headPoint = trail[^1];
        }

        MarkOccupiedOnTrail();
    }

    void MarkOccupiedOnTrail()
    {
        foreach (var p in trail)
            Data.Grid[p.y, p.x] = 1;
    }

    public void SetBackwardsMovement(bool isBackward)
    {
        isBackwards = isBackward;

        int n = trail.Count - 1;
        backwardPoint1 = trail[n];
        backwardPoint2 = trail[n - 1];
    }

    #endregion

    #region Segment Motion

    void InitSegmentTargets()
    {
        foreach (var seg in segments)
        {
            segmentTargets.Add(new SegmentTarget(
                seg.position,
                seg.eulerAngles.z
            ));
        }
    }

    bool MoveSegmentsToTargets(float speed, float dt)
    {
        bool allReached = true;

        for (int i = 0; i < segments.Count; i++)
        {
            var seg = segments[i];
            var tgt = segmentTargets[i];

            Vector3 curPos = seg.position;
            float dist = Vector3.Distance(curPos, tgt.pos);

            if (dist > 0.001f)
            {
                float t = Mathf.Min(1, speed * dt / dist);

                seg.position = Vector3.Lerp(curPos, tgt.pos, t);

                float nextAngle = Mathf.LerpAngle(
                    seg.eulerAngles.z,
                    tgt.angle,
                    t
                );

                seg.rotation = Quaternion.Euler(0, 0, nextAngle);

                allReached = false;
            }
            else
            {
                seg.position = tgt.pos;
                seg.rotation = Quaternion.Euler(0, 0, tgt.angle);
            }
        }

        return allReached;
    }

    public void LookAt2D(Vector3 target)
    {
        Vector3 from = moveNode.position;
        Vector3 to = target;

        float dx = to.x - from.x;
        float dy = to.y - from.y;

        float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg + 90f;

        float delta = (endNode == tailNode) ? 0f : 180f;

        moveNode.rotation = Quaternion.Euler(0f, 0f, angle + delta);
    }

    void ConsumeTailTargets(int count)
    {
        segmentTargets.RemoveRange(
            segmentTargets.Count - count,
            count
        );
    }

    #endregion

    #region Direction

    public void DetermineMovementDirection(Vector2Int targetPoint)
    {
        if (segmentTargets.Count > segments.Count) return;

        int distFromHead = Utils.Manhattan(targetPoint, headPoint);
        int distFromTail = Utils.Manhattan(targetPoint, tailPoint);

        if (distFromHead <= distFromTail)
        {
            movePoint = headPoint;
            endPoint = tailPoint;
            moveNode = headNode;
            endNode = tailNode;

            if (!isMovingHead) ReverseDirection();
            isMovingHead = true;
        }
        else
        {
            movePoint = tailPoint;
            endPoint = headPoint;
            moveNode = tailNode;
            endNode = headNode;

            if (isMovingHead) ReverseDirection();
            isMovingHead = false;
        }
    }

    void ReverseDirection()
    {
        segmentTargets.Reverse();
        trail.Reverse();
        segments.Reverse();
        parts.Reverse();

        foreach (var segment in segments)
        {
            float angle = segment.eulerAngles.z + 180f;
            segment.rotation = Quaternion.Euler(0, 0, angle);

            // Vector3 scale = segment.localScale;
            // segment.localScale = new Vector3(
            //     -scale.x,
            //     -scale.y,
            //     scale.z
            // );
        }
    }

    #endregion

    #region Helpers

    void AnimateLegs()
    {
        // foreach (var s in legSkeletons)
        //     s.timeScale = isMoving ? 1 : 0;
    }

    public void OnEnterExit()
    {
        isEnterExit = true;
    }

    #endregion
}