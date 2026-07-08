using System;
using System.Collections;
using System.IO;
using UnityEngine;

/// <summary>
/// Captures synchronized RGB, segmentation mask, and metadata from VehicleCamera.
/// Phase 2 starts with single-frame capture via the K key.
/// </summary>
public class DatasetCaptureManager : MonoBehaviour
{
    [Header("Output")]
    public string outputDirectoryName = "Captures";
    public int captureWidth = 1280;
    public int captureHeight = 720;
    public float continuousCaptureIntervalSeconds = 1f;

    private CameraManager cameraManager;
    private SegmentationMaterialManager segmentationManager;
    private VehicleController vehicle;
    private int captureIndex;
    private string outputRoot;
    private float nextContinuousCaptureTime;
    private bool isCapturingFrame;

    public bool IsContinuousCaptureEnabled { get; private set; }

    public void Initialize(CameraManager cameras, SegmentationMaterialManager segmentation, VehicleController vehicleController)
    {
        cameraManager = cameras;
        segmentationManager = segmentation;
        vehicle = vehicleController;
        outputRoot = Path.Combine(GetProjectRoot(), outputDirectoryName);
        EnsureOutputDirectories();
        WriteClassDefinitions();
        captureIndex = GetNextCaptureIndex();
    }

    private void Update()
    {
        if (!IsContinuousCaptureEnabled || isCapturingFrame || Time.time < nextContinuousCaptureTime)
        {
            return;
        }

        CaptureSingleFrame();
        nextContinuousCaptureTime = Time.time + Mathf.Max(0.1f, continuousCaptureIntervalSeconds);
    }

    public void ToggleContinuousCapture()
    {
        SetContinuousCaptureEnabled(!IsContinuousCaptureEnabled);
    }

    public void SetContinuousCaptureEnabled(bool enabled)
    {
        IsContinuousCaptureEnabled = enabled;
        nextContinuousCaptureTime = Time.time;
        Debug.Log("Dataset continuous capture: " + (IsContinuousCaptureEnabled ? "ON" : "OFF"));
    }

    public void CaptureSingleFrame()
    {
        if (isCapturingFrame)
        {
            return;
        }

        if (cameraManager == null || cameraManager.VehicleCamera == null)
        {
            Debug.LogWarning("DatasetCaptureManager: VehicleCamera is not configured.");
            return;
        }

        StartCoroutine(CaptureSingleFrameRoutine());
    }

    private IEnumerator CaptureSingleFrameRoutine()
    {
        isCapturingFrame = true;
        yield return new WaitForEndOfFrame();

        int frameIndex = captureIndex++;
        string frameName = "frame_" + frameIndex.ToString("D6");
        bool previousSegmentationMode = segmentationManager != null && segmentationManager.IsSegmentationMode;

        string rgbPath = Path.Combine(outputRoot, "rgb", frameName + "_rgb.png");
        string maskPath = Path.Combine(outputRoot, "mask", frameName + "_mask.png");
        string metadataPath = Path.Combine(outputRoot, "metadata", frameName + ".json");

        if (segmentationManager != null)
        {
            segmentationManager.SetSegmentationMode(false);
        }

        CaptureCameraPng(cameraManager.VehicleCamera, rgbPath);

        if (segmentationManager != null)
        {
            segmentationManager.SetSegmentationMode(true);
        }

        CaptureCameraPng(cameraManager.VehicleCamera, maskPath);

        if (segmentationManager != null)
        {
            segmentationManager.SetSegmentationMode(previousSegmentationMode);
        }

        WriteFrameMetadata(metadataPath, frameIndex, rgbPath, maskPath);
        Debug.Log("Dataset capture saved: " + frameName);
        isCapturingFrame = false;
    }

    private void CaptureCameraPng(Camera sourceCamera, string filePath)
    {
        RenderTexture previousTarget = sourceCamera.targetTexture;
        RenderTexture previousActive = RenderTexture.active;

        RenderTexture renderTexture = RenderTexture.GetTemporary(captureWidth, captureHeight, 24, RenderTextureFormat.ARGB32);
        Texture2D texture = new Texture2D(captureWidth, captureHeight, TextureFormat.RGB24, false);

        sourceCamera.targetTexture = renderTexture;
        sourceCamera.Render();
        RenderTexture.active = renderTexture;
        texture.ReadPixels(new Rect(0f, 0f, captureWidth, captureHeight), 0, 0);
        texture.Apply();

        File.WriteAllBytes(filePath, texture.EncodeToPNG());

        sourceCamera.targetTexture = previousTarget;
        RenderTexture.active = previousActive;
        RenderTexture.ReleaseTemporary(renderTexture);
        Destroy(texture);
    }

    private void EnsureOutputDirectories()
    {
        Directory.CreateDirectory(outputRoot);
        Directory.CreateDirectory(Path.Combine(outputRoot, "rgb"));
        Directory.CreateDirectory(Path.Combine(outputRoot, "mask"));
        Directory.CreateDirectory(Path.Combine(outputRoot, "metadata"));
    }

