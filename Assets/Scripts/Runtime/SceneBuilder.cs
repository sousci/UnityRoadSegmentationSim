using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds the Phase 1 city block from Unity primitives.
/// Generated objects are grouped so they can be adjusted manually later.
/// </summary>
public class SceneBuilder : MonoBehaviour
{
    public VehicleController Vehicle { get; private set; }
    public Camera MainCamera { get; private set; }
    public Camera VehicleCamera { get; private set; }
    public Camera TopViewCamera { get; private set; }

    private SegmentationMaterialManager segmentationManager;
    private readonly Dictionary<string, Material> normalMaterials = new Dictionary<string, Material>();
    private readonly Dictionary<string, Material> segmentationMaterials = new Dictionary<string, Material>();
    private Material vehiclePaintMaterial;
    private Material vehicleWindowMaterial;
    private Material vehicleTireMaterial;
    private Material vehicleLightMaterial;
    private Material vehicleTailLightMaterial;

    private Transform environmentRoot;
    private Transform roadsRoot;
    private Transform sidewalksRoot;
    private Transform buildingsRoot;
    private Transform roadMarksRoot;
    private Transform roadDamagesRoot;
    private Transform trafficRoot;
    private Transform propsRoot;
    private Transform vehiclesRoot;
    private Transform camerasRoot;
    private readonly float[] gridX = { -55f, 0f, 55f };
    private readonly float[] gridZ = { -38f, 0f, 38f };
    private const float MapWidth = 180f;
    private const float MapDepth = 140f;
    private const float RoadWidth = 8f;
    private const float SidewalkOffset = 6.25f;

    public void Build(SegmentationMaterialManager manager)
    {
        segmentationManager = manager;
        CreateMaterials();
        CreateRoots();
        CreateLighting();
        CreateRoads();
        CreateSidewalks();
        CreateRoadMarks();
        CreateBuildings();
        CreateTrafficObjects();
        CreateProps();
        CreateRoadDamages();
        CreateVehicle();
        CreateCameras();
        segmentationManager.RefreshFromScene();
    }

    private void CreateRoots()
    {
        environmentRoot = CreateRoot("Environment");
        roadsRoot = CreateRoot("Roads", environmentRoot);
        sidewalksRoot = CreateRoot("Sidewalks", environmentRoot);
        buildingsRoot = CreateRoot("Buildings", environmentRoot);
        roadMarksRoot = CreateRoot("RoadMarks", environmentRoot);
        roadDamagesRoot = CreateRoot("RoadDamages", environmentRoot);
        trafficRoot = CreateRoot("TrafficObjects", environmentRoot);
        propsRoot = CreateRoot("Props", environmentRoot);
        vehiclesRoot = CreateRoot("Vehicles");
        camerasRoot = CreateRoot("Cameras");
    }

    private Transform CreateRoot(string objectName, Transform parent = null)
    {
        GameObject root = new GameObject(objectName);
        root.transform.SetParent(parent, false);
        return root.transform;
    }

    private void CreateMaterials()
    {
        AddClassMaterials("normal_road", new Color(0.20f, 0.20f, 0.19f), new Color(0.2f, 0.2f, 0.2f));
        AddClassMaterials("sidewalk", new Color(0.60f, 0.58f, 0.52f), new Color(0.55f, 0.35f, 0.1f));
        AddClassMaterials("lane_line", new Color(0.92f, 0.90f, 0.84f), Color.yellow);
        AddClassMaterials("crosswalk", new Color(0.92f, 0.90f, 0.84f), new Color(1f, 0f, 1f));
        AddClassMaterials("puddle", new Color(0.06f, 0.18f, 0.28f, 0.85f), new Color(0f, 0.4f, 1f));
        AddClassMaterials("crack", new Color(0.02f, 0.02f, 0.02f), new Color(0f, 0f, 0f));
        AddClassMaterials("bump", new Color(0.33f, 0.28f, 0.21f), new Color(1f, 0.5f, 0f));
        AddClassMaterials("hole", new Color(0.01f, 0.01f, 0.01f), new Color(0.45f, 0f, 0f));
        AddClassMaterials("construction_area", new Color(0.95f, 0.45f, 0.05f), new Color(1f, 0f, 0f));
        AddClassMaterials("obstacle", new Color(0.45f, 0.25f, 0.12f), new Color(0f, 1f, 1f));
        AddClassMaterials("building", new Color(0.52f, 0.55f, 0.57f), new Color(0.35f, 0.1f, 0.7f));
        AddClassMaterials("traffic_light", new Color(0.05f, 0.05f, 0.05f), new Color(1f, 0.85f, 0f));
        AddClassMaterials("pedestrian_area", new Color(0.36f, 0.40f, 0.31f), new Color(0f, 1f, 0f));
        AddClassMaterials("background", new Color(0.38f, 0.44f, 0.34f), new Color(0.1f, 0.1f, 0.1f));

        vehiclePaintMaterial = CreateMaterial("MAT_Normal_vehicle_paint", new Color(0.72f, 0.36f, 0.14f));
        vehicleWindowMaterial = CreateMaterial("MAT_Normal_vehicle_glass", new Color(0.06f, 0.09f, 0.12f));
        vehicleTireMaterial = CreateMaterial("MAT_Normal_vehicle_tire", new Color(0.015f, 0.015f, 0.015f));
        vehicleLightMaterial = CreateMaterial("MAT_Normal_vehicle_headlight", new Color(1f, 0.92f, 0.62f));
        vehicleTailLightMaterial = CreateMaterial("MAT_Normal_vehicle_taillight", new Color(0.8f, 0.05f, 0.03f));
    }

