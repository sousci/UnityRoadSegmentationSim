using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Drives the ego vehicle along fixed waypoints and captures varied training data.
/// This is intentionally simple: it is a deterministic route with small random
/// steering, speed, and camera jitter so the dataset does not contain identical views.
/// </summary>
public class AutoDriveCaptureController : MonoBehaviour
{
    [Header("Driving")]
    public float targetSpeedKmh = 24f;
    public float waypointReachDistance = 4f;
    public float steeringGain = 1.35f;
    public float slowDownAngleDegrees = 55f;

    [Header("Randomization")]
    public float steeringNoiseAmount = 0.08f;
    public float speedNoiseKmh = 5f;
    public float cameraYawJitterDegrees = 2.5f;
    public float cameraPitchJitterDegrees = 1.5f;
    public float noiseUpdateIntervalSeconds = 1.4f;

    [Header("Capture")]
    public bool captureWhileAutoDriving = true;
    public float captureIntervalSeconds = 0.8f;

    private readonly List<Vector3> waypoints = new List<Vector3>();
    private VehicleController vehicle;
    private DatasetCaptureManager captureManager;
    private Camera vehicleCamera;
    private Quaternion vehicleCameraBaseLocalRotation;
    private int waypointIndex;
    private float nextCaptureTime;
    private float nextNoiseUpdateTime;
    private float steeringNoise;
    private float speedNoise;
    private float cameraYawNoise;
    private float cameraPitchNoise;

    public bool IsEnabled { get; private set; }

    public string StatusText
    {
        get
        {
            if (!IsEnabled)
            {
                return "OFF";
            }

            return "ON WP " + (waypointIndex + 1).ToString() + "/" + waypoints.Count.ToString();
        }
    }

    public void Initialize(VehicleController vehicleController, DatasetCaptureManager datasetCaptureManager, Camera camera)
    {
        vehicle = vehicleController;
        captureManager = datasetCaptureManager;
        vehicleCamera = camera;
        if (vehicleCamera != null)
        {
            vehicleCameraBaseLocalRotation = vehicleCamera.transform.localRotation;
        }

        BuildDefaultRoute();
    }

    private void Update()
    {
        if (!IsEnabled || vehicle == null || waypoints.Count == 0)
        {
            return;
        }

        UpdateNoise();
        DriveTowardWaypoint();
        CaptureIfNeeded();
    }

    public void Toggle()
    {
        SetEnabled(!IsEnabled);
    }

    public void SetEnabled(bool enabled)
    {
        IsEnabled = enabled;

        if (vehicle == null)
        {
            return;
        }

        if (IsEnabled)
        {
            waypointIndex = FindNearestForwardWaypoint();
            nextCaptureTime = Time.time + 0.2f;
            nextNoiseUpdateTime = 0f;
            SnapVehicleToRouteStartIfFarAway();
            vehicle.SetExternalInput(0f, 0f, true);
        }
        else
        {
            vehicle.ClearExternalInput();
            ResetCameraJitter();
        }

        Debug.Log("Auto drive capture: " + (IsEnabled ? "ON" : "OFF"));
    }

    private void BuildDefaultRoute()
    {
        waypoints.Clear();

        float left = -74f;
        float leftVerticalRoad = -55f;
        float rightVerticalRoad = 55f;
        float north = 36.2f;
        float south = -36.2f;
        float eastLane = 1.8f;
        float westLane = -1.8f;
        float northLane = 36.2f;
        float southLane = -36.2f;

        waypoints.Add(new Vector3(left, 0.75f, westLane));
        waypoints.Add(new Vector3(0f, 0.75f, westLane));
        waypoints.Add(new Vector3(rightVerticalRoad, 0.75f, westLane));
        waypoints.Add(new Vector3(rightVerticalRoad, 0.75f, northLane));
        waypoints.Add(new Vector3(0f, 0.75f, north));
        waypoints.Add(new Vector3(leftVerticalRoad, 0.75f, north));
        waypoints.Add(new Vector3(leftVerticalRoad, 0.75f, south));
        waypoints.Add(new Vector3(0f, 0.75f, south));
        waypoints.Add(new Vector3(rightVerticalRoad, 0.75f, south));
        waypoints.Add(new Vector3(rightVerticalRoad, 0.75f, eastLane));
        waypoints.Add(new Vector3(0f, 0.75f, eastLane));
        waypoints.Add(new Vector3(left, 0.75f, eastLane));
    }

