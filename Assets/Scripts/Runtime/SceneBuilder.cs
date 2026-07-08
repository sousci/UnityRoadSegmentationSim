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
        AddClassMaterials("normal_road", new Color(0.16f, 0.16f, 0.16f), new Color(0.2f, 0.2f, 0.2f));
        AddClassMaterials("sidewalk", new Color(0.48f, 0.48f, 0.44f), new Color(0.55f, 0.35f, 0.1f));
        AddClassMaterials("lane_line", Color.white, Color.yellow);
        AddClassMaterials("crosswalk", Color.white, new Color(1f, 0f, 1f));
        AddClassMaterials("puddle", new Color(0.06f, 0.18f, 0.28f, 0.85f), new Color(0f, 0.4f, 1f));
        AddClassMaterials("crack", new Color(0.02f, 0.02f, 0.02f), new Color(0f, 0f, 0f));
        AddClassMaterials("bump", new Color(0.33f, 0.28f, 0.21f), new Color(1f, 0.5f, 0f));
        AddClassMaterials("hole", new Color(0.01f, 0.01f, 0.01f), new Color(0.45f, 0f, 0f));
        AddClassMaterials("construction_area", new Color(0.95f, 0.45f, 0.05f), new Color(1f, 0f, 0f));
        AddClassMaterials("obstacle", new Color(0.45f, 0.25f, 0.12f), new Color(0f, 1f, 1f));
        AddClassMaterials("building", new Color(0.54f, 0.56f, 0.58f), new Color(0.35f, 0.1f, 0.7f));
        AddClassMaterials("traffic_light", new Color(0.05f, 0.05f, 0.05f), new Color(1f, 0.85f, 0f));
        AddClassMaterials("pedestrian_area", new Color(0.35f, 0.46f, 0.32f), new Color(0f, 1f, 0f));
        AddClassMaterials("background", new Color(0.42f, 0.60f, 0.34f), new Color(0.1f, 0.1f, 0.1f));
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
        return material;
    }

    private void CreateLighting()
    {
        GameObject lightObject = new GameObject("DirectionalLight_Sun");
        lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        Light sun = lightObject.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.intensity = 1.1f;

        CreateSemanticCube("Ground_Background", new Vector3(0f, -0.08f, 0f), new Vector3(90f, 0.08f, 90f), "background", 14, "Background", environmentRoot);
    }

    private void CreateRoads()
    {
        CreateSemanticCube("Road_Normal_EastWest", new Vector3(0f, 0f, 0f), new Vector3(70f, 0.12f, 8f), "normal_road", 1, "Road_Normal", roadsRoot);
        CreateSemanticCube("Road_Normal_NorthSouth", new Vector3(0f, 0.01f, 0f), new Vector3(8f, 0.12f, 70f), "normal_road", 1, "Road_Normal", roadsRoot);

        // Shoulders are split outside the intersection so road-edge masks do not cross the center.
        CreateSemanticCube("Road_Shoulder_EastWest_North_Left", new Vector3(-21.5f, 0.06f, 4.35f), new Vector3(27f, 0.08f, 0.5f), "normal_road", 1, "Road_Normal", roadsRoot);
        CreateSemanticCube("Road_Shoulder_EastWest_North_Right", new Vector3(21.5f, 0.06f, 4.35f), new Vector3(27f, 0.08f, 0.5f), "normal_road", 1, "Road_Normal", roadsRoot);
        CreateSemanticCube("Road_Shoulder_EastWest_South_Left", new Vector3(-21.5f, 0.06f, -4.35f), new Vector3(27f, 0.08f, 0.5f), "normal_road", 1, "Road_Normal", roadsRoot);
        CreateSemanticCube("Road_Shoulder_EastWest_South_Right", new Vector3(21.5f, 0.06f, -4.35f), new Vector3(27f, 0.08f, 0.5f), "normal_road", 1, "Road_Normal", roadsRoot);
        CreateSemanticCube("Road_Shoulder_NorthSouth_East_North", new Vector3(4.35f, 0.07f, 21.5f), new Vector3(0.5f, 0.08f, 27f), "normal_road", 1, "Road_Normal", roadsRoot);
        CreateSemanticCube("Road_Shoulder_NorthSouth_East_South", new Vector3(4.35f, 0.07f, -21.5f), new Vector3(0.5f, 0.08f, 27f), "normal_road", 1, "Road_Normal", roadsRoot);
        CreateSemanticCube("Road_Shoulder_NorthSouth_West_North", new Vector3(-4.35f, 0.07f, 21.5f), new Vector3(0.5f, 0.08f, 27f), "normal_road", 1, "Road_Normal", roadsRoot);
        CreateSemanticCube("Road_Shoulder_NorthSouth_West_South", new Vector3(-4.35f, 0.07f, -21.5f), new Vector3(0.5f, 0.08f, 27f), "normal_road", 1, "Road_Normal", roadsRoot);
    }

    private void CreateSidewalks()
    {
        // Sidewalks are segmented so they stop before crosswalks and do not cover the intersection mask.
        CreateSemanticCube("Sidewalk_North_Left", new Vector3(-21.5f, 0.11f, 6.25f), new Vector3(27f, 0.22f, 2.8f), "sidewalk", 2, "Sidewalk", sidewalksRoot);
        CreateSemanticCube("Sidewalk_North_Right", new Vector3(21.5f, 0.11f, 6.25f), new Vector3(27f, 0.22f, 2.8f), "sidewalk", 2, "Sidewalk", sidewalksRoot);
        CreateSemanticCube("Sidewalk_South_Left", new Vector3(-21.5f, 0.11f, -6.25f), new Vector3(27f, 0.22f, 2.8f), "sidewalk", 2, "Sidewalk", sidewalksRoot);
        CreateSemanticCube("Sidewalk_South_Right", new Vector3(21.5f, 0.11f, -6.25f), new Vector3(27f, 0.22f, 2.8f), "sidewalk", 2, "Sidewalk", sidewalksRoot);
        CreateSemanticCube("Sidewalk_East_North", new Vector3(6.25f, 0.12f, 21.5f), new Vector3(2.8f, 0.22f, 27f), "sidewalk", 2, "Sidewalk", sidewalksRoot);
        CreateSemanticCube("Sidewalk_East_South", new Vector3(6.25f, 0.12f, -21.5f), new Vector3(2.8f, 0.22f, 27f), "sidewalk", 2, "Sidewalk", sidewalksRoot);
        CreateSemanticCube("Sidewalk_West_North", new Vector3(-6.25f, 0.12f, 21.5f), new Vector3(2.8f, 0.22f, 27f), "sidewalk", 2, "Sidewalk", sidewalksRoot);
        CreateSemanticCube("Sidewalk_West_South", new Vector3(-6.25f, 0.12f, -21.5f), new Vector3(2.8f, 0.22f, 27f), "sidewalk", 2, "Sidewalk", sidewalksRoot);

        CreateSemanticCube("Sidewalk_Corner_NorthEast", new Vector3(6.25f, 0.13f, 6.25f), new Vector3(2.8f, 0.22f, 2.8f), "sidewalk", 2, "Sidewalk", sidewalksRoot);
        CreateSemanticCube("Sidewalk_Corner_NorthWest", new Vector3(-6.25f, 0.13f, 6.25f), new Vector3(2.8f, 0.22f, 2.8f), "sidewalk", 2, "Sidewalk", sidewalksRoot);
        CreateSemanticCube("Sidewalk_Corner_SouthEast", new Vector3(6.25f, 0.13f, -6.25f), new Vector3(2.8f, 0.22f, 2.8f), "sidewalk", 2, "Sidewalk", sidewalksRoot);
        CreateSemanticCube("Sidewalk_Corner_SouthWest", new Vector3(-6.25f, 0.13f, -6.25f), new Vector3(2.8f, 0.22f, 2.8f), "sidewalk", 2, "Sidewalk", sidewalksRoot);

        CreateSemanticCube("PedestrianArea_Plaza", new Vector3(18f, 0.13f, 11f), new Vector3(12f, 0.08f, 6f), "pedestrian_area", 13, "PedestrianArea", sidewalksRoot);
    }

    private void CreateRoadMarks()
    {
        for (int i = -5; i <= 5; i++)
        {
            if (Mathf.Abs(i) <= 1)
            {
                continue;
            }

            RemoveCollider(CreateSemanticCube("LaneLine_EastWest_" + i, new Vector3(i * 6f, 0.18f, 0f), new Vector3(3.5f, 0.04f, 0.16f), "lane_line", 3, "LaneLine", roadMarksRoot));
            RemoveCollider(CreateSemanticCube("LaneLine_NorthSouth_" + i, new Vector3(0f, 0.19f, i * 6f), new Vector3(0.16f, 0.04f, 3.5f), "lane_line", 3, "LaneLine", roadMarksRoot));
        }

        CreateCrosswalk("Crosswalk_West", new Vector3(-7.5f, 0.2f, 0f), true);
        CreateCrosswalk("Crosswalk_East", new Vector3(7.5f, 0.2f, 0f), true);
        CreateCrosswalk("Crosswalk_North", new Vector3(0f, 0.21f, 7.5f), false);
        CreateCrosswalk("Crosswalk_South", new Vector3(0f, 0.21f, -7.5f), false);
    }

    private void CreateCrosswalk(string baseName, Vector3 center, bool stripesAlongZ)
    {
        for (int i = -3; i <= 3; i++)
        {
            Vector3 position = center + (stripesAlongZ ? new Vector3(0f, 0f, i * 0.75f) : new Vector3(i * 0.75f, 0f, 0f));
            Vector3 scale = stripesAlongZ ? new Vector3(4f, 0.04f, 0.28f) : new Vector3(0.28f, 0.04f, 4f);
            GameObject stripe = CreateSemanticCube(baseName + "_Stripe_" + (i + 4), position, scale, "crosswalk", 4, "Crosswalk", roadMarksRoot);
            RemoveCollider(stripe);
        }
    }

    private void CreateBuildings()
    {
        Vector3[] positions =
        {
            new Vector3(-24f, 3f, 14f), new Vector3(-12f, 4f, 15f), new Vector3(28f, 5f, 15f),
            new Vector3(-28f, 3.5f, -15f), new Vector3(16f, 4.5f, -15f), new Vector3(30f, 3f, -14f),
            new Vector3(14f, 4f, 28f), new Vector3(-14f, 4.5f, -28f)
        };

        for (int i = 0; i < positions.Length; i++)
        {
            Vector3 scale = new Vector3(7f + (i % 3), positions[i].y * 2f, 6f + (i % 2) * 2f);
            CreateSemanticCube("Building_Block_" + (i + 1), positions[i], scale, "building", 11, "Building", buildingsRoot);
        }
    }

    private void CreateTrafficObjects()
    {
        CreateTrafficLight("TrafficLight_NorthEast", new Vector3(5.3f, 0f, 5.3f), Quaternion.Euler(0f, 225f, 0f));
        CreateTrafficLight("TrafficLight_SouthWest", new Vector3(-5.3f, 0f, -5.3f), Quaternion.Euler(0f, 45f, 0f));
        CreateTrafficLight("TrafficLight_NorthWest", new Vector3(-5.3f, 0f, 5.3f), Quaternion.Euler(0f, 135f, 0f));
        CreateTrafficLight("TrafficLight_SouthEast", new Vector3(5.3f, 0f, -5.3f), Quaternion.Euler(0f, -45f, 0f));
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
        for (int i = 0; i < 8; i++)
        {
            float x = -30f + i * 8f;
            CreateStreetLight("StreetLight_North_" + i, new Vector3(x, 0f, 8.4f));
            CreateTree("Tree_South_" + i, new Vector3(x + 3f, 0f, -9.5f));
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
        RemoveCollider(CreateSemanticCylinder("Road_Puddle_01", new Vector3(-18f, 0.23f, -1.7f), new Vector3(2.2f, 0.03f, 1.2f), "puddle", 5, "Road_Puddle", roadDamagesRoot));
        CreateCrack("Road_Crack_01", new Vector3(-8f, 0.25f, 2.2f));
        RemoveCollider(CreateSemanticCube("Road_Bump_01", new Vector3(14f, 0.28f, -1.8f), new Vector3(3.2f, 0.28f, 1.0f), "bump", 7, "Road_Bump", roadDamagesRoot));
        RemoveCollider(CreateSemanticCylinder("Road_Hole_01", new Vector3(22f, 0.23f, 1.7f), new Vector3(1.5f, 0.08f, 1.5f), "hole", 8, "Road_Hole", roadDamagesRoot));
        RemoveCollider(CreateSemanticCube("Road_Construction_Zone_01", new Vector3(-1.8f, 0.26f, -20f), new Vector3(4f, 0.1f, 5f), "construction_area", 9, "Road_Construction", roadDamagesRoot));
        CreateSemanticCube("Road_Obstacle_Box_01", new Vector3(8f, 0.55f, 2.4f), new Vector3(1.2f, 0.8f, 0.9f), "obstacle", 10, "Road_Obstacle", roadDamagesRoot);

        for (int i = 0; i < 4; i++)
        {
            CreateSemanticCylinder("Road_Construction_Cone_" + i, new Vector3(-3.4f + i * 1.1f, 0.45f, -17.7f), new Vector3(0.45f, 0.45f, 0.45f), "construction_area", 9, "Road_Construction", roadDamagesRoot);
        }
    }

    private void CreateCrack(string objectName, Vector3 center)
    {
        GameObject root = new GameObject(objectName);
        root.transform.SetParent(roadDamagesRoot, false);
        for (int i = 0; i < 5; i++)
        {
            GameObject segment = CreateSemanticCube(objectName + "_Segment_" + i, center + new Vector3(i * 0.55f, 0f, Mathf.Sin(i) * 0.35f), new Vector3(0.75f, 0.035f, 0.08f), "crack", 6, "Road_Crack", root.transform);
            segment.transform.rotation = Quaternion.Euler(0f, 20f + i * 16f, 0f);
            RemoveCollider(segment);
        }
    }

    private void CreateVehicle()
    {
        GameObject vehicleRoot = new GameObject("Vehicle_Ego");
        vehicleRoot.transform.SetParent(vehiclesRoot, false);
        vehicleRoot.transform.position = new Vector3(-28f, 0.75f, -1.8f);
        vehicleRoot.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

        Rigidbody rb = vehicleRoot.AddComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        BoxCollider collider = vehicleRoot.AddComponent<BoxCollider>();
        collider.center = new Vector3(0f, 0.15f, 0f);
        collider.size = new Vector3(2.1f, 1.1f, 4.1f);

        Vehicle = vehicleRoot.AddComponent<VehicleController>();

        GameObject body = CreateSemanticCube("Vehicle_Body", Vector3.zero, new Vector3(2.1f, 0.7f, 4f), "obstacle", 10, "Road_Obstacle", vehicleRoot.transform);
        body.transform.localPosition = new Vector3(0f, 0.15f, 0f);
        RemoveCollider(body);

        GameObject cabin = CreateSemanticCube("Vehicle_Cabin", Vector3.zero, new Vector3(1.5f, 0.7f, 1.7f), "obstacle", 10, "Road_Obstacle", vehicleRoot.transform);
        cabin.transform.localPosition = new Vector3(0f, 0.85f, -0.25f);
        RemoveCollider(cabin);

        Vector3[] wheelPositions =
        {
            new Vector3(-1.15f, -0.35f, 1.35f), new Vector3(1.15f, -0.35f, 1.35f),
            new Vector3(-1.15f, -0.35f, -1.35f), new Vector3(1.15f, -0.35f, -1.35f)
        };

        for (int i = 0; i < wheelPositions.Length; i++)
        {
            GameObject wheel = CreateSemanticCylinder("Vehicle_Wheel_" + i, Vector3.zero, new Vector3(0.55f, 0.22f, 0.55f), "obstacle", 10, "Road_Obstacle", vehicleRoot.transform);
            wheel.transform.localPosition = wheelPositions[i];
            wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            RemoveCollider(wheel);
        }
    }

    private void CreateCameras()
    {
        MainCamera = CreateCamera("Main Camera", new Vector3(-22f, 16f, -22f), Quaternion.Euler(35f, 45f, 0f), camerasRoot);
        VehicleCamera = CreateCamera("VehicleCamera", Vector3.zero, Quaternion.identity, camerasRoot);
        VehicleCamera.transform.SetParent(Vehicle.transform, false);
        VehicleCamera.transform.localPosition = new Vector3(0f, 1.35f, 1.9f);
        VehicleCamera.transform.localRotation = Quaternion.Euler(8f, 0f, 0f);
        VehicleCamera.fieldOfView = 70f;

        TopViewCamera = CreateCamera("TopViewCamera", new Vector3(0f, 55f, 0f), Quaternion.Euler(90f, 0f, 0f), camerasRoot);
        TopViewCamera.orthographic = true;
        TopViewCamera.orthographicSize = 42f;
    }

    private Camera CreateCamera(string objectName, Vector3 position, Quaternion rotation, Transform parent)
    {
        GameObject cameraObject = new GameObject(objectName);
        cameraObject.transform.SetParent(parent, false);
        cameraObject.transform.SetPositionAndRotation(position, rotation);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.nearClipPlane = 0.05f;
        camera.farClipPlane = 500f;
        cameraObject.AddComponent<AudioListener>();
        return camera;
    }

    private GameObject CreateSemanticCube(string objectName, Vector3 position, Vector3 scale, string className, int classId, string tagName, Transform parent)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = objectName;
        obj.transform.SetParent(parent, false);
        obj.transform.position = position;
        obj.transform.localScale = scale;
        obj.GetComponent<Renderer>().sharedMaterial = normalMaterials[className];
        AddInfo(obj, className, classId, tagName);
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
