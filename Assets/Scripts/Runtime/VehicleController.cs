using UnityEngine;

/// <summary>
/// Simple deterministic vehicle controller for Phase 1.
/// W/S move, A/D steer, Space brakes.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class VehicleController : MonoBehaviour
{
    [Header("Movement")]
    public float acceleration = 8f;
    public float reverseAcceleration = 6f;
    public float coastDeceleration = 5f;
    public float steeringDegreesPerSecond = 100f;
    public float brakeStrength = 18f;
    public float maxSpeed = 12f;
    public float maxReverseSpeed = 6f;

    public bool UseExternalInput { get; private set; }

    public float CurrentSpeedKmh
    {
        get
        {
            return Mathf.Abs(currentSpeed) * 3.6f;
        }
    }

    private Rigidbody cachedRigidbody;
    private Vector3 startPosition;
    private Quaternion startRotation;
    private float throttleInput;
    private float steeringInput;
    private bool brakeInput;
    private float currentSpeed;
    private float fixedYPosition;

    private void Awake()
    {
        cachedRigidbody = GetComponent<Rigidbody>();
        cachedRigidbody.isKinematic = true;
        cachedRigidbody.useGravity = false;
        cachedRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        cachedRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        startPosition = transform.position;
        startRotation = transform.rotation;
        fixedYPosition = startPosition.y;
    }

    private void FixedUpdate()
    {
        ReadInput();
        UpdateSpeed();
        MoveVehicle();
    }

    private void ReadInput()
    {
        if (UseExternalInput)
        {
            return;
        }

        throttleInput = 0f;
        steeringInput = 0f;
        brakeInput = false;

        if (Input.GetKey(KeyCode.W))
        {
            throttleInput = 1f;
        }
        else if (Input.GetKey(KeyCode.S))
        {
            throttleInput = -1f;
        }

        if (Input.GetKey(KeyCode.A))
        {
            steeringInput = -1f;
        }
        else if (Input.GetKey(KeyCode.D))
        {
            steeringInput = 1f;
        }

        brakeInput = Input.GetKey(KeyCode.Space);
    }

    private void UpdateSpeed()
    {
        if (brakeInput)
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, brakeStrength * Time.fixedDeltaTime);
            return;
        }

        if (throttleInput > 0f)
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, maxSpeed, acceleration * Time.fixedDeltaTime);
        }
        else if (throttleInput < 0f)
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, -maxReverseSpeed, reverseAcceleration * Time.fixedDeltaTime);
        }
        else
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, coastDeceleration * Time.fixedDeltaTime);
        }
    }

    private void MoveVehicle()
    {
        float speedFactor = Mathf.Clamp01(Mathf.Abs(currentSpeed) / 1.5f);
        float reverseFactor = currentSpeed < -0.05f ? -1f : 1f;
        float yawDelta = steeringInput * reverseFactor * steeringDegreesPerSecond * speedFactor * Time.fixedDeltaTime;

        Quaternion nextRotation = cachedRigidbody.rotation * Quaternion.Euler(0f, yawDelta, 0f);
        Vector3 nextPosition = cachedRigidbody.position + (nextRotation * Vector3.forward) * currentSpeed * Time.fixedDeltaTime;
        nextPosition.y = fixedYPosition;

        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            Debug.DrawLine(cachedRigidbody.position, nextPosition, Color.cyan, 0.05f);
        }

        cachedRigidbody.MoveRotation(nextRotation);
        cachedRigidbody.MovePosition(nextPosition);
    }

    public void ResetVehicle()
    {
        transform.SetPositionAndRotation(startPosition, startRotation);
        cachedRigidbody.position = startPosition;
        cachedRigidbody.rotation = startRotation;
        currentSpeed = 0f;
    }

    public void SetExternalInput(float throttle, float steering, bool brake)
    {
        UseExternalInput = true;
        throttleInput = Mathf.Clamp(throttle, -1f, 1f);
        steeringInput = Mathf.Clamp(steering, -1f, 1f);
        brakeInput = brake;
    }

    public void ClearExternalInput()
    {
        UseExternalInput = false;
        throttleInput = 0f;
        steeringInput = 0f;
        brakeInput = false;
    }

    public void TeleportTo(Vector3 position, Quaternion rotation)
    {
        transform.SetPositionAndRotation(position, rotation);
        cachedRigidbody.position = position;
        cachedRigidbody.rotation = rotation;
        currentSpeed = 0f;
        fixedYPosition = position.y;
    }
}
