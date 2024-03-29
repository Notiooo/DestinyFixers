using UnityEngine;

public class ToolInteraction : MonoBehaviour
{
    public PlayerController playerController;
    public float speedDebuffWithTool;
    public Transform toolHolder;
    public GameObject toolsParent;
    public float pickupRadius = 1f;

    private GameObject currentTool = null;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E)) // Press E to pick up or drop
        {
            if (currentTool == null)
            {
                TryPickupTool();
            }
            else
            {
                DropTool();
            }
        }
    }

    void TryPickupTool()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, pickupRadius);
        float minDistance = float.MaxValue;
        GameObject closestTool = null;

        foreach (var collider in colliders)
        {
            if (collider.CompareTag("Tool"))
            {
                float distance = Vector3.Distance(transform.position, collider.transform.position);
                if (distance < minDistance)
                {
                    closestTool = collider.gameObject;
                    minDistance = distance;
                }
            }
        }

        if (closestTool != null)
        {
            currentTool = closestTool.gameObject;
            AttachTool(currentTool);
            playerController.movementSpeedGround -= speedDebuffWithTool;
        }
    }

    void AttachTool(GameObject tool)
    {
        tool.GetComponent<Collider>().enabled = false;
        tool.transform.SetParent(toolHolder);
        tool.transform.localPosition = Vector3.zero;
    }

    void DropTool()
    {
        if (currentTool != null)
        {
            currentTool.transform.SetParent(toolsParent.transform);
            currentTool.GetComponent<Collider>().enabled = true;
            currentTool.transform.position = transform.position;
            currentTool = null;
            playerController.movementSpeedGround += speedDebuffWithTool;
        }
    }
}
