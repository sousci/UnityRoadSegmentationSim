using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central definition for segmentation classes used by materials, legends, and dataset metadata.
/// Keep this in sync with SceneBuilder class IDs and segmentation colors.
/// </summary>
public static class SegmentationClassRegistry
{
    public struct ClassDefinition
    {
        public int classId;
        public string className;
        public string tagName;
        public Color color;

        public ClassDefinition(int id, string name, string tag, Color classColor)
        {
            classId = id;
            className = name;
            tagName = tag;
            color = classColor;
        }
    }

    public static readonly ClassDefinition[] Classes =
    {
        new ClassDefinition(1, "normal_road", "Road_Normal", new Color(0.2f, 0.2f, 0.2f)),
        new ClassDefinition(2, "sidewalk", "Sidewalk", new Color(0.55f, 0.35f, 0.1f)),
        new ClassDefinition(3, "lane_line", "LaneLine", Color.yellow),
        new ClassDefinition(4, "crosswalk", "Crosswalk", new Color(1f, 0f, 1f)),
        new ClassDefinition(5, "puddle", "Road_Puddle", new Color(0f, 0.4f, 1f)),
        new ClassDefinition(6, "crack", "Road_Crack", Color.black),
        new ClassDefinition(7, "bump", "Road_Bump", new Color(1f, 0.5f, 0f)),
        new ClassDefinition(8, "hole", "Road_Hole", new Color(0.45f, 0f, 0f)),
        new ClassDefinition(9, "construction_area", "Road_Construction", Color.red),
        new ClassDefinition(10, "obstacle", "Road_Obstacle", Color.cyan),
        new ClassDefinition(11, "building", "Building", new Color(0.35f, 0.1f, 0.7f)),
        new ClassDefinition(12, "traffic_light", "TrafficLight", new Color(1f, 0.85f, 0f)),
        new ClassDefinition(13, "pedestrian_area", "PedestrianArea", Color.green),
        new ClassDefinition(14, "background", "Background", new Color(0.1f, 0.1f, 0.1f))
    };

    public static IReadOnlyList<ClassDefinition> GetClasses()
    {
        return Classes;
    }
}
