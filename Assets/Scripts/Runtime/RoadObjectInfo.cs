using UnityEngine;

/// <summary>
/// Holds semantic information used by future segmentation, dataset export, and debug tools.
/// Attach this to every object that should have a normal material and a segmentation material.
/// </summary>
public class RoadObjectInfo : MonoBehaviour
{
    [Header("Segmentation Class")]
    public string className = "background";
    public int classId = 0;

    [Header("Materials")]
    public Material normalMaterial;
    public Material segmentationMaterial;

    public void ApplyNormalMaterial()
    {
        ApplyMaterial(normalMaterial);
    }

    public void ApplySegmentationMaterial()
    {
        ApplyMaterial(segmentationMaterial);
    }

    private void ApplyMaterial(Material material)
    {
        if (material == null)
        {
            return;
        }

        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer targetRenderer in renderers)
        {
            targetRenderer.sharedMaterial = material;
        }
    }
}
