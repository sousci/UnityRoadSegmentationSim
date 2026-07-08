using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Switches all RoadObjectInfo objects between realistic display materials and flat class colors.
/// This is the first step toward generating segmentation masks from VehicleCamera.
/// </summary>
public class SegmentationMaterialManager : MonoBehaviour
{
    public bool IsSegmentationMode { get; private set; }

    private readonly List<RoadObjectInfo> trackedObjects = new List<RoadObjectInfo>();

    public void Register(RoadObjectInfo info)
    {
        if (info == null || trackedObjects.Contains(info))
        {
            return;
        }

        trackedObjects.Add(info);
        if (IsSegmentationMode)
        {
            info.ApplySegmentationMaterial();
        }
        else
        {
            info.ApplyNormalMaterial();
        }
    }

    public void RefreshFromScene()
    {
        trackedObjects.Clear();
        RoadObjectInfo[] infos = FindObjectsOfType<RoadObjectInfo>();
        foreach (RoadObjectInfo info in infos)
        {
            Register(info);
        }
    }

    public void ToggleMode()
    {
        SetSegmentationMode(!IsSegmentationMode);
    }

    public void SetSegmentationMode(bool enabled)
    {
        IsSegmentationMode = enabled;

        foreach (RoadObjectInfo info in trackedObjects)
        {
            if (info == null)
            {
                continue;
            }

            if (IsSegmentationMode)
            {
                info.ApplySegmentationMaterial();
            }
            else
            {
                info.ApplyNormalMaterial();
            }
        }
    }
}
