#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using MimicFacility.UI;

public class MainMenuSceneBuilder
{
    static readonly Color COL_BG        = new Color(0.02f, 0.02f, 0.025f);
    static readonly Color COL_FOG       = new Color(0.01f, 0.01f, 0.015f);
    static readonly Color COL_TITLE_RED = new Color(0.85f, 0.08f, 0.08f);
    static readonly Color COL_SUBTITLE  = new Color(0.6f, 0.6f, 0.6f, 0.85f);
    static readonly Color COL_BTN_BG    = new Color(0.08f, 0.08f, 0.10f, 0.92f);
    static readonly Color COL_BTN_HOVER = new Color(0.12f, 0.04f, 0.04f, 0.95f);
    static readonly Color COL_BTN_TEXT  = new Color(0.82f, 0.82f, 0.82f);
    static readonly Color COL_VERSION   = new Color(0.35f, 0.35f, 0.38f, 0.7f);
    static readonly Color COL_PANEL_DIM = new Color(0.04f, 0.04f, 0.05f, 0.96f);

    [MenuItem("MimicFacility/Scenes/Build Main Menu Scene")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (!EditorUtility.DisplayDialog("Build Main Menu Scene",
            "This creates the main menu scene for INTAKE.\nContinue?", "Build", "Cancel"))
            return;

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ── Render Settings ─────────────────────────────────────────────
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogDensity = 0.035f;
        RenderSettings.fogColor = COL_FOG;
        RenderSettings.ambientLight = new Color(0.03f, 0.03f, 0.04f);

        // ── Camera ──────────────────────────────────────────────────────
        var camObj = new GameObject("MainMenuCamera");
        var cam = camObj.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = COL_BG;
        cam.fieldOfView = 60f;
        cam.farClipPlane = 200f;
        cam.nearClipPlane = 0.1f;
        camObj.AddComponent<AudioListener>();
        camObj.transform.position = new Vector3(0f, 1.5f, -10f);
        camObj.transform.rotation = Quaternion.Euler(2f, 0f, 0f);

        // ── Background Environment ──────────────────────────────────────
        var envRoot = new GameObject("Environment");

        // Floor
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Floor";
        floor.transform.SetParent(envRoot.transform);
        floor.transform.localPosition = new Vector3(0f, -0.5f, 0f);
        floor.transform.localScale = new Vector3(60f, 0.1f, 60f);
        floor.isStatic = true;
        ApplyColor(floor, new Color(0.04f, 0.04f, 0.045f));

        // Distant walls for depth
        CreateWall(envRoot.transform, "WallBack", new Vector3(0, 5, 25), new Vector3(50, 12, 0.5f));
        CreateWall(envRoot.transform, "WallLeft", new Vector3(-25, 5, 0), new Vector3(0.5f, 12, 50));
        CreateWall(envRoot.transform, "WallRight", new Vector3(25, 5, 0), new Vector3(0.5f, 12, 50));

        // Ambient red light — low, ominous
        var ambientLight = new GameObject("AmbientRedLight");
        ambientLight.transform.SetParent(envRoot.transform);
        ambientLight.transform.position = new Vector3(0f, 6f, 5f);
        var pointLight = ambientLight.AddComponent<Light>();
        pointLight.type = LightType.Point;
        pointLight.color = new Color(0.6f, 0.08f, 0.05f);
        pointLight.intensity = 1.2f;
        pointLight.range = 30f;

        // Subtle fill from below
        var fillLight = new GameObject("FillLight");
        fillLight.transform.SetParent(envRoot.transform);
        fillLight.transform.position = new Vector3(0f, -1f, -5f);
        var fill = fillLight.AddComponent<Light>();
        fill.type = LightType.Point;
        fill.color = new Color(0.15f, 0.15f, 0.2f);
        fill.intensity = 0.8f;
        fill.range = 25f;

        // Fog particles
        var fogObj = new GameObject("FogParticles");
        fogObj.transform.SetParent(envRoot.transform);
        fogObj.transform.position = new Vector3(0f, 0.5f, 5f);
        var fogPS = fogObj.AddComponent<ParticleSystem>();
        var fogMain = fogPS.main;
        fogMain.startColor = new Color(0.06f, 0.06f, 0.08f, 0.08f);
        fogMain.startSize = 8f;
        fogMain.startLifetime = 15f;
        fogMain.startSpeed = 0.15f;
        fogMain.maxParticles = 40;
        fogMain.simulationSpace = ParticleSystemSimulationSpace.World;
        var fogEmission = fogPS.emission;
        fogEmission.rateOverTime = 3f;
        var fogShape = fogPS.shape;
        fogShape.shapeType = ParticleSystemShapeType.Box;
        fogShape.scale = new Vector3(40f, 2f, 30f);
        var fogRenderer = fogObj.GetComponent<ParticleSystemRenderer>();
        fogRenderer.material = GetParticleMaterial(new Color(0.06f, 0.06f, 0.08f, 0.08f));
        fogRenderer.renderMode = ParticleSystemRenderMode.Billboard;

        // ── Audio Source ────────────────────────────────────────────────
        var audioObj = new GameObject("MusicSource");
        var musicSource = audioObj.AddComponent<AudioSource>();
        musicSource.playOnAwake = true;
        musicSource.loop = true;
        musicSource.volume = 0.15f;
        musicSource.spatialBlend = 0f;
        audioObj.AddComponent<AudioCorruptor>();

        var daisyClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Music/DaisyBell.mp3");
        if (daisyClip == null)
            daisyClip = FindAsset<AudioClip>("DaisyBell");
        if (daisyClip != null)
        {
            musicSource.clip = daisyClip;
            Debug.Log($"[MainMenuSceneBuilder] Wired audio: {daisyClip.name} ({daisyClip.length:F1}s)");
        }
        else
        {
            Debug.LogWarning("[MainMenuSceneBuilder] Could not find DaisyBell audio clip. " +
                "Place it at Assets/Audio/Music/DaisyBell.mp3");
        }

        // ── UI Canvas ───────────────────────────────────────────────────
        var canvasObj = new GameObject("MainMenuCanvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();

        // ── Dark Vignette Overlay ───────────────────────────────────────
        var vignetteObj = CreateUIPanel(canvasObj.transform, "Vignette", new Color(0f, 0f, 0f, 0.3f));
        vignetteObj.GetComponent<Image>().raycastTarget = false;

        // ── Title: INTAKE ───────────────────────────────────────────────
        var titleContainer = new GameObject("TitleContainer");
        titleContainer.transform.SetParent(canvasObj.transform, false);
        var titleContainerRT = titleContainer.AddComponent<RectTransform>();
        titleContainerRT.anchorMin = new Vector2(0.1f, 0.7f);
        titleContainerRT.anchorMax = new Vector2(0.9f, 0.92f);
        titleContainerRT.sizeDelta = Vector2.zero;
        titleContainerRT.anchoredPosition = Vector2.zero;

        // MimicTitleRenderer (procedural INTAKE logo)
        var titleRendererObj = new GameObject("IntakeTitle");
        titleRendererObj.transform.SetParent(titleContainer.transform, false);
        var titleRT = titleRendererObj.AddComponent<RectTransform>();
        titleRT.anchorMin = Vector2.zero;
        titleRT.anchorMax = Vector2.one;
        titleRT.sizeDelta = Vector2.zero;
        titleRT.anchoredPosition = Vector2.zero;
        titleRendererObj.AddComponent<MimicTitleRenderer>();

        // ── Subtitle ────────────────────────────────────────────────────
        var subtitleObj = new GameObject("Subtitle");
        subtitleObj.transform.SetParent(canvasObj.transform, false);
        var subRT = subtitleObj.AddComponent<RectTransform>();
        subRT.anchorMin = new Vector2(0.15f, 0.62f);
        subRT.anchorMax = new Vector2(0.85f, 0.70f);
        subRT.sizeDelta = Vector2.zero;
        subRT.anchoredPosition = Vector2.zero;
        var subTMP = subtitleObj.AddComponent<TextMeshProUGUI>();
        subTMP.text = "Anything you say can and will be used against you.";
        subTMP.fontSize = 22;
        subTMP.color = COL_SUBTITLE;
        subTMP.alignment = TextAlignmentOptions.Center;
        subTMP.fontStyle = FontStyles.Italic;
        subTMP.raycastTarget = false;

        // ── Button Panel ────────────────────────────────────────────────
        var buttonPanel = new GameObject("ButtonPanel");
        buttonPanel.transform.SetParent(canvasObj.transform, false);
        var bpRT = buttonPanel.AddComponent<RectTransform>();
        bpRT.anchorMin = new Vector2(0.32f, 0.12f);
        bpRT.anchorMax = new Vector2(0.68f, 0.58f);
        bpRT.sizeDelta = Vector2.zero;
        bpRT.anchoredPosition = Vector2.zero;
        var bpLayout = buttonPanel.AddComponent<VerticalLayoutGroup>();
        bpLayout.spacing = 12f;
        bpLayout.childAlignment = TextAnchor.UpperCenter;
        bpLayout.childControlWidth = true;
        bpLayout.childControlHeight = true;
        bpLayout.childForceExpandWidth = true;
        bpLayout.childForceExpandHeight = true;
        bpLayout.padding = new RectOffset(0, 0, 0, 0);

        // Create each menu button
        var hostBtn = CreateMenuButton(buttonPanel.transform, "HostButton", "HOST GAME");
        var joinBtn = CreateMenuButton(buttonPanel.transform, "JoinButton", "JOIN GAME");
        var settingsBtn = CreateMenuButton(buttonPanel.transform, "SettingsButton", "SETTINGS");
        var quitBtn = CreateMenuButton(buttonPanel.transform, "QuitButton", "QUIT");

        // ── Join Panel (IP Input) ───────────────────────────────────────
        var joinPanel = CreateUIPanel(canvasObj.transform, "JoinPanel", COL_PANEL_DIM);
        joinPanel.SetActive(false);
        var joinPanelGroup = joinPanel.AddComponent<CanvasGroup>();
        joinPanelGroup.alpha = 1f;

        var joinInnerObj = new GameObject("JoinInner");
        joinInnerObj.transform.SetParent(joinPanel.transform, false);
        var joinInnerRT = joinInnerObj.AddComponent<RectTransform>();
        joinInnerRT.anchorMin = new Vector2(0.25f, 0.3f);
        joinInnerRT.anchorMax = new Vector2(0.75f, 0.7f);
        joinInnerRT.sizeDelta = Vector2.zero;
        var joinInnerLayout = joinInnerObj.AddComponent<VerticalLayoutGroup>();
        joinInnerLayout.spacing = 16f;
        joinInnerLayout.childAlignment = TextAnchor.MiddleCenter;
        joinInnerLayout.childControlWidth = true;
        joinInnerLayout.childControlHeight = true;
        joinInnerLayout.childForceExpandWidth = true;
        joinInnerLayout.childForceExpandHeight = false;

        // "ENTER SERVER ADDRESS" label
        var joinLabelObj = new GameObject("JoinLabel");
        joinLabelObj.transform.SetParent(joinInnerObj.transform, false);
        var joinLabelLE = joinLabelObj.AddComponent<LayoutElement>();
        joinLabelLE.preferredHeight = 40f;
        var joinLabel = joinLabelObj.AddComponent<TextMeshProUGUI>();
        joinLabel.text = "ENTER SERVER ADDRESS";
        joinLabel.fontSize = 24;
        joinLabel.color = COL_BTN_TEXT;
        joinLabel.alignment = TextAlignmentOptions.Center;

        // IP input field
        var ipInputObj = new GameObject("IPInput");
        ipInputObj.transform.SetParent(joinInnerObj.transform, false);
        var ipInputLE = ipInputObj.AddComponent<LayoutElement>();
        ipInputLE.preferredHeight = 50f;
        var ipInputImg = ipInputObj.AddComponent<Image>();
        ipInputImg.color = new Color(0.06f, 0.06f, 0.08f);

        // Input text area
        var ipTextArea = new GameObject("Text Area");
        ipTextArea.transform.SetParent(ipInputObj.transform, false);
        var ipTextAreaRT = ipTextArea.AddComponent<RectTransform>();
        ipTextAreaRT.anchorMin = Vector2.zero;
        ipTextAreaRT.anchorMax = Vector2.one;
        ipTextAreaRT.sizeDelta = new Vector2(-16f, -8f);

        var ipPlaceholder = new GameObject("Placeholder");
        ipPlaceholder.transform.SetParent(ipTextArea.transform, false);
        var ipPlaceholderRT = ipPlaceholder.AddComponent<RectTransform>();
        ipPlaceholderRT.anchorMin = Vector2.zero;
        ipPlaceholderRT.anchorMax = Vector2.one;
        ipPlaceholderRT.sizeDelta = Vector2.zero;
        var phTMP = ipPlaceholder.AddComponent<TextMeshProUGUI>();
        phTMP.text = "localhost";
        phTMP.fontSize = 20;
        phTMP.color = new Color(0.3f, 0.3f, 0.35f);
        phTMP.alignment = TextAlignmentOptions.MidlineLeft;

        var ipText = new GameObject("Text");
        ipText.transform.SetParent(ipTextArea.transform, false);
        var ipTextRT = ipText.AddComponent<RectTransform>();
        ipTextRT.anchorMin = Vector2.zero;
        ipTextRT.anchorMax = Vector2.one;
        ipTextRT.sizeDelta = Vector2.zero;
        var ipTMP = ipText.AddComponent<TextMeshProUGUI>();
        ipTMP.fontSize = 20;
        ipTMP.color = COL_BTN_TEXT;
        ipTMP.alignment = TextAlignmentOptions.MidlineLeft;

        var ipInput = ipInputObj.AddComponent<TMP_InputField>();
        ipInput.textViewport = ipTextAreaRT;
        ipInput.textComponent = ipTMP;
        ipInput.placeholder = phTMP;
        ipInput.fontAsset = ipTMP.font;

        // Join confirm + back buttons
        var joinBtnRow = new GameObject("JoinButtonRow");
        joinBtnRow.transform.SetParent(joinInnerObj.transform, false);
        var joinBtnRowLE = joinBtnRow.AddComponent<LayoutElement>();
        joinBtnRowLE.preferredHeight = 50f;
        var joinBtnRowLayout = joinBtnRow.AddComponent<HorizontalLayoutGroup>();
        joinBtnRowLayout.spacing = 16f;
        joinBtnRowLayout.childControlWidth = true;
        joinBtnRowLayout.childControlHeight = true;
        joinBtnRowLayout.childForceExpandWidth = true;
        joinBtnRowLayout.childForceExpandHeight = true;

        var joinConfirmBtn = CreateMenuButton(joinBtnRow.transform, "JoinConfirmButton", "CONNECT");
        var joinBackBtn = CreateMenuButton(joinBtnRow.transform, "JoinBackButton", "BACK");

        // ── Settings Panel ──────────────────────────────────────────────
        var settingsPanel = CreateUIPanel(canvasObj.transform, "SettingsPanel", COL_PANEL_DIM);
        settingsPanel.SetActive(false);
        var settingsPanelGroup = settingsPanel.AddComponent<CanvasGroup>();
        settingsPanelGroup.alpha = 1f;

        var settingsLabel = new GameObject("SettingsLabel");
        settingsLabel.transform.SetParent(settingsPanel.transform, false);
        var slRT = settingsLabel.AddComponent<RectTransform>();
        slRT.anchorMin = new Vector2(0.3f, 0.75f);
        slRT.anchorMax = new Vector2(0.7f, 0.85f);
        slRT.sizeDelta = Vector2.zero;
        var slTMP = settingsLabel.AddComponent<TextMeshProUGUI>();
        slTMP.text = "SETTINGS";
        slTMP.fontSize = 36;
        slTMP.color = COL_BTN_TEXT;
        slTMP.alignment = TextAlignmentOptions.Center;

        var settingsBackBtn = CreateMenuButton(settingsPanel.transform, "SettingsBackButton", "BACK");
        var sbRT = settingsBackBtn.GetComponent<RectTransform>();
        sbRT.anchorMin = new Vector2(0.35f, 0.1f);
        sbRT.anchorMax = new Vector2(0.65f, 0.18f);
        sbRT.sizeDelta = Vector2.zero;
        // Remove layout element since this button is manually positioned
        var sbLE = settingsBackBtn.GetComponent<LayoutElement>();
        if (sbLE != null) Object.DestroyImmediate(sbLE);

        // ── Version Display ─────────────────────────────────────────────
        var versionObj = new GameObject("VersionText");
        versionObj.transform.SetParent(canvasObj.transform, false);
        var vRT = versionObj.AddComponent<RectTransform>();
        vRT.anchorMin = new Vector2(0f, 0f);
        vRT.anchorMax = new Vector2(0.3f, 0.06f);
        vRT.sizeDelta = Vector2.zero;
        vRT.anchoredPosition = Vector2.zero;
        var vTMP = versionObj.AddComponent<TextMeshProUGUI>();
        vTMP.fontSize = 14;
        vTMP.color = COL_VERSION;
        vTMP.alignment = TextAlignmentOptions.BottomLeft;
        vTMP.margin = new Vector4(12f, 0f, 0f, 8f);
        vTMP.raycastTarget = false;

        // Read VERSION file at build time to set default text
        string versionPath = System.IO.Path.Combine(Application.dataPath, "..", "VERSION");
        if (System.IO.File.Exists(versionPath))
        {
            string ver = System.IO.File.ReadAllText(versionPath).Trim();
            vTMP.text = $"v{ver}";
        }
        else
        {
            vTMP.text = "vDEV";
        }

        // ── Event System ────────────────────────────────────────────────
        if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var esObj = new GameObject("EventSystem");
            esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // ── Main Menu Controller ────────────────────────────────────────
        var controllerObj = new GameObject("MainMenuController");
        var controller = controllerObj.AddComponent<MainMenuController>();

        // Wire serialized fields via SerializedObject
        var so = new SerializedObject(controller);
        so.Update();

        SetField(so, "mainPanel", buttonPanel);
        SetField(so, "joinPanel", joinPanel);
        SetField(so, "settingsPanel", settingsPanel);

        SetField(so, "hostButton", hostBtn.GetComponent<Button>());
        SetField(so, "joinButton", joinBtn.GetComponent<Button>());
        SetField(so, "settingsButton", settingsBtn.GetComponent<Button>());
        SetField(so, "quitButton", quitBtn.GetComponent<Button>());

        SetField(so, "joinConfirmButton", joinConfirmBtn.GetComponent<Button>());
        SetField(so, "joinBackButton", joinBackBtn.GetComponent<Button>());
        SetField(so, "settingsBackButton", settingsBackBtn.GetComponent<Button>());

        SetField(so, "serverAddressInput", ipInput);
        SetField(so, "versionText", vTMP);
        SetField(so, "musicSource", musicSource);

        so.ApplyModifiedPropertiesWithoutUndo();

        // ── Mark scene dirty ────────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log("[MainMenuSceneBuilder] Main menu scene built successfully.");
        Debug.Log("  Title: INTAKE (procedural MimicTitleRenderer)");
        Debug.Log("  Buttons: HOST GAME, JOIN GAME, SETTINGS, QUIT");
        Debug.Log("  Audio: Daisy Bell with AudioCorruptor degradation");
        Debug.Log("  Save the scene to Assets/Scenes/MainMenu.unity");
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    static GameObject CreateMenuButton(Transform parent, string name, string label)
    {
        var btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        var rt = btnObj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0f, 52f);

        var le = btnObj.AddComponent<LayoutElement>();
        le.preferredHeight = 52f;
        le.flexibleWidth = 1f;

        var img = btnObj.AddComponent<Image>();
        img.color = COL_BTN_BG;

        var btn = btnObj.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = COL_BTN_BG;
        colors.highlightedColor = COL_BTN_HOVER;
        colors.pressedColor = new Color(0.18f, 0.05f, 0.05f, 0.98f);
        colors.selectedColor = COL_BTN_HOVER;
        colors.fadeDuration = 0.12f;
        btn.colors = colors;
        btn.targetGraphic = img;

        // Button label
        var textObj = new GameObject("Label");
        textObj.transform.SetParent(btnObj.transform, false);
        var textRT = textObj.AddComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.sizeDelta = Vector2.zero;
        textRT.anchoredPosition = Vector2.zero;

        var tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 22;
        tmp.color = COL_BTN_TEXT;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        tmp.raycastTarget = false;
        tmp.characterSpacing = 6f;

        return btnObj;
    }

    static GameObject CreateUIPanel(Transform parent, string name, Color color)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;

        var img = obj.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;

        return obj;
    }

