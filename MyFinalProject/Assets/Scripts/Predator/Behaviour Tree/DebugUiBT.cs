using UnityEngine;

public class DebugUiBT : MonoBehaviour
{
    public PredatorBT agent;
    public Vector2 windowPos = new Vector2(320, 10);
    public Vector2 windowSize = new Vector2(300, 220);

    private void Reset()
    {
        agent = GetComponent<PredatorBT>();
    }

    private void OnGUI()
    {
        if (agent == null) return;
        GUI.Box(new Rect(windowPos.x, windowPos.y, windowSize.x, windowSize.y), "BT Predator Debug");

        float x = windowPos.x + 8f;
        float y = windowPos.y + 22f;
        float h = 18f;

        GUI.Label(new Rect(x, y, 260, h), $"Time Since Seen: {agent.TimeSinceSeen:F2}s");
        y += h;
        GUI.Label(new Rect(x, y, 260, h), $"Prey Recognised: {agent.IsRecognised}");
        y += h;
        GUI.Label(new Rect(x, y, 260, h), $"Active Familiars: {agent.ActiveFamiliarCount}");
        y += h;
        GUI.Label(new Rect(x, y, 260, h), $"Familiar Cooldown: {agent.FamiliarCooldDownRemaining:F1}s");
        y += h;
        GUI.Label(new Rect(x, y, 260, h), $"Current Behaviour: {agent.CurrentBehaviour}");
        y += h;

    }
}
