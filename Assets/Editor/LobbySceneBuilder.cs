#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using MimicFacility.UI;

public class LobbySceneBuilder
{
    static readonly Color COL_CONCRETE   = new Color(0.28f, 0.28f, 0.30f);
    static readonly Color COL_CONCRETE_D = new Color(0.18f, 0.18f, 0.20f);
    static readonly Color COL_METAL      = new Color(0.35f, 0.35f, 0.40f);
    static readonly Color COL_FLOOR      = new Color(0.12f, 0.11f, 0.10f);
    static readonly Color COL_CEILING    = new Color(0.15f, 0.15f, 0.17f);
    static readonly Color COL_TABLE      = new Color(0.22f, 0.22f, 0.24f);
    static readonly Color COL_SEAT       = new Color(0.20f, 0.18f, 0.16f);
    static readonly Color COL_SCREEN_BG  = new Color(0.02f, 0.06f, 0.02f);
    static readonly Color COL_SCREEN_TXT = new Color(0.15f, 0.55f, 0.15f);
    static readonly Color COL_GLOW_NUM   = new Color(0.8f, 0.1f, 0.1f);
    static readonly Color COL_READY_ON   = new Color(0.1f, 0.7f, 0.15f);
    static readonly Color COL_READY_OFF  = new Color(0.6f, 0.08f, 0.08f);
    static readonly Color COL_NAMECARD   = new Color(0.10f, 0.10f, 0.12f);
    static readonly Color COL_UI_BG      = new Color(0.03f, 0.03f, 0.04f, 0.95f);
    static readonly Color COL_UI_PANEL   = new Color(0.06f, 0.06f, 0.08f, 0.92f);
    static readonly Color COL_UI_BTN     = new Color(0.10f, 0.10f, 0.13f, 0.95f);
    static readonly Color COL_UI_TEXT    = new Color(0.78f, 0.78f, 0.78f);
    static readonly Color COL_UI_ACCENT  = new Color(0.8f, 0.12f, 0.08f);
    static readonly Color COL_UI_DIM     = new Color(0.4f, 0.4f, 0.42f, 0.7f);

    [MenuItem("MimicFacility/Scenes/Build Lobby Scene")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (!EditorUtility.DisplayDialog("Build Lobby Scene",
            "This creates the multiplayer lobby waiting room.\nContinue?", "Build", "Cancel"))
            return;

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ── Render Settings ─────────────────────────────────────────────
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogDensity = 0.04f;
        RenderSettings.fogColor = new Color(0.01f, 0.01f, 0.015f);
        RenderSettings.ambientLight = new Color(0.04f, 0.04f, 0.05f);

        // ── Camera ──────────────────────────────────────────────────────
        var camObj = new GameObject("LobbyCamera");
        var cam = camObj.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.01f, 0.01f, 0.012f);
        cam.fieldOfView = 60f;
        cam.farClipPlane = 100f;
        cam.nearClipPlane = 0.1f;
        camObj.AddComponent<AudioListener>();
        camObj.transform.position = new Vector3(0f, 4f, -8f);
        camObj.transform.rotation = Quaternion.Euler(25f, 0f, 0f);

        // ── Audio ───────────────────────────────────────────────────────
        var audioObj = new GameObject("AmbientSource");
        var ambientSource = audioObj.AddComponent<AudioSource>();
        ambientSource.playOnAwake = true;
        ambientSource.loop = true;
        ambientSource.volume = 0.3f;

        // ════════════════════════════════════════════════════════════════
        // ROOM GEOMETRY — dark concrete waiting room
        // ════════════════════════════════════════════════════════════════
        var room = new GameObject("WaitingRoom");
        var rm = room.transform;

        float roomW = 14f;
        float roomD = 14f;
        float roomH = 4f;
        float wallThick = 0.4f;