    private void SnapVehicleToRouteStartIfFarAway()
    {
        Vector3 first = waypoints[0];
        if (Vector3.Distance(vehicle.transform.position, first) < 20f)
        {
            return;
        }

        Vector3 second = waypoints[Mathf.Min(1, waypoints.Count - 1)];
        Quaternion rotation = Quaternion.LookRotation((second - first).normalized, Vector3.up);
        vehicle.TeleportTo(first, rotation);
        waypointIndex = 1;
    }

    private int FindNearestForwardWaypoint()
    {
        int nearestIndex = 0;
        float nearestDistance = float.MaxValue;
        Vector3 position = vehicle.transform.position;

        for (int i = 0; i < waypoints.Count; i++)
        {
            float distance = Vector3.Distance(position, waypoints[i]);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestIndex = i;
            }
        }

        return (nearestIndex + 1) % waypoints.Count;
    }

    private void UpdateNoise()
    {
        if (Time.time < nextNoiseUpdateTime)
        {
            return;
        }

        steeringNoise = Random.Range(-steeringNoiseAmount, steeringNoiseAmount);
        speedNoise = Random.Range(-speedNoiseKmh, speedNoiseKmh);
        cameraYawNoise = Random.Range(-cameraYawJitterDegrees, cameraYawJitterDegrees);
        cameraPitchNoise = Random.Range(-cameraPitchJitterDegrees, cameraPitchJitterDegrees);
        nextNoiseUpdateTime = Time.time + noiseUpdateIntervalSeconds;

        if (vehicleCamera != null)
        {
            vehicleCamera.transform.localRotation = vehicleCameraBaseLocalRotation * Quaternion.Euler(cameraPitchNoise, cameraYawNoise, 0f);
        }
    }

    private void DriveTowardWaypoint()
    {
        Vector3 target = waypoints[waypointIndex];
        Vector3 toTarget = target - vehicle.transform.position;
        toTarget.y = 0f;

        if (toTarget.magnitude < waypointReachDistance)
        {
            waypointIndex = (waypointIndex + 1) % waypoints.Count;
            return;
        }

        Vector3 forward = vehicle.transform.forward;
        forward.y = 0f;
        float angle = Vector3.SignedAngle(forward.normalized, toTarget.normalized, Vector3.up);
        float steering = Mathf.Clamp(angle / 35f * steeringGain + steeringNoise, -1f, 1f);
        float targetSpeed = Mathf.Max(8f, targetSpeedKmh + speedNoise);
        float angleSlowdown = Mathf.InverseLerp(slowDownAngleDegrees, 5f, Mathf.Abs(angle));
        float throttle = vehicle.CurrentSpeedKmh < targetSpeed * Mathf.Clamp(angleSlowdown, 0.35f, 1f) ? 1f : 0.15f;
        bool brake = Mathf.Abs(angle) > 85f && vehicle.CurrentSpeedKmh > 10f;

        vehicle.SetExternalInput(throttle, steering, brake);
    }

    private void CaptureIfNeeded()
    {
        if (!captureWhileAutoDriving || captureManager == null || Time.time < nextCaptureTime)
        {
            return;
        }

        captureManager.CaptureSingleFrame();
        nextCaptureTime = Time.time + Mathf.Max(0.2f, captureIntervalSeconds);
    }

    private void ResetCameraJitter()
    {
        if (vehicleCamera != null)
        {
            vehicleCamera.transform.localRotation = vehicleCameraBaseLocalRotation;
        }
    }
}
