using UnityEngine;

public class DebugUI : MonoBehaviour
{
    public PredatorAgent agent;
    public Vector2 windowPos = new Vector2(10, 10);
    public Vector2 windowSize = new Vector2(280, 150);

    private void Reset()
    {
        agent = GetComponent<PredatorAgent>();
    }

    private void OnGUI()
    {
        if (agent == null) return;
        GUI.Box(new Rect(windowPos.x, windowPos.y, windowSize.x, windowSize.y), "Predator Debug");

        float x = windowPos.x + 8f;
        float y = windowPos.y + 22f;
        float h = 18f;

        GUI.Label(new Rect(x, y, 260, h), $"Cumlative Reward: {agent.GetCumulativeReward():F3}");
        y += h;
        GUI.Label(new Rect(x, y, 260, h), $"LastDistanceToPlayer: {agent.LastDistanceToPlayer:F2}");
        y += h;
        GUI.Label(new Rect(x, y, 260, h), $"LastDistanceToScent: {agent.LastDistanceToScent:F2}");
        y += h;
        GUI.Label(new Rect(x, y, 260, h), $"TimeSinceSeen: {agent.TimeSinceSeen:F2}");
        y += h;
        GUI.Label(new Rect(x, y, 260, h), $"Has LOS: {agent.HasLineOfSight()}");
        y += h;

        // agent state Heuristic
        string state = "UNKOWN";
        if (agent.HasLineOfSight()) state = "CHASING";
        else if (agent.LastDistanceToScent < agent.LastDistanceToPlayer) state = "TRACKING_SCENT";
        else state = "SEARCHING";

        GUI.Label(new Rect(x, y, 260, h), $"Agent State: {state}");
    }
}
