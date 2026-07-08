using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Minimal runtime UI for speed, camera, segmentation mode, and controls.
/// </summary>
public class UIManager : MonoBehaviour
{
    private Text statusText;
    private GameObject helpPanel;
    private Text helpText;
    private GameObject vehiclePipPanel;
    private RawImage vehiclePipImage;
    private GameObject realtimeSegmentationPanel;
    private RawImage realtimeSegmentationImage;
    private Text realtimeSegmentationTitleText;
    private Text realtimeSegmentationStatusText;
    private GameObject segmentationLegendPanel;
    private VehicleController vehicle;
    private CameraManager cameraManager;
    private SegmentationMaterialManager segmentationManager;
    private DatasetCaptureManager datasetCaptureManager;
    private RealtimeSegmentationManager realtimeSegmentationManager;
    private AutoDriveCaptureController autoDriveCaptureController;
    private bool isHelpVisible = true;
    private bool isVehiclePipVisible = true;

    public void Initialize(VehicleController vehicleController, CameraManager cameras, SegmentationMaterialManager segmentation, DatasetCaptureManager captureManager, RealtimeSegmentationManager realtimeSegmentation, AutoDriveCaptureController autoDriveCapture)
    {
        vehicle = vehicleController;
        cameraManager = cameras;
        segmentationManager = segmentation;
        datasetCaptureManager = captureManager;
        realtimeSegmentationManager = realtimeSegmentation;
        autoDriveCaptureController = autoDriveCapture;
        CreateCanvas();
    }

    private void Update()
    {
        if (statusText == null)
        {
            return;
        }

        float speed = vehicle != null ? vehicle.CurrentSpeedKmh : 0f;
        string cameraName = cameraManager != null ? cameraManager.CurrentCameraName : "None";
        string segmentationMode = segmentationManager != null && segmentationManager.IsSegmentationMode ? "ON" : "OFF";
        string captureMode = datasetCaptureManager != null && datasetCaptureManager.IsContinuousCaptureEnabled ? "AUTO" : "MANUAL";
        string realtimeMode = realtimeSegmentationManager != null && realtimeSegmentationManager.IsEnabled ? "ON" : "OFF";
        string autoDriveMode = autoDriveCaptureController != null ? autoDriveCaptureController.StatusText : "OFF";

        statusText.text =
            "Camera: " + cameraName + "\n" +
            "Speed: " + speed.ToString("0.0") + " km/h\n" +
            "Seg: " + segmentationMode + "    RT: " + realtimeMode + "    Capture: " + captureMode + "    AutoDrive: " + autoDriveMode + "\n" +
            "B: Auto drive capture    I: RTSeg    O: Provider    K: Shot    L: Auto    P: PiP    H: Help";

        if (segmentationLegendPanel != null && segmentationManager != null)
        {
            segmentationLegendPanel.SetActive(segmentationManager.IsSegmentationMode);
        }

        if (realtimeSegmentationPanel != null && realtimeSegmentationManager != null)
        {
            realtimeSegmentationPanel.SetActive(realtimeSegmentationManager.IsEnabled);
            if (realtimeSegmentationTitleText != null)
            {
                realtimeSegmentationTitleText.text = "Realtime Segmentation: " + realtimeSegmentationManager.ProviderName + "  I/O";
            }

            if (realtimeSegmentationStatusText != null)
            {
                realtimeSegmentationStatusText.text = realtimeSegmentationManager.StatusText;
            }
        }
    }

    public void ToggleHelpPanel()
    {
        SetHelpPanelVisible(!isHelpVisible);
    }

    public void SetHelpPanelVisible(bool visible)
    {
        isHelpVisible = visible;
        if (helpPanel != null)
        {
            helpPanel.SetActive(isHelpVisible);
        }
    }

    public void ToggleVehiclePip()
    {
        SetVehiclePipVisible(!isVehiclePipVisible);
    }

    public void SetVehiclePipVisible(bool visible)
    {
        isVehiclePipVisible = visible;
        if (vehiclePipPanel != null)
        {
            vehiclePipPanel.SetActive(isVehiclePipVisible);
        }
    }