        // Floor
        Prim(rm, "Floor", Vector3.zero, new Vector3(roomW, 0.1f, roomD), COL_FLOOR);
        // Floor stains
        Prim(rm, "FloorStain1", new Vector3(-2f, 0.01f, 1.5f), new Vector3(3f, 0.01f, 2f), new Color(0.09f, 0.08f, 0.07f));
        Prim(rm, "FloorStain2", new Vector3(3f, 0.01f, -2f), new Vector3(2.5f, 0.01f, 1.5f), new Color(0.07f, 0.07f, 0.06f));

        // Ceiling
        Prim(rm, "Ceiling", new Vector3(0f, roomH, 0f), new Vector3(roomW, 0.2f, roomD), COL_CEILING);

        // Walls
        Prim(rm, "WallN", new Vector3(0f, roomH / 2f, roomD / 2f), new Vector3(roomW, roomH, wallThick), COL_CONCRETE);
        Prim(rm, "WallS", new Vector3(0f, roomH / 2f, -roomD / 2f), new Vector3(roomW, roomH, wallThick), COL_CONCRETE);
        Prim(rm, "WallE", new Vector3(roomW / 2f, roomH / 2f, 0f), new Vector3(wallThick, roomH, roomD), COL_CONCRETE_D);
        Prim(rm, "WallW", new Vector3(-roomW / 2f, roomH / 2f, 0f), new Vector3(wallThick, roomH, roomD), COL_CONCRETE_D);

        // Corner trim strips — industrial feel
        float trimH = roomH - 0.2f;
        Prim(rm, "TrimNE", new Vector3(roomW / 2f - 0.05f, trimH / 2f, roomD / 2f - 0.05f), new Vector3(0.1f, trimH, 0.1f), COL_METAL);
        Prim(rm, "TrimNW", new Vector3(-roomW / 2f + 0.05f, trimH / 2f, roomD / 2f - 0.05f), new Vector3(0.1f, trimH, 0.1f), COL_METAL);
        Prim(rm, "TrimSE", new Vector3(roomW / 2f - 0.05f, trimH / 2f, -roomD / 2f + 0.05f), new Vector3(0.1f, trimH, 0.1f), COL_METAL);
        Prim(rm, "TrimSW", new Vector3(-roomW / 2f + 0.05f, trimH / 2f, -roomD / 2f + 0.05f), new Vector3(0.1f, trimH, 0.1f), COL_METAL);

        // Baseboard
        Prim(rm, "BaseN", new Vector3(0f, 0.08f, roomD / 2f - 0.15f), new Vector3(roomW - 0.8f, 0.16f, 0.05f), new Color(0.2f, 0.2f, 0.22f));
        Prim(rm, "BaseS", new Vector3(0f, 0.08f, -roomD / 2f + 0.15f), new Vector3(roomW - 0.8f, 0.16f, 0.05f), new Color(0.2f, 0.2f, 0.22f));
        Prim(rm, "BaseE", new Vector3(roomW / 2f - 0.15f, 0.08f, 0f), new Vector3(0.05f, 0.16f, roomD - 0.8f), new Color(0.2f, 0.2f, 0.22f));
        Prim(rm, "BaseW", new Vector3(-roomW / 2f + 0.15f, 0.08f, 0f), new Vector3(0.05f, 0.16f, roomD - 0.8f), new Color(0.2f, 0.2f, 0.22f));

        // ── Central Table ───────────────────────────────────────────────
        var tableGroup = new GameObject("Table");
        tableGroup.transform.SetParent(rm);

        float tableY = 0.75f;
        // Tabletop — large hexagonal approximated as a wide slab
        Prim(tableGroup.transform, "TableTop", new Vector3(0f, tableY, 0f), new Vector3(5f, 0.08f, 3.5f), COL_TABLE);
        // Table legs
        float legInset = 1.8f;
        for (int lx = -1; lx <= 1; lx += 2)
        for (int lz = -1; lz <= 1; lz += 2)
        {
            Prim(tableGroup.transform, $"TableLeg_{lx}_{lz}",
                new Vector3(lx * legInset, tableY / 2f, lz * 1.2f),
                new Vector3(0.08f, tableY, 0.08f), COL_METAL);
        }

        // ── Seats Around Table (12 positions) ───────────────────────────
        var seatsGroup = new GameObject("Seats");
        seatsGroup.transform.SetParent(rm);