    private void AddClassMaterials(string className, Color normalColor, Color segmentationColor)
    {
        normalMaterials[className] = CreateMaterial("MAT_Normal_" + className, normalColor);
        segmentationMaterials[className] = CreateMaterial("MAT_Seg_" + className, segmentationColor);
    }

    private Material CreateMaterial(string materialName, Color color)
    {
        Material material = new Material(Shader.Find("Standard"));
        material.name = materialName;
        material.color = color;
        material.SetFloat("_Glossiness", 0.22f);
        return material;
    }

    private void CreateLighting()
    {
        GameObject lightObject = new GameObject("DirectionalLight_Sun");
        lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        Light sun = lightObject.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.intensity = 1.25f;
        sun.shadows = LightShadows.Soft;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.58f, 0.64f, 0.70f);

        CreateSemanticCube("Ground_Background", new Vector3(0f, -0.08f, 0f), new Vector3(MapWidth + 24f, 0.08f, MapDepth + 24f), "background", 14, "Background", environmentRoot);
    }

    private void CreateRoads()
    {
        foreach (float z in gridZ)
        {
            CreateSemanticCube("Road_Normal_EastWest_Z" + z.ToString("0"), new Vector3(0f, 0f, z), new Vector3(MapWidth, 0.12f, RoadWidth), "normal_road", 1, "Road_Normal", roadsRoot);
            CreateSemanticCube("Road_Shoulder_EastWest_North_Z" + z.ToString("0"), new Vector3(0f, 0.06f, z + RoadWidth * 0.5f + 0.35f), new Vector3(MapWidth, 0.08f, 0.5f), "normal_road", 1, "Road_Normal", roadsRoot);
            CreateSemanticCube("Road_Shoulder_EastWest_South_Z" + z.ToString("0"), new Vector3(0f, 0.06f, z - RoadWidth * 0.5f - 0.35f), new Vector3(MapWidth, 0.08f, 0.5f), "normal_road", 1, "Road_Normal", roadsRoot);
        }

        foreach (float x in gridX)
        {
            CreateSemanticCube("Road_Normal_NorthSouth_X" + x.ToString("0"), new Vector3(x, 0.01f, 0f), new Vector3(RoadWidth, 0.12f, MapDepth), "normal_road", 1, "Road_Normal", roadsRoot);
            CreateSemanticCube("Road_Shoulder_NorthSouth_East_X" + x.ToString("0"), new Vector3(x + RoadWidth * 0.5f + 0.35f, 0.07f, 0f), new Vector3(0.5f, 0.08f, MapDepth), "normal_road", 1, "Road_Normal", roadsRoot);
            CreateSemanticCube("Road_Shoulder_NorthSouth_West_X" + x.ToString("0"), new Vector3(x - RoadWidth * 0.5f - 0.35f, 0.07f, 0f), new Vector3(0.5f, 0.08f, MapDepth), "normal_road", 1, "Road_Normal", roadsRoot);
        }
    }

    private void CreateSidewalks()
    {
        foreach (float z in gridZ)
        {
            CreateSemanticCube("Sidewalk_EastWest_North_Z" + z.ToString("0"), new Vector3(0f, 0.11f, z + SidewalkOffset), new Vector3(MapWidth, 0.22f, 2.8f), "sidewalk", 2, "Sidewalk", sidewalksRoot);
            CreateSemanticCube("Sidewalk_EastWest_South_Z" + z.ToString("0"), new Vector3(0f, 0.11f, z - SidewalkOffset), new Vector3(MapWidth, 0.22f, 2.8f), "sidewalk", 2, "Sidewalk", sidewalksRoot);
        }

        foreach (float x in gridX)
        {
            CreateSemanticCube("Sidewalk_NorthSouth_East_X" + x.ToString("0"), new Vector3(x + SidewalkOffset, 0.12f, 0f), new Vector3(2.8f, 0.22f, MapDepth), "sidewalk", 2, "Sidewalk", sidewalksRoot);
            CreateSemanticCube("Sidewalk_NorthSouth_West_X" + x.ToString("0"), new Vector3(x - SidewalkOffset, 0.12f, 0f), new Vector3(2.8f, 0.22f, MapDepth), "sidewalk", 2, "Sidewalk", sidewalksRoot);
        }

        CreateSemanticCube("PedestrianArea_Plaza_East", new Vector3(76f, 0.13f, 22f), new Vector3(16f, 0.08f, 12f), "pedestrian_area", 13, "PedestrianArea", sidewalksRoot);
        CreateSemanticCube("PedestrianArea_Plaza_West", new Vector3(-76f, 0.13f, -22f), new Vector3(16f, 0.08f, 12f), "pedestrian_area", 13, "PedestrianArea", sidewalksRoot);
    }

    private void CreateRoadMarks()
    {
        for (int i = -13; i <= 13; i++)
        {
            float x = i * 6f;
            if (IsNearAny(x, gridX, 10f))
            {
                continue;
            }

            foreach (float z in gridZ)
            {
                RemoveCollider(CreateSemanticCube("LaneLine_EastWest_Z" + z.ToString("0") + "_" + i, new Vector3(x, 0.067f, z), new Vector3(3.5f, 0.012f, 0.16f), "lane_line", 3, "LaneLine", roadMarksRoot));
            }
        }

        for (int i = -10; i <= 10; i++)
        {
            float z = i * 6f;
            if (IsNearAny(z, gridZ, 10f))
            {
                continue;
            }

            foreach (float x in gridX)
            {
                RemoveCollider(CreateSemanticCube("LaneLine_NorthSouth_X" + x.ToString("0") + "_" + i, new Vector3(x, 0.077f, z), new Vector3(0.16f, 0.012f, 3.5f), "lane_line", 3, "LaneLine", roadMarksRoot));
            }
        }

        foreach (float x in gridX)
        {
            foreach (float z in gridZ)
            {
                CreateCrosswalk("Crosswalk_West_X" + x.ToString("0") + "_Z" + z.ToString("0"), new Vector3(x - 7.5f, 0.077f, z), true);
                CreateCrosswalk("Crosswalk_East_X" + x.ToString("0") + "_Z" + z.ToString("0"), new Vector3(x + 7.5f, 0.077f, z), true);
                CreateCrosswalk("Crosswalk_North_X" + x.ToString("0") + "_Z" + z.ToString("0"), new Vector3(x, 0.077f, z + 7.5f), false);
                CreateCrosswalk("Crosswalk_South_X" + x.ToString("0") + "_Z" + z.ToString("0"), new Vector3(x, 0.077f, z - 7.5f), false);
            }
        }
    }

    private bool IsNearAny(float value, float[] centers, float threshold)
    {
        foreach (float center in centers)
        {
            if (Mathf.Abs(value - center) <= threshold)
            {
                return true;
            }
        }

        return false;
    }

    private void CreateCrosswalk(string baseName, Vector3 center, bool stripesAlongZ)
    {
        for (int i = -3; i <= 3; i++)
        {
            Vector3 position = center + (stripesAlongZ ? new Vector3(0f, 0f, i * 0.75f) : new Vector3(i * 0.75f, 0f, 0f));
            Vector3 scale = stripesAlongZ ? new Vector3(4f, 0.012f, 0.28f) : new Vector3(0.28f, 0.012f, 4f);
            GameObject stripe = CreateSemanticCube(baseName + "_Stripe_" + (i + 4), position, scale, "crosswalk", 4, "Crosswalk", roadMarksRoot);
            RemoveCollider(stripe);
        }
    }

    private void CreateBuildings()
    {
        Vector3[] positions =
        {
            new Vector3(-82f, 3.5f, 56f), new Vector3(-66f, 5f, 20f), new Vector3(-80f, 4f, -54f),
            new Vector3(-35f, 4.5f, 56f), new Vector3(-22f, 3.5f, 20f), new Vector3(-36f, 5.5f, -54f),
            new Vector3(24f, 4f, 56f), new Vector3(34f, 5f, 20f), new Vector3(20f, 3.5f, -54f),
            new Vector3(74f, 5.5f, 56f), new Vector3(82f, 4.5f, 18f), new Vector3(76f, 4f, -54f),
            new Vector3(-82f, 4f, -18f), new Vector3(-18f, 5f, -18f), new Vector3(78f, 5f, -18f)
        };

        for (int i = 0; i < positions.Length; i++)
        {
            Vector3 scale = new Vector3(7f + (i % 3), positions[i].y * 2f, 6f + (i % 2) * 2f);
            GameObject building = CreateSemanticCube("Building_Block_" + (i + 1), positions[i], scale, "building", 11, "Building", buildingsRoot);
            AddBuildingWindows(building, i);
        }
    }

    private void AddBuildingWindows(GameObject building, int buildingIndex)
    {
        Material windowMaterial = CreateMaterial("MAT_Normal_BuildingWindow_" + buildingIndex, new Color(0.20f, 0.24f, 0.27f));
        Bounds bounds = building.GetComponent<Renderer>().bounds;
        int rows = Mathf.Max(2, Mathf.FloorToInt(bounds.size.y / 1.6f));
        int columns = Mathf.Max(2, Mathf.FloorToInt(bounds.size.x / 1.6f));

        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                if ((row + column + buildingIndex) % 3 == 0)
                {
                    continue;
                }

                float x = bounds.min.x + 0.8f + column * (bounds.size.x - 1.6f) / Mathf.Max(1, columns - 1);
                float y = bounds.min.y + 1.1f + row * 1.25f;
                float z = bounds.min.z - 0.035f;
                GameObject window = CreateSemanticCube(building.name + "_Window_Front_" + row + "_" + column, new Vector3(x, y, z), new Vector3(0.46f, 0.38f, 0.025f), "building", 11, "Building", buildingsRoot, windowMaterial);
                RemoveCollider(window);
            }
        }
    }

    private void CreateTrafficObjects()
    {
        foreach (float x in gridX)
        {
            foreach (float z in gridZ)
            {
                CreateTrafficLight("TrafficLight_NorthEast_X" + x.ToString("0") + "_Z" + z.ToString("0"), new Vector3(x + 5.3f, 0f, z + 5.3f), Quaternion.Euler(0f, 225f, 0f));
                CreateTrafficLight("TrafficLight_SouthWest_X" + x.ToString("0") + "_Z" + z.ToString("0"), new Vector3(x - 5.3f, 0f, z - 5.3f), Quaternion.Euler(0f, 45f, 0f));
            }
        }
    }

    private void CreateTrafficLight(string objectName, Vector3 position, Quaternion rotation)
    {
        GameObject root = new GameObject(objectName);
        root.transform.SetParent(trafficRoot, false);
        root.transform.SetPositionAndRotation(position, rotation);

        CreateSemanticCylinder(objectName + "_Pole", root.transform.TransformPoint(new Vector3(0f, 1.8f, 0f)), new Vector3(0.12f, 1.8f, 0.12f), "traffic_light", 12, "TrafficLight", root.transform);
        GameObject head = CreateSemanticCube(objectName + "_Head", root.transform.TransformPoint(new Vector3(0f, 3.7f, 0.35f)), new Vector3(0.5f, 1.1f, 0.35f), "traffic_light", 12, "TrafficLight", root.transform);
        head.transform.rotation = rotation;

        CreateLightBulb(objectName + "_Red", root.transform.TransformPoint(new Vector3(0f, 4.05f, 0.56f)), Color.red, root.transform);
        CreateLightBulb(objectName + "_Yellow", root.transform.TransformPoint(new Vector3(0f, 3.7f, 0.56f)), Color.yellow, root.transform);
        CreateLightBulb(objectName + "_Green", root.transform.TransformPoint(new Vector3(0f, 3.35f, 0.56f)), Color.green, root.transform);
    }

    private void CreateLightBulb(string objectName, Vector3 position, Color color, Transform parent)
    {
        GameObject bulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bulb.name = objectName;
        bulb.transform.SetParent(parent, true);
        bulb.transform.position = position;
        bulb.transform.localScale = Vector3.one * 0.18f;
        bulb.GetComponent<Renderer>().sharedMaterial = CreateMaterial("MAT_" + objectName, color);
        AddInfo(bulb, "traffic_light", 12, "TrafficLight");
    }

    private void CreateProps()
    {
        for (int i = 0; i < 18; i++)
        {
            float x = -82f + i * 9.5f;
            CreateStreetLight("StreetLight_Center_North_" + i, new Vector3(x, 0f, 8.4f));
            CreateTree("Tree_Center_South_" + i, new Vector3(x + 3f, 0f, -9.5f));
            if (i % 2 == 0)
            {
                CreateStreetLight("StreetLight_NorthRoad_" + i, new Vector3(x, 0f, 46.4f));
                CreateTree("Tree_SouthRoad_" + i, new Vector3(x + 2f, 0f, -47.5f));
            }
        }
    }

    private void CreateStreetLight(string objectName, Vector3 position)
    {
        GameObject root = new GameObject(objectName);
        root.transform.SetParent(propsRoot, false);
        CreateSemanticCylinder(objectName + "_Pole", position + Vector3.up * 2f, new Vector3(0.1f, 2f, 0.1f), "background", 14, "Background", root.transform);
        CreateSemanticCube(objectName + "_Lamp", position + new Vector3(0.35f, 4f, 0f), new Vector3(0.7f, 0.14f, 0.25f), "background", 14, "Background", root.transform);
    }

    private void CreateTree(string objectName, Vector3 position)
    {
        GameObject root = new GameObject(objectName);
        root.transform.SetParent(propsRoot, false);
        CreateSemanticCylinder(objectName + "_Trunk", position + Vector3.up * 0.8f, new Vector3(0.35f, 0.8f, 0.35f), "background", 14, "Background", root.transform);
        GameObject crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        crown.name = objectName + "_Crown";
        crown.transform.SetParent(root.transform, false);
        crown.transform.position = position + Vector3.up * 2.1f;
        crown.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);
        crown.GetComponent<Renderer>().sharedMaterial = normalMaterials["pedestrian_area"];
        AddInfo(crown, "background", 14, "Background");
    }

    private void CreateRoadDamages()
    {
        RemoveCollider(CreateSemanticCylinder("Road_Puddle_01", new Vector3(-18f, 0.067f, -1.7f), new Vector3(2.2f, 0.012f, 1.2f), "puddle", 5, "Road_Puddle", roadDamagesRoot));
        RemoveCollider(CreateSemanticCylinder("Road_Puddle_02", new Vector3(42f, 0.067f, 36.3f), new Vector3(2.0f, 0.012f, 1.1f), "puddle", 5, "Road_Puddle", roadDamagesRoot));
        RemoveCollider(CreateSemanticCylinder("Road_Puddle_03", new Vector3(-64f, 0.067f, -36.1f), new Vector3(1.8f, 0.012f, 1.0f), "puddle", 5, "Road_Puddle", roadDamagesRoot));
        CreateCrack("Road_Crack_01", new Vector3(-8f, 0.067f, 2.2f));
        CreateCrack("Road_Crack_02", new Vector3(55f, 0.077f, -18f));
        CreateCrack("Road_Crack_03", new Vector3(-55f, 0.077f, 22f));
        RemoveCollider(CreateSemanticCube("Road_Bump_01", new Vector3(14f, 0.12f, -1.8f), new Vector3(3.2f, 0.12f, 1.0f), "bump", 7, "Road_Bump", roadDamagesRoot));
        RemoveCollider(CreateSemanticCube("Road_Bump_02", new Vector3(-28f, 0.12f, 38f), new Vector3(2.8f, 0.12f, 0.9f), "bump", 7, "Road_Bump", roadDamagesRoot));
        RemoveCollider(CreateSemanticCube("Road_Bump_03", new Vector3(70f, 0.12f, -38f), new Vector3(2.8f, 0.12f, 0.9f), "bump", 7, "Road_Bump", roadDamagesRoot));
        RemoveCollider(CreateSemanticCylinder("Road_Hole_01", new Vector3(22f, 0.073f, 1.7f), new Vector3(1.5f, 0.025f, 1.5f), "hole", 8, "Road_Hole", roadDamagesRoot));
        RemoveCollider(CreateSemanticCylinder("Road_Hole_02", new Vector3(-52f, 0.073f, -16f), new Vector3(1.3f, 0.025f, 1.3f), "hole", 8, "Road_Hole", roadDamagesRoot));
        RemoveCollider(CreateSemanticCylinder("Road_Hole_03", new Vector3(55f, 0.073f, 20f), new Vector3(1.4f, 0.025f, 1.4f), "hole", 8, "Road_Hole", roadDamagesRoot));
        RemoveCollider(CreateSemanticCube("Road_Construction_Zone_01", new Vector3(-1.8f, 0.077f, -20f), new Vector3(4f, 0.012f, 5f), "construction_area", 9, "Road_Construction", roadDamagesRoot));
        RemoveCollider(CreateSemanticCube("Road_Construction_Zone_02", new Vector3(36f, 0.077f, 38f), new Vector3(5f, 0.012f, 4f), "construction_area", 9, "Road_Construction", roadDamagesRoot));
        CreateSemanticCube("Road_Obstacle_Box_01", new Vector3(8f, 0.45f, 2.4f), new Vector3(1.2f, 0.55f, 0.9f), "obstacle", 10, "Road_Obstacle", roadDamagesRoot);
        CreateSemanticCube("Road_Obstacle_Box_02", new Vector3(-38f, 0.45f, -35.8f), new Vector3(1.1f, 0.5f, 0.85f), "obstacle", 10, "Road_Obstacle", roadDamagesRoot);
        CreateSemanticCube("Road_Obstacle_Box_03", new Vector3(62f, 0.45f, 1.9f), new Vector3(1.0f, 0.5f, 0.8f), "obstacle", 10, "Road_Obstacle", roadDamagesRoot);

        for (int i = 0; i < 4; i++)
        {
            CreateSemanticCylinder("Road_Construction_Cone_" + i, new Vector3(-3.4f + i * 1.1f, 0.45f, -17.7f), new Vector3(0.45f, 0.45f, 0.45f), "construction_area", 9, "Road_Construction", roadDamagesRoot);
            CreateSemanticCylinder("Road_Construction_Cone_B_" + i, new Vector3(34.2f + i * 1.1f, 0.45f, 40.2f), new Vector3(0.45f, 0.45f, 0.45f), "construction_area", 9, "Road_Construction", roadDamagesRoot);
        }
    }

    private void CreateCrack(string objectName, Vector3 center)
    {
        GameObject root = new GameObject(objectName);
        root.transform.SetParent(roadDamagesRoot, false);
        for (int i = 0; i < 5; i++)
        {
            GameObject segment = CreateSemanticCube(objectName + "_Segment_" + i, center + new Vector3(i * 0.55f, 0f, Mathf.Sin(i) * 0.35f), new Vector3(0.75f, 0.012f, 0.08f), "crack", 6, "Road_Crack", root.transform);
            segment.transform.rotation = Quaternion.Euler(0f, 20f + i * 16f, 0f);
            RemoveCollider(segment);
        }
    }

    private void CreateVehicle()
    {
        GameObject vehicleRoot = new GameObject("Vehicle_Ego");
        vehicleRoot.transform.SetParent(vehiclesRoot, false);
        vehicleRoot.transform.position = new Vector3(-74f, 0.75f, -1.8f);
        vehicleRoot.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

        Rigidbody rb = vehicleRoot.AddComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        BoxCollider collider = vehicleRoot.AddComponent<BoxCollider>();
        collider.center = new Vector3(0f, 0.15f, 0f);
        collider.size = new Vector3(2.1f, 1.1f, 4.1f);

        Vehicle = vehicleRoot.AddComponent<VehicleController>();

        CreateDetailedVehicleVisual(vehicleRoot.transform, "Vehicle_Ego", vehiclePaintMaterial);
        CreateTrafficVehicle("Vehicle_Traffic_Sedan_01", new Vector3(-18f, 0.75f, 1.8f), Quaternion.Euler(0f, 90f, 0f), new Color(0.62f, 0.64f, 0.65f));
        CreateTrafficVehicle("Vehicle_Traffic_Hatchback_02", new Vector3(18f, 0.75f, -1.8f), Quaternion.Euler(0f, -90f, 0f), new Color(0.16f, 0.28f, 0.55f));
        CreateTrafficVehicle("Vehicle_Traffic_Van_03", new Vector3(2.4f, 0.85f, 18f), Quaternion.Euler(0f, 180f, 0f), new Color(0.82f, 0.82f, 0.74f));
        CreateTrafficVehicle("Vehicle_Traffic_Sedan_04", new Vector3(-48f, 0.75f, 36.2f), Quaternion.Euler(0f, -90f, 0f), new Color(0.86f, 0.88f, 0.9f));
        CreateTrafficVehicle("Vehicle_Traffic_Hatchback_05", new Vector3(48f, 0.75f, -36.2f), Quaternion.Euler(0f, 90f, 0f), new Color(0.1f, 0.2f, 0.62f));
        CreateTrafficVehicle("Vehicle_Traffic_Van_06", new Vector3(-55f, 0.85f, -22f), Quaternion.Euler(0f, 0f, 0f), new Color(0.75f, 0.74f, 0.68f));
    }

    private void CreateTrafficVehicle(string objectName, Vector3 position, Quaternion rotation, Color paintColor)
    {
        GameObject vehicleRoot = new GameObject(objectName);
        vehicleRoot.transform.SetParent(vehiclesRoot, false);
        vehicleRoot.transform.SetPositionAndRotation(position, rotation);

        BoxCollider collider = vehicleRoot.AddComponent<BoxCollider>();
        collider.center = new Vector3(0f, 0.15f, 0f);
        collider.size = new Vector3(2.05f, 1.15f, 4.2f);

        Material paint = CreateMaterial("MAT_Normal_" + objectName + "_Paint", paintColor);
        CreateDetailedVehicleVisual(vehicleRoot.transform, objectName, paint);
    }

    private void CreateDetailedVehicleVisual(Transform parent, string namePrefix, Material paintMaterial)
    {
        GameObject lowerBody = CreateSemanticCubeLocal(namePrefix + "_LowerBody", parent, new Vector3(0f, 0.13f, 0f), new Vector3(2.05f, 0.42f, 4.15f), "obstacle", 10, "Road_Obstacle", paintMaterial);
        GameObject upperBody = CreateSemanticCubeLocal(namePrefix + "_UpperBody", parent, new Vector3(0f, 0.48f, -0.12f), new Vector3(1.82f, 0.35f, 3.05f), "obstacle", 10, "Road_Obstacle", paintMaterial);
        GameObject cabin = CreateSemanticCubeLocal(namePrefix + "_Cabin", parent, new Vector3(0f, 0.9f, -0.25f), new Vector3(1.42f, 0.62f, 1.55f), "obstacle", 10, "Road_Obstacle", paintMaterial);
        RemoveCollider(lowerBody);
        RemoveCollider(upperBody);
        RemoveCollider(cabin);

        AddVehiclePanel(parent, namePrefix + "_Windshield", new Vector3(0f, 0.99f, 0.58f), new Vector3(1.24f, 0.36f, 0.035f), Quaternion.Euler(-22f, 0f, 0f), vehicleWindowMaterial);
        AddVehiclePanel(parent, namePrefix + "_RearWindow", new Vector3(0f, 0.99f, -1.06f), new Vector3(1.20f, 0.34f, 0.035f), Quaternion.Euler(20f, 0f, 0f), vehicleWindowMaterial);
        AddVehiclePanel(parent, namePrefix + "_LeftWindow", new Vector3(-0.735f, 0.94f, -0.25f), new Vector3(0.035f, 0.34f, 1.05f), Quaternion.identity, vehicleWindowMaterial);
        AddVehiclePanel(parent, namePrefix + "_RightWindow", new Vector3(0.735f, 0.94f, -0.25f), new Vector3(0.035f, 0.34f, 1.05f), Quaternion.identity, vehicleWindowMaterial);

        AddVehiclePanel(parent, namePrefix + "_FrontBumper", new Vector3(0f, 0.13f, 2.16f), new Vector3(1.9f, 0.24f, 0.18f), Quaternion.identity, vehicleTireMaterial);
        AddVehiclePanel(parent, namePrefix + "_RearBumper", new Vector3(0f, 0.13f, -2.16f), new Vector3(1.9f, 0.24f, 0.18f), Quaternion.identity, vehicleTireMaterial);
        AddVehiclePanel(parent, namePrefix + "_LeftHeadlight", new Vector3(-0.55f, 0.31f, 2.27f), new Vector3(0.42f, 0.18f, 0.035f), Quaternion.identity, vehicleLightMaterial);
        AddVehiclePanel(parent, namePrefix + "_RightHeadlight", new Vector3(0.55f, 0.31f, 2.27f), new Vector3(0.42f, 0.18f, 0.035f), Quaternion.identity, vehicleLightMaterial);
        AddVehiclePanel(parent, namePrefix + "_LeftTailLight", new Vector3(-0.55f, 0.31f, -2.27f), new Vector3(0.36f, 0.16f, 0.035f), Quaternion.identity, vehicleTailLightMaterial);
        AddVehiclePanel(parent, namePrefix + "_RightTailLight", new Vector3(0.55f, 0.31f, -2.27f), new Vector3(0.36f, 0.16f, 0.035f), Quaternion.identity, vehicleTailLightMaterial);

        Vector3[] wheelPositions =
        {
            new Vector3(-1.05f, -0.28f, 1.32f), new Vector3(1.05f, -0.28f, 1.32f),
            new Vector3(-1.05f, -0.28f, -1.32f), new Vector3(1.05f, -0.28f, -1.32f)
        };

        for (int i = 0; i < wheelPositions.Length; i++)
        {
            GameObject wheel = CreateSemanticCylinderLocal(namePrefix + "_Wheel_" + i, parent, wheelPositions[i], new Vector3(0.46f, 0.18f, 0.46f), "obstacle", 10, "Road_Obstacle", vehicleTireMaterial);
            wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            RemoveCollider(wheel);
        }
    }

    private void AddVehiclePanel(Transform parent, string objectName, Vector3 localPosition, Vector3 scale, Quaternion localRotation, Material material)
    {
        GameObject panel = CreateSemanticCubeLocal(objectName, parent, localPosition, scale, "obstacle", 10, "Road_Obstacle", material);
        panel.transform.localRotation = localRotation;
        RemoveCollider(panel);
    }

    private void CreateCameras()
    {
        MainCamera = CreateCamera("Main Camera", new Vector3(-58f, 32f, -62f), Quaternion.Euler(38f, 43f, 0f), camerasRoot);
        VehicleCamera = CreateCamera("VehicleCamera", Vector3.zero, Quaternion.identity, camerasRoot);
        VehicleCamera.transform.SetParent(Vehicle.transform, false);
        VehicleCamera.transform.localPosition = new Vector3(0f, 1.35f, 1.9f);
        VehicleCamera.transform.localRotation = Quaternion.Euler(8f, 0f, 0f);
        VehicleCamera.fieldOfView = 70f;

        TopViewCamera = CreateCamera("TopViewCamera", new Vector3(0f, 95f, 0f), Quaternion.Euler(90f, 0f, 0f), camerasRoot);
        TopViewCamera.orthographic = true;
        TopViewCamera.orthographicSize = 88f;
    }

    private Camera CreateCamera(string objectName, Vector3 position, Quaternion rotation, Transform parent)
    {
        GameObject cameraObject = new GameObject(objectName);
        cameraObject.transform.SetParent(parent, false);
        cameraObject.transform.SetPositionAndRotation(position, rotation);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.nearClipPlane = 0.05f;
        camera.farClipPlane = 500f;
        camera.backgroundColor = new Color(0.56f, 0.70f, 0.88f);
        cameraObject.AddComponent<AudioListener>();
        return camera;
    }

    private GameObject CreateSemanticCubeLocal(string objectName, Transform parent, Vector3 localPosition, Vector3 scale, string className, int classId, string tagName, Material displayMaterial = null)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = objectName;
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = localPosition;
        obj.transform.localRotation = Quaternion.identity;
        obj.transform.localScale = scale;
        obj.GetComponent<Renderer>().sharedMaterial = displayMaterial != null ? displayMaterial : normalMaterials[className];
        AddInfo(obj, className, classId, tagName);
        if (displayMaterial != null)
        {
            obj.GetComponent<RoadObjectInfo>().normalMaterial = displayMaterial;
        }
        return obj;
    }

    private GameObject CreateSemanticCube(string objectName, Vector3 position, Vector3 scale, string className, int classId, string tagName, Transform parent, Material displayMaterial = null)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = objectName;
        obj.transform.SetParent(parent, false);
        obj.transform.position = position;
        obj.transform.localScale = scale;
        obj.GetComponent<Renderer>().sharedMaterial = displayMaterial != null ? displayMaterial : normalMaterials[className];
        AddInfo(obj, className, classId, tagName);
        if (displayMaterial != null)
        {
            obj.GetComponent<RoadObjectInfo>().normalMaterial = displayMaterial;
        }
        return obj;
    }

    private GameObject CreateSemanticCylinder(string objectName, Vector3 position, Vector3 scale, string className, int classId, string tagName, Transform parent)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        obj.name = objectName;
        obj.transform.SetParent(parent, false);
        obj.transform.position = position;
        obj.transform.localScale = scale;
        obj.GetComponent<Renderer>().sharedMaterial = normalMaterials[className];
        AddInfo(obj, className, classId, tagName);
        return obj;
    }

    private GameObject CreateSemanticCylinderLocal(string objectName, Transform parent, Vector3 localPosition, Vector3 scale, string className, int classId, string tagName, Material displayMaterial = null)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        obj.name = objectName;
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = localPosition;
        obj.transform.localRotation = Quaternion.identity;
        obj.transform.localScale = scale;
        obj.GetComponent<Renderer>().sharedMaterial = displayMaterial != null ? displayMaterial : normalMaterials[className];
        AddInfo(obj, className, classId, tagName);
        if (displayMaterial != null)
        {
            obj.GetComponent<RoadObjectInfo>().normalMaterial = displayMaterial;
        }
        return obj;
    }

    private void RemoveCollider(GameObject obj)
    {
        Collider collider = obj.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }
    }

    private void AddInfo(GameObject obj, string className, int classId, string tagName)
    {
        RoadObjectInfo info = obj.AddComponent<RoadObjectInfo>();
        info.className = className;
        info.classId = classId;
        info.normalMaterial = normalMaterials[className];
        info.segmentationMaterial = segmentationMaterials[className];

        int layer = LayerMask.NameToLayer(tagName);
        obj.layer = layer >= 0 ? layer : 0;

        try
        {
            obj.tag = tagName;
        }
        catch (UnityException)
        {
            obj.tag = "Untagged";
        }

        if (segmentationManager != null)
        {
            segmentationManager.Register(info);
        }
    }
}