    private void WriteClassDefinitions()
    {
        string path = Path.Combine(outputRoot, "segmentation_classes.json");
        ClassDefinitionList list = new ClassDefinitionList();

        foreach (SegmentationClassRegistry.ClassDefinition classDefinition in SegmentationClassRegistry.GetClasses())
        {
            list.classes.Add(new SerializableClassDefinition(classDefinition));
        }

        File.WriteAllText(path, JsonUtility.ToJson(list, true));
    }

    private void WriteFrameMetadata(string path, int frameIndex, string rgbPath, string maskPath)
    {
        CaptureFrameMetadata metadata = new CaptureFrameMetadata
        {
            frameIndex = frameIndex,
            timestampUtc = DateTime.UtcNow.ToString("o"),
            unityTime = Time.time,
            rgbPath = ToUnityRelativePath(rgbPath),
            maskPath = ToUnityRelativePath(maskPath),
            segmentationModeDuringMask = true
        };

        if (vehicle != null)
        {
            metadata.vehiclePosition = SerializableVector3.FromVector3(vehicle.transform.position);
            metadata.vehicleRotationEuler = SerializableVector3.FromVector3(vehicle.transform.rotation.eulerAngles);
            metadata.vehicleSpeedKmh = vehicle.CurrentSpeedKmh;
        }

        if (cameraManager != null && cameraManager.VehicleCamera != null)
        {
            Camera camera = cameraManager.VehicleCamera;
            metadata.cameraPosition = SerializableVector3.FromVector3(camera.transform.position);
            metadata.cameraRotationEuler = SerializableVector3.FromVector3(camera.transform.rotation.eulerAngles);
            metadata.cameraFieldOfView = camera.fieldOfView;
            metadata.captureWidth = captureWidth;
            metadata.captureHeight = captureHeight;
        }

        File.WriteAllText(path, JsonUtility.ToJson(metadata, true));
    }

    private string ToUnityRelativePath(string absolutePath)
    {
        string normalized = absolutePath.Replace("\\", "/");
        string projectRoot = GetProjectRoot().Replace("\\", "/");
        if (normalized.StartsWith(projectRoot))
        {
            string relativePath = normalized.Substring(projectRoot.Length).TrimStart('/');
            return relativePath;
        }

        string dataPath = Application.dataPath.Replace("\\", "/");
        if (normalized.StartsWith(dataPath))
        {
            return "Assets" + normalized.Substring(dataPath.Length);
        }

        return normalized;
    }

    private string GetProjectRoot()
    {
        DirectoryInfo dataDirectory = Directory.GetParent(Application.dataPath);
        return dataDirectory != null ? dataDirectory.FullName : Application.dataPath;
    }

    private int GetNextCaptureIndex()
    {
        string rgbDirectory = Path.Combine(outputRoot, "rgb");
        if (!Directory.Exists(rgbDirectory))
        {
            return 0;
        }

        int maxIndex = -1;
        string[] files = Directory.GetFiles(rgbDirectory, "frame_*_rgb.png");
        foreach (string file in files)
        {
            string fileName = Path.GetFileNameWithoutExtension(file);
            if (fileName.StartsWith("frame_") && fileName.EndsWith("_rgb"))
            {
                string numberText = fileName.Substring(6, fileName.Length - 10);
                if (int.TryParse(numberText, out int parsedIndex))
                {
                    maxIndex = Mathf.Max(maxIndex, parsedIndex);
                }
            }
        }

        return maxIndex + 1;
    }

    [Serializable]
    private class ClassDefinitionList
    {
        public System.Collections.Generic.List<SerializableClassDefinition> classes = new System.Collections.Generic.List<SerializableClassDefinition>();
    }

    [Serializable]
    private class SerializableClassDefinition
    {
        public int classId;
        public string className;
        public string tagName;
        public string layerName;
        public SerializableColor color;

        public SerializableClassDefinition(SegmentationClassRegistry.ClassDefinition definition)
        {
            classId = definition.classId;
            className = definition.className;
            tagName = definition.tagName;
            layerName = definition.tagName;
            color = SerializableColor.FromColor(definition.color);
        }
    }

    [Serializable]
    private class CaptureFrameMetadata
    {
        public int frameIndex;
        public string timestampUtc;
        public float unityTime;
        public string rgbPath;
        public string maskPath;
        public bool segmentationModeDuringMask;
        public int captureWidth;
        public int captureHeight;
        public SerializableVector3 vehiclePosition;
        public SerializableVector3 vehicleRotationEuler;
        public float vehicleSpeedKmh;
        public SerializableVector3 cameraPosition;
        public SerializableVector3 cameraRotationEuler;
        public float cameraFieldOfView;
    }

    [Serializable]
    private struct SerializableVector3
    {
        public float x;
        public float y;
        public float z;

        public static SerializableVector3 FromVector3(Vector3 value)
        {
            return new SerializableVector3 { x = value.x, y = value.y, z = value.z };
        }
    }

    [Serializable]
    private struct SerializableColor
    {
        public float r;
        public float g;
        public float b;
        public float a;
        public string hexRgb;

        public static SerializableColor FromColor(Color value)
        {
            return new SerializableColor
            {
                r = value.r,
                g = value.g,
                b = value.b,
                a = value.a,
                hexRgb = "#" + ColorUtility.ToHtmlStringRGB(value)
            };
        }
    }
}