        float seatRadius = 3.8f;
        float seatH = 0.45f;
        float seatW = 0.5f;

        for (int i = 0; i < 12; i++)
        {
            float angle = i * 30f * Mathf.Deg2Rad;
            float sx = Mathf.Sin(angle) * seatRadius;
            float sz = Mathf.Cos(angle) * seatRadius;

            var seatParent = new GameObject($"Seat_{i + 1:D2}");
            seatParent.transform.SetParent(seatsGroup.transform);
            seatParent.transform.position = new Vector3(sx, 0f, sz);

            // Chair seat
            Prim(seatParent.transform, "SeatPad",
                new Vector3(0f, seatH, 0f),
                new Vector3(seatW, 0.06f, seatW), COL_SEAT);

            // Chair legs
            float legOff = 0.18f;
            for (int clx = -1; clx <= 1; clx += 2)
            for (int clz = -1; clz <= 1; clz += 2)
            {
                Prim(seatParent.transform, $"ChairLeg_{clx}_{clz}",
                    new Vector3(clx * legOff, seatH / 2f, clz * legOff),
                    new Vector3(0.03f, seatH, 0.03f), COL_METAL);
            }

            // Backrest
            float backAngle = i * 30f;
            var backrest = Prim(seatParent.transform, "Backrest",
                new Vector3(0f, seatH + 0.25f, -0.2f),
                new Vector3(seatW, 0.5f, 0.04f), COL_SEAT);
            // Rotate the whole seat to face center
            seatParent.transform.rotation = Quaternion.Euler(0f, backAngle, 0f);

            // Glowing subject number — mounted on the backrest
            var numLight = new GameObject($"SubjectNum_{i + 1:D2}");
            numLight.transform.SetParent(seatParent.transform);
            numLight.transform.localPosition = new Vector3(0f, seatH + 0.35f, -0.23f);
            var numPt = numLight.AddComponent<Light>();
            numPt.type = LightType.Point;
            numPt.color = COL_GLOW_NUM;
            numPt.intensity = 0.6f;
            numPt.range = 0.8f;

            // Number plate
            var numPlate = Prim(seatParent.transform, $"NumPlate_{i + 1:D2}",
                new Vector3(0f, seatH + 0.35f, -0.22f),
                new Vector3(0.18f, 0.18f, 0.02f), COL_GLOW_NUM);
            SetEmission(numPlate, COL_GLOW_NUM * 0.4f);

            // Name card slot on table edge (facing outward from center)
            float cardDist = 2.2f;
            float cx = Mathf.Sin(angle) * cardDist;
            float cz = Mathf.Cos(angle) * cardDist;
            var nameCard = Prim(rm, $"NameCard_{i + 1:D2}",
                new Vector3(cx, tableY + 0.05f, cz),
                new Vector3(0.5f, 0.01f, 0.2f), COL_NAMECARD);
            nameCard.transform.rotation = Quaternion.Euler(0f, i * 30f, 0f);

            // Ready status indicator — small light recessed in table near card
            float indDist = 1.9f;
            float ix = Mathf.Sin(angle) * indDist;
            float iz = Mathf.Cos(angle) * indDist;
            var readyInd = Prim(rm, $"ReadyLight_{i + 1:D2}",
                new Vector3(ix, tableY + 0.02f, iz),
                new Vector3(0.08f, 0.02f, 0.08f), COL_READY_OFF);

            var indLight = new GameObject($"ReadyGlow_{i + 1:D2}");
            indLight.transform.SetParent(rm);
            indLight.transform.position = new Vector3(ix, tableY + 0.1f, iz);
            var rl = indLight.AddComponent<Light>();
            rl.type = LightType.Point;
            rl.color = COL_READY_OFF;
            rl.intensity = 0.3f;
            rl.range = 0.5f;
        }

