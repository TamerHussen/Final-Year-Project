using UnityEngine;
using UnityEngine.InputSystem;

public class Collectible : MonoBehaviour
{
    [Header("Visuals")]
    public float bobHeight = 0.3f; // makes the item move up and down
    public float bobSpeed = 2f; // how fast it moves up and down
    public float rotateSpeed = 90f; // makes item spin

    [Header("Collection")]
    public float interactRadius = 3f; // how close the player can interact with
    public GameObject collectFX; // paritcles when picked up

    [Header("Prompt")]
    public GameObject promptUI; // shows the interact button

    private Vector3 startPos;
    private Transform playerTransform;
    private bool collected = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        startPos = transform.position;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) playerTransform = playerObj.transform;

        // show prompt when close
        if (promptUI != null) promptUI.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (collected) return;

        // floating item
        float newY = startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(startPos.x, newY, startPos.z);
        transform.Rotate(Vector3.up * rotateSpeed * Time.deltaTime);

        if (playerTransform == null) return;

        float dist = Vector3.Distance(transform.position, playerTransform.position);
        bool playerNear = dist < interactRadius;

        // show prompt when close
        if (promptUI != null) promptUI.SetActive(playerNear);

        // press E to collect when close
        if (playerNear && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            Collect();
        }
    }

    void Collect()
    {
        collected = true;
        if (promptUI != null) promptUI.SetActive(false);

        // spawn pickup effect
        if (collectFX != null) Instantiate(collectFX, transform.position, Quaternion.identity);

        // update GameManager
        GameManager.instance?.OnCollectibleFound();

        // This finds the renderer even if it's on a child object
        GetComponentInChildren<Renderer>().enabled = false;

        // Also disable the collider so you can't interact with it while it's "dying"
        if (GetComponent<Collider>() != null) GetComponent<Collider>().enabled = false;

        Destroy(gameObject, 1.5f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}