    static void CreateWall(Transform parent, string name, Vector3 pos, Vector3 scale)
    {
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.SetParent(parent);
        wall.transform.localPosition = pos;
        wall.transform.localScale = scale;
        wall.isStatic = true;
        ApplyColor(wall, new Color(0.03f, 0.03f, 0.035f));
    }

    static void ApplyColor(GameObject obj, Color color)
    {
        var shader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit");
        var r = obj.GetComponent<Renderer>();
        if (r != null)
            r.material = new Material(shader) { color = color };
    }

    static Material GetParticleMaterial(Color color)
    {
        string matDir = "Assets/Materials/Particles";
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder(matDir))
            AssetDatabase.CreateFolder("Assets/Materials", "Particles");

        string safeName = $"Particle_{ColorToHex(color)}";
        string matPath = $"{matDir}/{safeName}.mat";

        var existing = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (existing != null) return existing;

        string[] shaderNames = new string[]
        {
            "Universal Render Pipeline/Particles/Unlit",
            "Universal Render Pipeline/Particles/Simple Lit",
            "Particles/Standard Unlit",
            "Particles/Standard Surface",
            "Legacy Shaders/Particles/Alpha Blended",
            "Sprites/Default",
        };

        Shader shader = null;
        foreach (var name in shaderNames)
        {
            shader = Shader.Find(name);
            if (shader != null) break;
        }

