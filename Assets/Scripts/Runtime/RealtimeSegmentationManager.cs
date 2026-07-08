using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Provides a real-time segmentation baseline.
/// The first implementation renders the current scene using segmentation materials.
/// Later, an ONNX/Sentis/Python provider can replace this output while keeping the UI contract.
/// </summary>
public class RealtimeSegmentationManager : MonoBehaviour
{
    public enum ProviderMode
    {
        GroundTruthMask,
        ExternalHttpModel
    }

    [Header("Realtime Segmentation")]
    public int outputWidth = 640;
    public int outputHeight = 360;
    public bool startEnabled;
    public ProviderMode providerMode = ProviderMode.GroundTruthMask;

    [Header("External Model")]
    public string externalEndpoint = "http://127.0.0.1:5000/segment";
    public float externalInferenceFps = 5f;
    public int externalRequestTimeoutSeconds = 5;

    public RenderTexture OutputTexture { get; private set; }
    public bool IsEnabled { get; private set; }
    public string ProviderName { get; private set; } = "GroundTruthMask";
    public string StatusText { get; private set; } = "Idle";
    public float LastInferenceMilliseconds { get; private set; }

    private CameraManager cameraManager;
    private SegmentationMaterialManager segmentationManager;
    private Camera segmentationCamera;
    private float nextExternalInferenceTime;
    private bool isExternalRequestRunning;
    private Texture2D externalMaskTexture;

    public void Initialize(CameraManager cameras, SegmentationMaterialManager segmentation)
    {
        cameraManager = cameras;
        segmentationManager = segmentation;

        OutputTexture = new RenderTexture(outputWidth, outputHeight, 24, RenderTextureFormat.ARGB32)
        {
            name = "RealtimeSegmentation_Output"
        };

        CreateSegmentationCamera();
        UpdateProviderName();
        SetEnabled(startEnabled);
    }

    private void LateUpdate()
    {
        if (!IsEnabled || cameraManager == null || cameraManager.VehicleCamera == null || segmentationCamera == null)
        {
            return;
        }

        if (providerMode == ProviderMode.GroundTruthMask)
        {
            RenderGroundTruthMask();
            StatusText = "Ground truth";
            return;
        }

        if (providerMode == ProviderMode.ExternalHttpModel && Time.time >= nextExternalInferenceTime && !isExternalRequestRunning)
        {
            nextExternalInferenceTime = Time.time + 1f / Mathf.Max(0.1f, externalInferenceFps);
            StartCoroutine(RequestExternalInference());
        }
    }

    public void Toggle()
    {
        SetEnabled(!IsEnabled);
    }

    public void SetEnabled(bool enabled)
    {
        IsEnabled = enabled;
        if (segmentationCamera != null)
        {
            segmentationCamera.enabled = false;
        }
    }

    public void SwitchProvider()
    {
        if (providerMode == ProviderMode.GroundTruthMask)
        {
            providerMode = ProviderMode.ExternalHttpModel;
        }
        else
        {
            providerMode = ProviderMode.GroundTruthMask;
        }

        UpdateProviderName();
        StatusText = "Provider: " + ProviderName;
    }

    private void CreateSegmentationCamera()
    {
        if (cameraManager == null || cameraManager.VehicleCamera == null)
        {
            return;
        }

        GameObject cameraObject = new GameObject("RealtimeSegmentationCamera");
        cameraObject.transform.SetParent(cameraManager.VehicleCamera.transform, false);
        cameraObject.transform.localPosition = Vector3.zero;
        cameraObject.transform.localRotation = Quaternion.identity;

        segmentationCamera = cameraObject.AddComponent<Camera>();
        SyncCameraSettings();
        segmentationCamera.targetTexture = OutputTexture;
        segmentationCamera.enabled = false;
    }

    private void SyncCameraSettings()
    {
        Camera source = cameraManager.VehicleCamera;
        segmentationCamera.CopyFrom(source);
        segmentationCamera.targetTexture = OutputTexture;
        segmentationCamera.enabled = false;

        AudioListener listener = segmentationCamera.GetComponent<AudioListener>();
        if (listener != null)
        {
            Destroy(listener);
        }
    }

    private void RenderGroundTruthMask()
    {
        bool previousSegmentationMode = segmentationManager != null && segmentationManager.IsSegmentationMode;

        SyncCameraSettings();

        if (segmentationManager != null)
        {
            segmentationManager.SetSegmentationMode(true);
        }

        segmentationCamera.Render();

        if (segmentationManager != null)
        {
            segmentationManager.SetSegmentationMode(previousSegmentationMode);
        }
    }

    private IEnumerator RequestExternalInference()
    {
        isExternalRequestRunning = true;
        float startTime = Time.realtimeSinceStartup;

        byte[] pngBytes = CaptureVehicleCameraPng();
        if (pngBytes == null || pngBytes.Length == 0)
        {
            StatusText = "Capture failed";
            isExternalRequestRunning = false;
            yield break;
        }

        UnityWebRequest request = new UnityWebRequest(externalEndpoint, UnityWebRequest.kHttpVerbPOST);
        request.uploadHandler = new UploadHandlerRaw(pngBytes);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.timeout = Mathf.Max(1, externalRequestTimeoutSeconds);
        request.SetRequestHeader("Content-Type", "image/png");
        request.SetRequestHeader("Accept", "image/png");

        yield return request.SendWebRequest();

        LastInferenceMilliseconds = (Time.realtimeSinceStartup - startTime) * 1000f;

        if (request.result != UnityWebRequest.Result.Success)
        {
            StatusText = "HTTP error: " + request.error;
            isExternalRequestRunning = false;
            request.Dispose();
            yield break;
        }

        if (externalMaskTexture == null)
        {
            externalMaskTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        }

        bool loaded = externalMaskTexture.LoadImage(request.downloadHandler.data);
        if (!loaded)
        {
            StatusText = "Invalid mask PNG";
            isExternalRequestRunning = false;
            request.Dispose();
            yield break;
        }

        Graphics.Blit(externalMaskTexture, OutputTexture);
        StatusText = "HTTP " + LastInferenceMilliseconds.ToString("0") + " ms";

        isExternalRequestRunning = false;
        request.Dispose();
    }

    private byte[] CaptureVehicleCameraPng()
    {
        if (cameraManager == null || cameraManager.VehicleCamera == null)
        {
            return null;
        }

        Camera sourceCamera = cameraManager.VehicleCamera;
        RenderTexture previousTarget = sourceCamera.targetTexture;
        RenderTexture previousActive = RenderTexture.active;

        RenderTexture renderTexture = RenderTexture.GetTemporary(outputWidth, outputHeight, 24, RenderTextureFormat.ARGB32);
        Texture2D texture = new Texture2D(outputWidth, outputHeight, TextureFormat.RGB24, false);

        sourceCamera.targetTexture = renderTexture;
        sourceCamera.Render();
        RenderTexture.active = renderTexture;
        texture.ReadPixels(new Rect(0f, 0f, outputWidth, outputHeight), 0, 0);
        texture.Apply();

        byte[] pngBytes = texture.EncodeToPNG();

        sourceCamera.targetTexture = previousTarget;
        RenderTexture.active = previousActive;
        RenderTexture.ReleaseTemporary(renderTexture);
        Destroy(texture);

        return pngBytes;
    }

    private void UpdateProviderName()
    {
        ProviderName = providerMode == ProviderMode.GroundTruthMask ? "GroundTruthMask" : "ExternalHttpModel";
    }
}
