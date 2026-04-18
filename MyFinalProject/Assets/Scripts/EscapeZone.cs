using Unity.VisualScripting;
using UnityEngine;

public class EscapeZone : MonoBehaviour
{
    [Header("Visual")]
    public ParticleSystem escapeFX;
    public Light escapeLight;

    private float pulseTimer = 0f;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }
    private void OnEnable()
    {
        if (escapeFX != null) escapeFX.Play();
    }

    // Update is called once per frame
    void Update()
    {
        pulseTimer += Time.deltaTime;
        float pulse = Mathf.Sin(pulseTimer * 3f);

        // pulse the light for player attention
        if (escapeLight != null)
        {
            escapeLight.intensity = 8.0f + pulse * 2.0f;
            escapeLight.range = 25f;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GameManager.instance?.OnPlayerEscaped();
        }
    }
}