        if (shader == null)
            shader = Shader.Find("Sprites/Default");
        if (shader == null)
            return new Material(Shader.Find("Hidden/InternalErrorShader"));

        var mat = new Material(shader);
        mat.color = color;

        if (mat.HasProperty("_Surface"))
            mat.SetFloat("_Surface", 1f);
        if (mat.HasProperty("_SrcBlend"))
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (mat.HasProperty("_DstBlend"))
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (mat.HasProperty("_ZWrite"))
            mat.SetInt("_ZWrite", 0);

        mat.renderQueue = 3000;
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

        AssetDatabase.CreateAsset(mat, matPath);
        AssetDatabase.SaveAssets();

        return mat;
    }

    static string ColorToHex(Color c)
    {
        return $"{(int)(c.r * 255):X2}{(int)(c.g * 255):X2}{(int)(c.b * 255):X2}";
    }

    static void SetField(SerializedObject so, string fieldName, Object value)
    {
        var prop = so.FindProperty(fieldName);
        if (prop != null)
            prop.objectReferenceValue = value;
        else
            Debug.LogWarning($"[MainMenuSceneBuilder] Field '{fieldName}' not found on MainMenuController.");
    }

    static T FindAsset<T>(string name) where T : Object
    {
        string[] guids = AssetDatabase.FindAssets(name + " t:" + typeof(T).Name);
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
        }
        return null;
    }
}
#endif
