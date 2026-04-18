using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class EnvironmentVariety
{
    [Header("Identity")]
    public string varietyName = "Dense Fog";

    [Header("Fog")]
    public bool enableFog = true;
    public Color fogColor = new Color(0.4f, 0.4f, 0.4f);
    public float fogDensity = 0.04f;

    [Header("Lighting")]
    [Range(0f, 1f)]
    public float darknessLevel = 0f; // 0 light - 10 dark
    public Color ambientColor = new Color(0.2f, 0.2f, 0.25f);

    [Header("Extra Obstacles")]
    public List<GameObject> extraSoftObjGroups;
    public List<GameObject> extraSolidObjGroups;

    [Header("Audio")]
    public AudioClip ambientLoop;
    public float ambienceVolume = 0.3f;

}

public class BiomeRandomiser : MonoBehaviour
{
    [Header("Environement Varieties")]
    public List<EnvironmentVariety> varieties;

    [Header("Ambience Audio")]
    public AudioSource ambienceSource;

    [Header("Directional Light")]
    public Light sunLight;

    private EnvironmentVariety currentVariety;
    public string CurrentVarietyName => currentVariety != null ? currentVariety.varietyName : "None";

    public void RandomiseBiome()
    {
        if (varieties == null || varieties.Count == 0) return;

        foreach (var v in varieties)
        {
            SetGroupActive(v.extraSoftObjGroups, false);
            SetGroupActive(v.extraSolidObjGroups, false);
        }

        currentVariety = varieties[Random.Range(0, varieties.Count)];
        ApplyBiome(currentVariety);
    }

    void ApplyBiome(EnvironmentVariety v)
    {

        // set fog
        RenderSettings.fog = v.enableFog;
        RenderSettings.fogColor = v.fogColor;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogDensity = v.fogDensity;

        // ambient light
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = v.ambientColor;

        // directional light
        if (sunLight != null)
            sunLight.intensity = Mathf.Lerp(2f, 0.05f, v.darknessLevel);

        SetGroupActive(v.extraSoftObjGroups, true);
        SetGroupActive(v.extraSolidObjGroups, true);

        // ambience audio
        if (ambienceSource != null && v.ambientLoop != null)
        {
            ambienceSource.clip = v.ambientLoop;
            ambienceSource.volume = v.ambienceVolume;
            ambienceSource.loop = true;
            ambienceSource.Play();
        }

        Debug.Log($"[Biome switched to : {v.varietyName}");
    }

    void SetGroupActive(List<GameObject> groups, bool active)
    {
        if (groups == null) return;
        foreach (var g in groups)
            if (g != null) g.SetActive(active);
    }
}
