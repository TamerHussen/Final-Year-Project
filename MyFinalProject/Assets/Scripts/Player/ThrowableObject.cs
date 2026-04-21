using UnityEngine;

public class ThrowableObject : MonoBehaviour
{
    public float stunDuration = 3f;
    public GameObject hitEffect;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Destroy(gameObject, 5f);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Predator"))
        {
            PredatorAgent mlAgent = collision.gameObject.GetComponent<PredatorAgent>();
            if (mlAgent != null) mlAgent.ApplyStun(stunDuration);

            PredatorBT btAgent = collision.gameObject.GetComponent<PredatorBT>();
            if (btAgent != null) btAgent.ApplyStun(stunDuration);

            if (hitEffect != null) Instantiate(hitEffect, transform.position, Quaternion.identity);

            Destroy(gameObject);
        }
    }
}
