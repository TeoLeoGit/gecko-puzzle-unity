using UnityEngine;
using System;

public class BaseExit : MonoBehaviour
{
    [SerializeField]
    private Vector2Int point = new Vector2Int(4, 0);

    public Vector2Int Point => point;

    void Awake()
    {
        // Notify Game that this exit exists
        GameEvents.RaiseExitCreated(this);
    }

    public void OnGeckoEnter()
    {
        Debug.Log("I have gecko");
    }

    public void ConsumeGecko(GeckoController gecko)
    {
        // Future logic:
        // - Destroy gecko
        // - Play animation
        // - Increase score
    }
}