        // ── Overhead Light — dim, institutional ─────────────────────────
        var overheadObj = new GameObject("OverheadLight");
        overheadObj.transform.SetParent(rm);
        overheadObj.transform.position = new Vector3(0f, roomH - 0.3f, 0f);
        var overhead = overheadObj.AddComponent<Light>();
        overhead.type = LightType.Point;
        overhead.color = new Color(0.7f, 0.65f, 0.5f);
        overhead.intensity = 1.8f;
        overhead.range = 12f;

        // Physical fixture
        Prim(rm, "LightFixture", new Vector3(0f, roomH - 0.15f, 0f), new Vector3(0.8f, 0.06f, 0.3f), COL_METAL);
        // Cable from ceiling
        Prim(rm, "LightCable", new Vector3(0f, roomH - 0.08f, 0f), new Vector3(0.02f, 0.15f, 0.02f), new Color(0.1f, 0.1f, 0.12f));

        // Secondary fill lights in corners — very dim
        for (int cx = -1; cx <= 1; cx += 2)
        for (int cz = -1; cz <= 1; cz += 2)
        {
            var cornerLight = new GameObject($"CornerFill_{cx}_{cz}");
            cornerLight.transform.SetParent(rm);
            cornerLight.transform.position = new Vector3(cx * 5f, 3.5f, cz * 5f);
            var cl = cornerLight.AddComponent<Light>();
            cl.type = LightType.Point;
            cl.color = new Color(0.3f, 0.3f, 0.4f);
            cl.intensity = 0.15f;
            cl.range = 4f;
        }

        // ── Monitor — wall-mounted, north wall ─────────────────────────
        var monitorGroup = new GameObject("Monitor");
        monitorGroup.transform.SetParent(rm);

        float monY = 2.8f;
        float monZ = roomD / 2f - wallThick / 2f - 0.05f;

        // Monitor housing
        Prim(monitorGroup.transform, "MonitorFrame",
            new Vector3(0f, monY, monZ),
            new Vector3(2.4f, 1.2f, 0.15f), COL_METAL);

        // Screen surface
        var screen = Prim(monitorGroup.transform, "MonitorScreen",
            new Vector3(0f, monY, monZ - 0.08f),
            new Vector3(2.2f, 1.0f, 0.02f), COL_SCREEN_BG);
        SetEmission(screen, COL_SCREEN_TXT * 0.3f);

        // Monitor mounting bracket
        Prim(monitorGroup.transform, "MonitorBracket",
            new Vector3(0f, monY, monZ + 0.08f),
            new Vector3(0.3f, 0.3f, 0.15f), COL_METAL);

        // Screen glow — casts faint green on the room
        var screenGlow = new GameObject("ScreenGlow");
        screenGlow.transform.SetParent(monitorGroup.transform);
        screenGlow.transform.position = new Vector3(0f, monY, monZ - 0.3f);
        var sg = screenGlow.AddComponent<Light>();
        sg.type = LightType.Spot;
        sg.color = COL_SCREEN_TXT;
        sg.intensity = 1.2f;
        sg.range = 6f;
        sg.spotAngle = 80f;
        screenGlow.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

        // ── Wall Details ────────────────────────────────────────────────
        // Exposed pipe on east wall
        Prim(rm, "PipeE1", new Vector3(roomW / 2f - 0.25f, 2.5f, -2f), new Vector3(0.08f, 0.08f, 6f), new Color(0.30f, 0.18f, 0.10f));
        Prim(rm, "PipeE2", new Vector3(roomW / 2f - 0.25f, 1.5f, 3f), new Vector3(0.06f, 0.06f, 4f), new Color(0.28f, 0.16f, 0.09f));

        // Conduit on west wall
        Prim(rm, "ConduitW", new Vector3(-roomW / 2f + 0.22f, 3.2f, 0f), new Vector3(0.05f, 0.05f, 10f), new Color(0.25f, 0.25f, 0.27f));

        // Vent grate on north wall
        Prim(rm, "VentGrate", new Vector3(4f, 3.2f, roomD / 2f - 0.18f), new Vector3(0.8f, 0.4f, 0.05f), COL_METAL);