    private void CreateCanvas()
    {
        GameObject canvasObject = new GameObject("UI_Canvas");
        canvasObject.transform.SetParent(GetOrCreateUIRoot(), false);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject panelObject = new GameObject("StatusPanel");
        panelObject.transform.SetParent(canvasObject.transform, false);

        Image panelImage = panelObject.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.55f);

        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(12f, -12f);
        panelRect.sizeDelta = new Vector2(650f, 92f);

        GameObject textObject = new GameObject("StatusText");
        textObject.transform.SetParent(panelObject.transform, false);

        statusText = textObject.AddComponent<Text>();
        statusText.font = LoadRuntimeFont();
        statusText.fontSize = 16;
        statusText.alignment = TextAnchor.UpperLeft;
        statusText.color = Color.white;

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(12f, 10f);
        textRect.offsetMax = new Vector2(-12f, -10f);

        CreateHelpPanel(canvasObject.transform);
        CreateVehiclePipPanel(canvasObject.transform);
        CreateRealtimeSegmentationPanel(canvasObject.transform);
        CreateSegmentationLegendPanel(canvasObject.transform);
    }

    private void CreateHelpPanel(Transform canvasTransform)
    {
        helpPanel = new GameObject("HelpPanel");
        helpPanel.transform.SetParent(canvasTransform, false);

        Image panelImage = helpPanel.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.68f);

        RectTransform panelRect = helpPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 1f);
        panelRect.anchorMax = new Vector2(1f, 1f);
        panelRect.pivot = new Vector2(1f, 1f);
        panelRect.anchoredPosition = new Vector2(-12f, -12f);
        panelRect.sizeDelta = new Vector2(460f, 430f);

        GameObject titleObject = new GameObject("HelpTitle");
        titleObject.transform.SetParent(helpPanel.transform, false);

        Text titleText = titleObject.AddComponent<Text>();
        titleText.font = LoadRuntimeFont();
        titleText.fontSize = 20;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.UpperLeft;
        titleText.color = Color.white;
        titleText.text = "Phase 1 Usage Guide";

        RectTransform titleRect = titleObject.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -12f);
        titleRect.sizeDelta = new Vector2(-24f, 28f);

        GameObject textObject = new GameObject("HelpText");
        textObject.transform.SetParent(helpPanel.transform, false);

        helpText = textObject.AddComponent<Text>();
        helpText.font = LoadRuntimeFont();
        helpText.fontSize = 15;
        helpText.alignment = TextAnchor.UpperLeft;
        helpText.color = Color.white;
        helpText.text =
            "Driving\n" +
            "W/S: Forward / Reverse    A/D: Steer\n" +
            "Space: Brake              R: Reset vehicle\n\n" +
            "Dataset Route\n" +
            "B: Toggle auto drive + capture\n" +
            "Auto mode follows fixed waypoints with small random jitter.\n\n" +
            "View\n" +
            "C: Switch camera\n" +
            "P: Show / hide VehicleCamera PiP\n" +
            "I: Toggle realtime segmentation preview\n" +
            "O: Switch GroundTruth / external model\n" +
            "M: Normal / segmentation colors\n" +
            "K: Save RGB + segmentation mask\n" +
            "L: Toggle continuous capture\n" +
            "H: Show / hide this guide\n" +
            "Esc: Pause / resume\n\n" +
            "What to Check\n" +
            "- Vehicle can drive through the crossroad.\n" +
            "- Road damage objects are visible on the road.\n" +
            "- Segmentation mode changes objects to class colors.\n" +
            "- VehicleCamera is the future AI input view.\n\n" +
            "Realtime Baseline\n" +
            "I shows the segmentation stream.\n" +
            "O switches GroundTruth and external HTTP model.\n\n" +
            "Capture Output\n" +
            "Captures/rgb, mask, metadata\n\n" +
            "Phase 1 excludes VR, OSM, and full autonomous driving.";

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(14f, 12f);
        textRect.offsetMax = new Vector2(-14f, -52f);
    }

    private void CreateVehiclePipPanel(Transform canvasTransform)
    {
        vehiclePipPanel = new GameObject("VehicleCamera_PiP");
        vehiclePipPanel.transform.SetParent(canvasTransform, false);

        Image panelImage = vehiclePipPanel.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.72f);

        RectTransform panelRect = vehiclePipPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 0f);
        panelRect.anchorMax = new Vector2(1f, 0f);
        panelRect.pivot = new Vector2(1f, 0f);
        panelRect.anchoredPosition = new Vector2(-12f, 12f);
        panelRect.sizeDelta = new Vector2(330f, 214f);

        GameObject titleObject = new GameObject("VehicleCamera_PiP_Title");
        titleObject.transform.SetParent(vehiclePipPanel.transform, false);

        Text titleText = titleObject.AddComponent<Text>();
        titleText.font = LoadRuntimeFont();
        titleText.fontSize = 14;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleLeft;
        titleText.color = Color.white;
        titleText.text = "VehicleCamera  P";

        RectTransform titleRect = titleObject.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -4f);
        titleRect.sizeDelta = new Vector2(-16f, 24f);

        GameObject imageObject = new GameObject("VehicleCamera_PiP_Image");
        imageObject.transform.SetParent(vehiclePipPanel.transform, false);

        vehiclePipImage = imageObject.AddComponent<RawImage>();
        vehiclePipImage.color = Color.white;
        if (cameraManager != null)
        {
            vehiclePipImage.texture = cameraManager.VehicleRenderTexture;
        }

        RectTransform imageRect = imageObject.GetComponent<RectTransform>();
        imageRect.anchorMin = Vector2.zero;
        imageRect.anchorMax = Vector2.one;
        imageRect.offsetMin = new Vector2(8f, 8f);
        imageRect.offsetMax = new Vector2(-8f, -32f);
    }

    private void CreateRealtimeSegmentationPanel(Transform canvasTransform)
    {
        realtimeSegmentationPanel = new GameObject("RealtimeSegmentationPreview");
        realtimeSegmentationPanel.transform.SetParent(canvasTransform, false);

        Image panelImage = realtimeSegmentationPanel.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.72f);

        RectTransform panelRect = realtimeSegmentationPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 0f);
        panelRect.anchorMax = new Vector2(1f, 0f);
        panelRect.pivot = new Vector2(1f, 0f);
        panelRect.anchoredPosition = new Vector2(-12f, 238f);
        panelRect.sizeDelta = new Vector2(330f, 214f);

        GameObject titleObject = new GameObject("RealtimeSegmentationPreview_Title");
        titleObject.transform.SetParent(realtimeSegmentationPanel.transform, false);

        realtimeSegmentationTitleText = titleObject.AddComponent<Text>();
        realtimeSegmentationTitleText.font = LoadRuntimeFont();
        realtimeSegmentationTitleText.fontSize = 14;
        realtimeSegmentationTitleText.fontStyle = FontStyle.Bold;
        realtimeSegmentationTitleText.alignment = TextAnchor.MiddleLeft;
        realtimeSegmentationTitleText.color = Color.white;
        realtimeSegmentationTitleText.text = "Realtime Segmentation Baseline  I";

        RectTransform titleRect = titleObject.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -4f);
        titleRect.sizeDelta = new Vector2(-16f, 24f);

        GameObject imageObject = new GameObject("RealtimeSegmentationPreview_Image");
        imageObject.transform.SetParent(realtimeSegmentationPanel.transform, false);

        realtimeSegmentationImage = imageObject.AddComponent<RawImage>();
        realtimeSegmentationImage.color = Color.white;
        if (realtimeSegmentationManager != null)
        {
            realtimeSegmentationImage.texture = realtimeSegmentationManager.OutputTexture;
        }

        RectTransform imageRect = imageObject.GetComponent<RectTransform>();
        imageRect.anchorMin = Vector2.zero;
        imageRect.anchorMax = Vector2.one;
        imageRect.offsetMin = new Vector2(8f, 26f);
        imageRect.offsetMax = new Vector2(-8f, -32f);

        GameObject statusObject = new GameObject("RealtimeSegmentationPreview_Status");
        statusObject.transform.SetParent(realtimeSegmentationPanel.transform, false);

        realtimeSegmentationStatusText = statusObject.AddComponent<Text>();
        realtimeSegmentationStatusText.font = LoadRuntimeFont();
        realtimeSegmentationStatusText.fontSize = 12;
        realtimeSegmentationStatusText.alignment = TextAnchor.MiddleLeft;
        realtimeSegmentationStatusText.color = Color.white;
        realtimeSegmentationStatusText.text = "Idle";

        RectTransform statusRect = statusObject.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0f, 0f);
        statusRect.anchorMax = new Vector2(1f, 0f);
        statusRect.pivot = new Vector2(0.5f, 0f);
        statusRect.anchoredPosition = new Vector2(0f, 6f);
        statusRect.sizeDelta = new Vector2(-16f, 18f);

        realtimeSegmentationPanel.SetActive(false);
    }

    private void CreateSegmentationLegendPanel(Transform canvasTransform)
    {
        segmentationLegendPanel = new GameObject("SegmentationLegend");
        segmentationLegendPanel.transform.SetParent(canvasTransform, false);

        Image panelImage = segmentationLegendPanel.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.68f);

        RectTransform panelRect = segmentationLegendPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 0f);
        panelRect.anchorMax = new Vector2(0f, 0f);
        panelRect.pivot = new Vector2(0f, 0f);
        panelRect.anchoredPosition = new Vector2(12f, 12f);
        panelRect.sizeDelta = new Vector2(300f, 292f);

        GameObject titleObject = new GameObject("SegmentationLegend_Title");
        titleObject.transform.SetParent(segmentationLegendPanel.transform, false);

        Text titleText = titleObject.AddComponent<Text>();
        titleText.font = LoadRuntimeFont();
        titleText.fontSize = 16;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.UpperLeft;
        titleText.color = Color.white;
        titleText.text = "Segmentation Classes";

        RectTransform titleRect = titleObject.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -10f);
        titleRect.sizeDelta = new Vector2(-20f, 22f);

        int rowIndex = 0;
        foreach (SegmentationClassRegistry.ClassDefinition classDefinition in SegmentationClassRegistry.GetClasses())
        {
            string label = classDefinition.classId.ToString("D2") + " " + classDefinition.className;
            AddLegendRow(label, classDefinition.color, rowIndex);
            rowIndex++;
        }

        segmentationLegendPanel.SetActive(false);
    }

    private void AddLegendRow(string label, Color color, int rowIndex)
    {
        GameObject swatchObject = new GameObject("LegendSwatch_" + rowIndex);
        swatchObject.transform.SetParent(segmentationLegendPanel.transform, false);

        Image swatchImage = swatchObject.AddComponent<Image>();
        swatchImage.color = color;

        RectTransform swatchRect = swatchObject.GetComponent<RectTransform>();
        swatchRect.anchorMin = new Vector2(0f, 1f);
        swatchRect.anchorMax = new Vector2(0f, 1f);
        swatchRect.pivot = new Vector2(0f, 1f);
        swatchRect.anchoredPosition = new Vector2(14f, -40f - rowIndex * 17f);
        swatchRect.sizeDelta = new Vector2(14f, 14f);

        GameObject labelObject = new GameObject("LegendLabel_" + rowIndex);
        labelObject.transform.SetParent(segmentationLegendPanel.transform, false);

        Text labelText = labelObject.AddComponent<Text>();
        labelText.font = LoadRuntimeFont();
        labelText.fontSize = 12;
        labelText.alignment = TextAnchor.MiddleLeft;
        labelText.color = Color.white;
        labelText.text = label;

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 1f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.pivot = new Vector2(0f, 1f);
        labelRect.anchoredPosition = new Vector2(34f, -39f - rowIndex * 17f);
        labelRect.sizeDelta = new Vector2(-44f, 16f);
    }

    private Transform GetOrCreateUIRoot()
    {
        GameObject uiRoot = GameObject.Find("UI");
        if (uiRoot == null)
        {
            uiRoot = new GameObject("UI");
        }

        return uiRoot.transform;
    }

    private Font LoadRuntimeFont()
    {
        Font font = TryLoadBuiltinFont("LegacyRuntime.ttf");
        if (font != null)
        {
            return font;
        }

        font = TryLoadBuiltinFont("Arial.ttf");
        if (font != null)
        {
            return font;
        }

        string[] osFonts = Font.GetOSInstalledFontNames();
        return osFonts.Length > 0 ? Font.CreateDynamicFontFromOSFont(osFonts[0], 16) : null;
    }

    private Font TryLoadBuiltinFont(string fontName)
    {
        try
        {
            return Resources.GetBuiltinResource<Font>(fontName);
        }
        catch (System.ArgumentException)
        {
            return null;
        }
    }
}
