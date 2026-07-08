using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Owns camera activation and the VehicleCamera render texture used by future AI input capture.
/// </summary>
public class CameraManager : MonoBehaviour
{
    public Camera MainCamera { get; private set; }
    public Camera VehicleCamera { get; private set; }
    public Camera TopViewCamera { get; private set; }
    public RenderTexture VehicleRenderTexture { get; private set; }
    public string CurrentCameraName { get; private set; } = "Main Camera";

    private readonly List<Camera> cameras = new List<Camera>();
    private Camera vehicleCaptureCamera;
    private int activeCameraIndex;

    public void Configure(Camera mainCamera, Camera vehicleCamera, Camera topViewCamera)
    {
        MainCamera = mainCamera;
        VehicleCamera = vehicleCamera;
        TopViewCamera = topViewCamera;

        cameras.Clear();
        cameras.Add(MainCamera);
        cameras.Add(VehicleCamera);
        cameras.Add(TopViewCamera);

        VehicleRenderTexture = new RenderTexture(1280, 720, 24)
        {
            name = "VehicleCamera_RenderTexture"
        };

        GameObject captureObject = new GameObject("VehicleCamera_RenderTextureCapture");
        captureObject.transform.SetParent(VehicleCamera.transform, false);
        vehicleCaptureCamera = captureObject.AddComponent<Camera>();
        vehicleCaptureCamera.CopyFrom(VehicleCamera);
        vehicleCaptureCamera.targetTexture = VehicleRenderTexture;
        vehicleCaptureCamera.enabled = true;

        SetActiveCamera(0);
    }

    public void SwitchCamera()
    {
        if (cameras.Count == 0)
        {
            return;
        }

        SetActiveCamera((activeCameraIndex + 1) % cameras.Count);
    }

    private void SetActiveCamera(int index)
    {
        activeCameraIndex = Mathf.Clamp(index, 0, cameras.Count - 1);

        for (int i = 0; i < cameras.Count; i++)
        {
            if (cameras[i] == null)
            {
                continue;
            }

            cameras[i].enabled = i == activeCameraIndex;
            AudioListener listener = cameras[i].GetComponent<AudioListener>();
            if (listener != null)
            {
                listener.enabled = i == activeCameraIndex;
            }
        }

        CurrentCameraName = cameras[activeCameraIndex] != null ? cameras[activeCameraIndex].name : "Unknown";
    }
}
