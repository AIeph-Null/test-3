using UnityEngine;

public class FishSwimInBoundsAndAvoidTerrain : MonoBehaviour
{
    public float speed = 2f;
    public float turnSpeed = 2f;
    public float changeTargetInterval = 2f;

    public BoxCollider bounds; // drag your underwater box collider here
    public LayerMask obstacleMask; // set this to Terrain or Default

    public float avoidDistance = 3f;  // how far fish can "see" obstacles
    public float avoidTurnStrength = 3f; // how strongly fish turn away

    private Vector3 target;
    private float timer;

    void Start()
    {
        PickNewTarget();
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer > changeTargetInterval)
        {
            PickNewTarget();
            timer = 0f;
        }

        // Move forward
        transform.position += transform.forward * speed * Time.deltaTime;

        // Base direction toward target
        Vector3 dir = target - transform.position;

        // Terrain avoidance
        dir += AvoidTerrainDirection();

        // If dir is zero, skip
        if (dir.sqrMagnitude > 0.1f)
        {
            Quaternion rot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, rot, turnSpeed * Time.deltaTime);
        }

        // Stay inside box
        if (!IsInsideBounds(transform.position))
        {
            target = GetRandomPointInsideBounds();
        }
    }

    // ---------------------------
    // Terrain avoidance behavior
    // ---------------------------
    Vector3 AvoidTerrainDirection()
    {
        Vector3 avoid = Vector3.zero;

        // Forward ray
        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hitF, avoidDistance, obstacleMask))
        {
            avoid -= transform.forward * (avoidTurnStrength / Mathf.Max(0.1f, hitF.distance));
        }

        // Left ray
        if (Physics.Raycast(transform.position, -transform.right, out RaycastHit hitL, avoidDistance, obstacleMask))
        {
            avoid += transform.right * (avoidTurnStrength / Mathf.Max(0.1f, hitL.distance));
        }

        // Right ray
        if (Physics.Raycast(transform.position, transform.right, out RaycastHit hitR, avoidDistance, obstacleMask))
        {
            avoid -= transform.right * (avoidTurnStrength / Mathf.Max(0.1f, hitR.distance));
        }

        // Down ray (avoid seabed/terrain)
        if (Physics.Raycast(transform.position, -transform.up, out RaycastHit hitD, avoidDistance, obstacleMask))
        {
            avoid += transform.up * (avoidTurnStrength / Mathf.Max(0.1f, hitD.distance));
        }

        return avoid;
    }

    // ---------------------------
    // Bounds checking
    // ---------------------------
    bool IsInsideBounds(Vector3 pos)
    {
        Vector3 local = bounds.transform.InverseTransformPoint(pos);
        Vector3 extent = bounds.size * 0.5f;
        return Mathf.Abs(local.x) <= extent.x &&
               Mathf.Abs(local.y) <= extent.y &&
               Mathf.Abs(local.z) <= extent.z;
    }

    void PickNewTarget()
    {
        target = GetRandomPointInsideBounds();
    }

    Vector3 GetRandomPointInsideBounds()
    {
        Vector3 ext = bounds.size * 0.5f;
        Vector3 randomLocal = new Vector3(
            Random.Range(-ext.x, ext.x),
            Random.Range(-ext.y, ext.y),
            Random.Range(-ext.z, ext.z)
        );
        return bounds.transform.TransformPoint(randomLocal);
    }
}