        // Door frame — south wall, sealed
        Prim(rm, "DoorFrame", new Vector3(0f, 1.3f, -roomD / 2f + 0.15f), new Vector3(1.2f, 2.6f, 0.1f), new Color(0.38f, 0.28f, 0.18f));
        Prim(rm, "DoorSurface", new Vector3(0f, 1.3f, -roomD / 2f + 0.22f), new Vector3(1.0f, 2.4f, 0.05f), new Color(0.3f, 0.22f, 0.14f));
        // Door status light — red, locked
        var doorLight = new GameObject("DoorLockLight");
        doorLight.transform.SetParent(rm);
        doorLight.transform.position = new Vector3(0.8f, 2.5f, -roomD / 2f + 0.25f);
        var dl = doorLight.AddComponent<Light>();
        dl.type = LightType.Point;
        dl.color = Color.red;
        dl.intensity = 0.8f;
        dl.range = 1f;

        // ════════════════════════════════════════════════════════════════
        // UI CANVAS — lobby interface overlay
        // ════════════════════════════════════════════════════════════════
        var canvasObj = new GameObject("LobbyCanvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObj.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        var canvasRT = canvasObj.GetComponent<RectTransform>();

        // ── Header Bar ──────────────────────────────────────────────────
        var header = CreateUIPanel(canvasRT, "HeaderBar", new Color(0.02f, 0.02f, 0.03f, 0.9f));
        var headerRT = header.GetComponent<RectTransform>();
        headerRT.anchorMin = new Vector2(0f, 0.9f);
        headerRT.anchorMax = Vector2.one;
        headerRT.sizeDelta = Vector2.zero;

        // "WAITING FOR SUBJECTS" title
        CreateTMPText(headerRT, "TitleText", "WAITING FOR SUBJECTS", 36, COL_UI_TEXT,
            TextAlignmentOptions.MidlineLeft, new Vector2(40f, 0f));

        // Player count
        CreateTMPText(headerRT, "PlayerCountText", "0/12 SUBJECTS", 28, COL_UI_DIM,
            TextAlignmentOptions.MidlineRight, new Vector2(-40f, 0f));

        // ── Player List Panel — left side ───────────────────────────────
        var listPanel = CreateUIPanel(canvasRT, "PlayerListPanel", COL_UI_PANEL);
        var listRT = listPanel.GetComponent<RectTransform>();
        listRT.anchorMin = new Vector2(0f, 0.1f);
        listRT.anchorMax = new Vector2(0.35f, 0.88f);
        listRT.sizeDelta = Vector2.zero;

        // Scroll view for player entries
        var scrollObj = new GameObject("PlayerScroll");
        scrollObj.transform.SetParent(listPanel.transform, false);
        var scrollRT = scrollObj.AddComponent<RectTransform>();
        scrollRT.anchorMin = new Vector2(0.02f, 0.02f);
        scrollRT.anchorMax = new Vector2(0.98f, 0.98f);
        scrollRT.sizeDelta = Vector2.zero;
        var scrollRect = scrollObj.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;

        var viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollObj.transform, false);
        var vpRT = viewport.AddComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero;
        vpRT.anchorMax = Vector2.one;
        vpRT.sizeDelta = Vector2.zero;
        viewport.AddComponent<RectMask2D>();
        scrollRect.viewport = vpRT;

