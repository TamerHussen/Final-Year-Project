using UnityEngine;

public class DebugUI : MonoBehaviour
{
    public PredatorAgent agent;
    public Vector2 windowPos = new Vector2(10, 10);
    public Vector2 windowSize = new Vector2(300, 220);

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
        GUI.Label(new Rect(x, y, 260, h), $"Dist to Player: {agent.LastDistanceToPlayer:F2}");
        y += h;
        GUI.Label(new Rect(x, y, 260, h), $"Dist To Scent: {agent.LastDistanceToScent:F2}");
        y += h;
        GUI.Label(new Rect(x, y, 260, h), $"Time Since Seen: {agent.TimeSinceSeen:F2}s");
        y += h;
        GUI.Label(new Rect(x, y, 260, h), $"Prey Recognised: {agent.IsRecognised}");
        y += h;
        GUI.Label(new Rect(x, y, 260, h), $"Has LOS: {agent.HasLineOfSight()}");
        y += h;
        GUI.Label(new Rect(x, y, 260, h), $"Active Familiars: {agent.ActiveFamiliarCount}");
        y += h;
        GUI.Label(new Rect(x, y, 260, h), $"Familiar Cooldown: {agent.FamiliarCooldDownRemaining:F1}s");
        y += h;

        // agent state Heuristic
        string state = "SEARCHING";
        if (agent.HasLineOfSight())
            state = agent.IsRecognised ? "STRIKING" : "STALKING";
        else if (agent.LastDistanceToScent < agent.LastDistanceToPlayer)
            state = "TRACKING_SCENT";
        else if (agent.TimeSinceSeen > 5f)
            state = "LOST_TARGET";

        GUI.Label(new Rect(x, y, 280, h), $"Agent State: {state}");
        y += h;
    }
}
