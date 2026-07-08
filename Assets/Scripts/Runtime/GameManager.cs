using UnityEngine;

/// <summary>
/// Entry point for Phase 1. It creates managers and builds the scene if no GameManager exists.
/// </summary>
public class GameManager : MonoBehaviour
{
    private SceneBuilder sceneBuilder;
    private SegmentationMaterialManager segmentationManager;
    private CameraManager cameraManager;
    private UIManager uiManager;
    private DatasetCaptureManager datasetCaptureManager;
    private RealtimeSegmentationManager realtimeSegmentationManager;
    private AutoDriveCaptureController autoDriveCaptureController;
    private bool isPaused;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateGameManagerIfNeeded()
    {
        if (FindObjectOfType<GameManager>() != null)
        {
            return;
        }

        GameObject managerObject = new GameObject("GameManager");
        managerObject.AddComponent<GameManager>();
    }

    private void Awake()
    {
        if (FindObjectsOfType<GameManager>().Length > 1)
        {
            Destroy(gameObject);
            return;
        }

        transform.SetParent(CreateRoot("Managers"), false);
        BuildPhaseOneScene();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            cameraManager.SwitchCamera();
        }

        if (Input.GetKeyDown(KeyCode.M))
        {
            segmentationManager.ToggleMode();
        }

        if (Input.GetKeyDown(KeyCode.R) && sceneBuilder.Vehicle != null)
        {
            sceneBuilder.Vehicle.ResetVehicle();
        }

        if (Input.GetKeyDown(KeyCode.H))
        {
            uiManager.ToggleHelpPanel();
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            uiManager.ToggleVehiclePip();
        }

        if (Input.GetKeyDown(KeyCode.K))
        {
            datasetCaptureManager.CaptureSingleFrame();
        }

        if (Input.GetKeyDown(KeyCode.L))
        {
            datasetCaptureManager.ToggleContinuousCapture();
        }

        if (Input.GetKeyDown(KeyCode.I))
        {
            realtimeSegmentationManager.Toggle();
        }

        if (Input.GetKeyDown(KeyCode.O))
        {
            realtimeSegmentationManager.SwitchProvider();
        }

        if (Input.GetKeyDown(KeyCode.B))
        {
            autoDriveCaptureController.Toggle();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            isPaused = !isPaused;
            Time.timeScale = isPaused ? 0f : 1f;
        }
    }

    private void BuildPhaseOneScene()
    {
        segmentationManager = gameObject.AddComponent<SegmentationMaterialManager>();
        sceneBuilder = gameObject.AddComponent<SceneBuilder>();
        cameraManager = gameObject.AddComponent<CameraManager>();
        uiManager = gameObject.AddComponent<UIManager>();
        datasetCaptureManager = gameObject.AddComponent<DatasetCaptureManager>();
        realtimeSegmentationManager = gameObject.AddComponent<RealtimeSegmentationManager>();
        autoDriveCaptureController = gameObject.AddComponent<AutoDriveCaptureController>();

        sceneBuilder.Build(segmentationManager);
        cameraManager.Configure(sceneBuilder.MainCamera, sceneBuilder.VehicleCamera, sceneBuilder.TopViewCamera);
        datasetCaptureManager.Initialize(cameraManager, segmentationManager, sceneBuilder.Vehicle);
        realtimeSegmentationManager.Initialize(cameraManager, segmentationManager);
        autoDriveCaptureController.Initialize(sceneBuilder.Vehicle, datasetCaptureManager, sceneBuilder.VehicleCamera);
        uiManager.Initialize(sceneBuilder.Vehicle, cameraManager, segmentationManager, datasetCaptureManager, realtimeSegmentationManager, autoDriveCaptureController);
    }

    private Transform CreateRoot(string objectName)
    {
        GameObject existing = GameObject.Find(objectName);
        if (existing != null)
        {
            return existing.transform;
        }

        return new GameObject(objectName).transform;
    }
}