        var content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        var contentRT = content.AddComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0f, 1f);
        contentRT.anchorMax = Vector2.one;
        contentRT.pivot = new Vector2(0.5f, 1f);
        contentRT.sizeDelta = new Vector2(0f, 600f);
        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 4f;
        vlg.padding = new RectOffset(8, 8, 8, 8);
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        var csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scrollRect.content = contentRT;

        // Template player entry (will be cloned at runtime)
        var entryTemplate = CreatePlayerEntry(content.transform, "PlayerEntryTemplate", "--", "SUBJECT --", false);
        entryTemplate.SetActive(false);

        // ── Chat Panel — bottom-left ────────────────────────────────────
        var chatPanel = CreateUIPanel(canvasRT, "ChatPanel", COL_UI_PANEL);
        var chatRT = chatPanel.GetComponent<RectTransform>();
        chatRT.anchorMin = Vector2.zero;
        chatRT.anchorMax = new Vector2(0.35f, 0.09f);
        chatRT.sizeDelta = Vector2.zero;

        // Chat input field
        var chatInputObj = new GameObject("ChatInput");
        chatInputObj.transform.SetParent(chatPanel.transform, false);
        var chatInputRT = chatInputObj.AddComponent<RectTransform>();
        chatInputRT.anchorMin = new Vector2(0.02f, 0.1f);
        chatInputRT.anchorMax = new Vector2(0.98f, 0.9f);
        chatInputRT.sizeDelta = Vector2.zero;

        var chatInputImg = chatInputObj.AddComponent<Image>();
        chatInputImg.color = new Color(0.05f, 0.05f, 0.06f, 0.9f);

        var chatInput = chatInputObj.AddComponent<TMP_InputField>();

        var chatTextArea = new GameObject("TextArea");
        chatTextArea.transform.SetParent(chatInputObj.transform, false);
        var textAreaRT = chatTextArea.AddComponent<RectTransform>();
        textAreaRT.anchorMin = Vector2.zero;
        textAreaRT.anchorMax = Vector2.one;
        textAreaRT.offsetMin = new Vector2(10f, 2f);
        textAreaRT.offsetMax = new Vector2(-10f, -2f);
        chatTextArea.AddComponent<RectMask2D>();

        var chatPlaceholder = new GameObject("Placeholder");
        chatPlaceholder.transform.SetParent(chatTextArea.transform, false);
        var phRT = chatPlaceholder.AddComponent<RectTransform>();
        phRT.anchorMin = Vector2.zero;
        phRT.anchorMax = Vector2.one;
        phRT.sizeDelta = Vector2.zero;
        var phText = chatPlaceholder.AddComponent<TextMeshProUGUI>();
        phText.text = "Type message...";
        phText.fontSize = 14;
        phText.color = new Color(0.35f, 0.35f, 0.38f, 0.5f);
        phText.fontStyle = FontStyles.Italic;

        var chatTextObj = new GameObject("Text");
        chatTextObj.transform.SetParent(chatTextArea.transform, false);
        var ctRT = chatTextObj.AddComponent<RectTransform>();
        ctRT.anchorMin = Vector2.zero;
        ctRT.anchorMax = Vector2.one;
        ctRT.sizeDelta = Vector2.zero;
        var chatTMP = chatTextObj.AddComponent<TextMeshProUGUI>();
        chatTMP.fontSize = 14;
        chatTMP.color = COL_UI_TEXT;

        chatInput.textViewport = textAreaRT;
        chatInput.textComponent = chatTMP;
        chatInput.placeholder = phText;
        chatInput.fontAsset = chatTMP.font;

        // ── Right Side Controls ─────────────────────────────────────────
        var controlsPanel = CreateUIPanel(canvasRT, "ControlsPanel", new Color(0f, 0f, 0f, 0f));
        var ctrlRT = controlsPanel.GetComponent<RectTransform>();
        ctrlRT.anchorMin = new Vector2(0.65f, 0.1f);
        ctrlRT.anchorMax = new Vector2(1f, 0.88f);
        ctrlRT.sizeDelta = Vector2.zero;
        controlsPanel.GetComponent<Image>().raycastTarget = false;

        // Display Name input
        var nameGroup = CreateUIPanel(controlsPanel.transform, "NameGroup", COL_UI_PANEL);
        var ngRT = nameGroup.GetComponent<RectTransform>();
        ngRT.anchorMin = new Vector2(0.05f, 0.82f);
        ngRT.anchorMax = new Vector2(0.95f, 0.98f);
        ngRT.sizeDelta = Vector2.zero;

        CreateTMPText(ngRT, "NameLabel", "DISPLAY NAME", 14, COL_UI_DIM,
            TextAlignmentOptions.TopLeft, new Vector2(12f, -4f));

        var nameInputObj = new GameObject("NameInput");
        nameInputObj.transform.SetParent(nameGroup.transform, false);
        var nameInputRT = nameInputObj.AddComponent<RectTransform>();
        nameInputRT.anchorMin = new Vector2(0.03f, 0.05f);
        nameInputRT.anchorMax = new Vector2(0.97f, 0.55f);
        nameInputRT.sizeDelta = Vector2.zero;

        var nameInputImg = nameInputObj.AddComponent<Image>();
        nameInputImg.color = new Color(0.05f, 0.05f, 0.06f, 0.9f);

        var nameInput = nameInputObj.AddComponent<TMP_InputField>();
        nameInput.characterLimit = 24;

        var nameTextArea = new GameObject("TextArea");
        nameTextArea.transform.SetParent(nameInputObj.transform, false);
        var ntaRT = nameTextArea.AddComponent<RectTransform>();
        ntaRT.anchorMin = Vector2.zero;
        ntaRT.anchorMax = Vector2.one;
        ntaRT.offsetMin = new Vector2(10f, 2f);
        ntaRT.offsetMax = new Vector2(-10f, -2f);
        nameTextArea.AddComponent<RectMask2D>();

        var nameTextObj = new GameObject("Text");
        nameTextObj.transform.SetParent(nameTextArea.transform, false);
        var ntRT = nameTextObj.AddComponent<RectTransform>();
        ntRT.anchorMin = Vector2.zero;
        ntRT.anchorMax = Vector2.one;
        ntRT.sizeDelta = Vector2.zero;
        var nameTMP = nameTextObj.AddComponent<TextMeshProUGUI>();
        nameTMP.fontSize = 18;
        nameTMP.color = COL_UI_TEXT;

        nameInput.textViewport = ntaRT;
        nameInput.textComponent = nameTMP;
        nameInput.fontAsset = nameTMP.font;

        // Ready Button
        var readyBtn = CreateButton(controlsPanel.transform, "ReadyButton", "READY",
            new Vector2(0.05f, 0.6f), new Vector2(0.95f, 0.76f), COL_UI_BTN, COL_UI_TEXT, 24);

        // Countdown display
        CreateTMPText(ctrlRT, "CountdownText", "", 48, COL_UI_ACCENT,
            TextAlignmentOptions.Center, new Vector2(0f, -20f));

        // Host-only: Initiate Intake button
        var hostBtn = CreateButton(controlsPanel.transform, "HostStartButton", "INITIATE INTAKE",
            new Vector2(0.05f, 0.42f), new Vector2(0.95f, 0.56f), COL_UI_ACCENT, Color.white, 22);
        hostBtn.SetActive(false);

        // ── Status Bar — bottom ─────────────────────────────────────────
        var statusBar = CreateUIPanel(canvasRT, "StatusBar", new Color(0.02f, 0.02f, 0.03f, 0.85f));
        var sbRT = statusBar.GetComponent<RectTransform>();
        sbRT.anchorMin = new Vector2(0.35f, 0f);
        sbRT.anchorMax = Vector2.right;
        sbRT.sizeDelta = new Vector2(0f, 40f);
        sbRT.pivot = new Vector2(0.5f, 0f);

        CreateTMPText(sbRT, "StatusText", "INTAKE PROCESSING FACILITY  //  AWAITING AUTHORIZATION",
            12, COL_UI_DIM, TextAlignmentOptions.MidlineCenter);

        // ── Attach LobbyUI Component ────────────────────────────────────
        var lobbyUI = canvasObj.AddComponent<LobbyUI>();

        // ── Mark Scene Dirty + Save ─────────────────────────────────────
        EditorSceneManager.MarkAllScenesDirty();

        string scenePath = "Assets/Scenes/Lobby.unity";
        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            AssetDatabase.CreateFolder("Assets", "Scenes");

        EditorSceneManager.SaveScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene(), scenePath);
        AssetDatabase.Refresh();

        Debug.Log($"[LobbySceneBuilder] Lobby scene built and saved to {scenePath}");
    }

    // ════════════════════════════════════════════════════════════════════
    // HELPERS — same Prim/SetEmission pattern as IntroSceneBuilder
    // ════════════════════════════════════════════════════════════════════

    static GameObject Prim(Transform parent, string name, Vector3 pos, Vector3 scale, Color color)
    {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name;
        obj.transform.SetParent(parent);
        obj.transform.position = pos;
        obj.transform.localScale = scale;
        obj.isStatic = true;
        var shader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit");
        obj.GetComponent<Renderer>().material = new Material(shader) { color = color };
        return obj;
    }

    static void SetEmission(GameObject obj, Color emissionColor)
    {
        var r = obj.GetComponent<Renderer>();
        if (r == null) return;
        r.sharedMaterial.EnableKeyword("_EMISSION");
        r.sharedMaterial.SetColor("_EmissionColor", emissionColor);
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

    static TextMeshProUGUI CreateTMPText(Transform parent, string name, string text,
        int fontSize, Color color, TextAlignmentOptions alignment, Vector2? offset = null)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = offset ?? Vector2.zero;

        var tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.raycastTarget = false;

        return tmp;
    }

    static GameObject CreateButton(Transform parent, string name, string label,
        Vector2 anchorMin, Vector2 anchorMax, Color bgColor, Color textColor, int fontSize)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.sizeDelta = Vector2.zero;

        var img = obj.AddComponent<Image>();
        img.color = bgColor;

        var btn = obj.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = bgColor;
        colors.highlightedColor = new Color(bgColor.r + 0.06f, bgColor.g + 0.03f, bgColor.b + 0.03f, bgColor.a);
        colors.pressedColor = new Color(bgColor.r * 0.7f, bgColor.g * 0.7f, bgColor.b * 0.7f, bgColor.a);
        btn.colors = colors;

        CreateTMPText(obj.transform, "Label", label, fontSize, textColor, TextAlignmentOptions.Center);

        return obj;
    }

    static GameObject CreatePlayerEntry(Transform parent, string name, string number, string displayName, bool ready)
    {
        var entry = new GameObject(name);
        entry.transform.SetParent(parent, false);
        var rt = entry.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0f, 40f);

        var layout = entry.AddComponent<LayoutElement>();
        layout.preferredHeight = 40f;
        layout.flexibleWidth = 1f;

        var bg = entry.AddComponent<Image>();
        bg.color = new Color(0.07f, 0.07f, 0.09f, 0.85f);

        var hlg = entry.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8f;
        hlg.padding = new RectOffset(12, 12, 4, 4);
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childAlignment = TextAnchor.MiddleLeft;

        // Ready indicator dot
        var dotObj = new GameObject("ReadyDot");
        dotObj.transform.SetParent(entry.transform, false);
        var dotRT = dotObj.AddComponent<RectTransform>();
        var dotLE = dotObj.AddComponent<LayoutElement>();
        dotLE.preferredWidth = 12f;
        dotLE.preferredHeight = 12f;
        var dotImg = dotObj.AddComponent<Image>();
        dotImg.color = ready ? COL_READY_ON : COL_READY_OFF;

        // Subject number
        var numObj = new GameObject("SubjectNumber");
        numObj.transform.SetParent(entry.transform, false);
        numObj.AddComponent<RectTransform>();
        var numLE = numObj.AddComponent<LayoutElement>();
        numLE.preferredWidth = 36f;
        var numTMP = numObj.AddComponent<TextMeshProUGUI>();
        numTMP.text = number;
        numTMP.fontSize = 16;
        numTMP.color = COL_GLOW_NUM;
        numTMP.alignment = TextAlignmentOptions.MidlineCenter;
        numTMP.fontStyle = FontStyles.Bold;

        // Player name
        var nameObj = new GameObject("PlayerName");
        nameObj.transform.SetParent(entry.transform, false);
        nameObj.AddComponent<RectTransform>();
        var nameLE = nameObj.AddComponent<LayoutElement>();
        nameLE.flexibleWidth = 1f;
        var nameTMP = nameObj.AddComponent<TextMeshProUGUI>();
        nameTMP.text = displayName;
        nameTMP.fontSize = 16;
        nameTMP.color = COL_UI_TEXT;
        nameTMP.alignment = TextAlignmentOptions.MidlineLeft;

        return entry;
    }
}
#endif
