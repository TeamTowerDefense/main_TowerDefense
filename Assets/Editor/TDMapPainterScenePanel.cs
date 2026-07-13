#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public enum TDMapPainterCategory
{
    Ground,
    Path,
    Endpoint,
    Decoration,
    Erase
}

public struct TDMapPainterAsset
{
    public string Name;
    public GameObject Prefab;
    public TDMapCellType Type;
    public TDDecorationPaletteEntry Decoration;

    public TDMapPainterAsset(string name, GameObject prefab, TDMapCellType type, TDDecorationPaletteEntry decoration = null)
    {
        Name = name;
        Prefab = prefab;
        Type = type;
        Decoration = decoration;
    }
}

[InitializeOnLoad]
public static class TDGridMapPainter
{
    const float TitleHeight = 24f;
    const float ResizeHandleSize = 18f;
    const float PanelMargin = 4f;
    const float MinPanelWidth = 280f;
    const float MinPanelHeight = 300f;

    const int DragControlHint = 0x54444D44;
    const int ResizeControlHint = 0x54444D52;

    const string PanelXKey = "TDGridMapPainter.PanelX";
    const string PanelYKey = "TDGridMapPainter.PanelY";
    const string PanelWidthKey = "TDGridMapPainter.PanelWidth";
    const string PanelHeightKey = "TDGridMapPainter.PanelHeight";

    static Rect panelRect = new(12f, 40f, 330f, 580f);
    static Vector2 panelDragOffset;
    static Vector2 resizeStartMouse;
    static Vector2 resizeStartSize;
    static Vector2 panelScroll;
    static bool panelVisible = true;
    static TDMapRoot mapRoot;
    static TDMapTilePaletteSO palette;
    static TDMapPainterCategory category;
    static TDMapPainterAsset selectedAsset;
    static TDMapPainterAsset hoveredAsset;

    static GameObject previewRoot;
    static GameObject previewPrefab;
    static Vector2Int? lastPaintedCell;
    static Vector3? lastDecorationPosition;

    static float rotationY;
    static float surfaceOffset;
    static float decorationSpacing = 0.5f;
    static int gridExtent = 20;
    static bool paintEnabled;
    static bool drawGrid = true;

    public static TDMapRoot MapRoot
    {
        get => mapRoot;
        set
        {
            if (mapRoot == value) return;
            mapRoot = value;

            if (mapRoot)
            {
                palette = mapRoot.Palette ? mapRoot.Palette : palette;
                EnsureHierarchy();
            }

            ResetDrag();
            DestroyPreview();
            SceneView.RepaintAll();
        }
    }

    public static TDMapTilePaletteSO Palette
    {
        get => palette;
        set
        {
            if (palette == value) return;
            palette = value;

            if (mapRoot)
            {
                Undo.RecordObject(mapRoot, "TD Map 팔레트 변경");
                mapRoot.SetPalette(palette);
                EditorUtility.SetDirty(mapRoot);
            }

            SelectFirstAsset();
        }
    }

    public static TDMapPainterCategory Category
    {
        get => category;
        set { category = value; DestroyPreview(); ResetDrag(); }
    }

    public static TDMapPainterAsset SelectedAsset => selectedAsset;
    public static TDMapPainterAsset HoveredAsset { get => hoveredAsset; set => hoveredAsset = value; }
    public static GameObject SelectedPrefab => selectedAsset.Prefab;
    public static TDMapCellType SelectedType => selectedAsset.Type;
    public static float RotationY => rotationY;
    public static float SurfaceOffset { get => surfaceOffset; set => surfaceOffset = value; }
    public static float DecorationSpacing { get => decorationSpacing; set => decorationSpacing = value; }
    public static bool DrawGrid { get => drawGrid; set => drawGrid = value; }

    public static bool PaintEnabled
    {
        get => paintEnabled;
        set
        {
            paintEnabled = value;
            if (!value) DestroyPreview();
            ResetDrag();
            SceneView.RepaintAll();
        }
    }

    static TDGridMapPainter()
    {
        LoadPanelRect();
        SceneView.duringSceneGui += OnSceneGUI;
        Selection.selectionChanged += OnSelectionChanged;
        Undo.undoRedoPerformed += OnUndoRedo;
        AssemblyReloadEvents.beforeAssemblyReload += DestroyPreview;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        EditorSceneManager.activeSceneChangedInEditMode += OnActiveSceneChanged;
    }

    #region Scene Panel

    [MenuItem("Tools/Grid Map Painter/Scene Panel _F7")]
    static void TogglePanel()
    {
        panelVisible = !panelVisible;
        SceneView.RepaintAll();
    }

    [MenuItem("Tools/Grid Map Painter/패널 위치 및 크기 초기화")]
    static void ResetPanelRect()
    {
        panelRect = new Rect(12f, 40f, 330f, 580f);
        panelVisible = true;
        SavePanelRect();
        SceneView.RepaintAll();
    }

    static void DrawScenePanel(SceneView sceneView)
    {
        Handles.BeginGUI();

        try
        {
            if (!panelVisible) return;

            ClampPanelRect(sceneView);

            Rect titleRect = new(panelRect.x, panelRect.y, panelRect.width, TitleHeight);
            Rect closeRect = new(titleRect.xMax - 24f, titleRect.y + 2f, 20f, 20f);
            Rect contentRect = new(
                panelRect.x + PanelMargin,
                panelRect.y + TitleHeight + PanelMargin,
                panelRect.width - PanelMargin * 2f,
                panelRect.height - TitleHeight - PanelMargin * 2f);
            Rect resizeRect = new(
                panelRect.xMax - ResizeHandleSize,
                panelRect.yMax - ResizeHandleSize,
                ResizeHandleSize,
                ResizeHandleSize);

            int dragControlId = GUIUtility.GetControlID(DragControlHint, FocusType.Passive);
            int resizeControlId = GUIUtility.GetControlID(ResizeControlHint, FocusType.Passive);

            Color panelBackground = EditorGUIUtility.isProSkin
                ? new Color(0.18f, 0.18f, 0.18f, 1f)
                : new Color(0.76f, 0.76f, 0.76f, 1f);

            EditorGUI.DrawRect(panelRect, panelBackground);
            GUI.Box(panelRect, GUIContent.none, EditorStyles.helpBox);
            GUI.Box(titleRect, GUIContent.none, EditorStyles.toolbar);
            GUI.Label(
                new Rect(titleRect.x + 8f, titleRect.y + 3f, titleRect.width - 38f, 18f),
                "TD Map Painter",
                EditorStyles.boldLabel);

            bool closeClicked = GUI.Button(closeRect, "×", EditorStyles.toolbarButton);

            GUILayout.BeginArea(contentRect);

            try
            {
                panelScroll = EditorGUILayout.BeginScrollView(panelScroll);

                try
                {
                    DrawPanelGUI();
                }
                finally
                {
                    EditorGUILayout.EndScrollView();
                }
            }
            finally
            {
                GUILayout.EndArea();
            }

            GUI.Label(resizeRect, "◢", EditorStyles.miniLabel);
            EditorGUIUtility.AddCursorRect(titleRect, MouseCursor.Pan);
            EditorGUIUtility.AddCursorRect(resizeRect, MouseCursor.ResizeUpLeft);

            if (closeClicked)
            {
                panelVisible = false;
                ReleasePanelControl(dragControlId, resizeControlId);
                SceneView.RepaintAll();
                return;
            }

            HandlePanelInteraction(
                sceneView,
                titleRect,
                closeRect,
                resizeRect,
                dragControlId,
                resizeControlId);
        }
        finally
        {
            Handles.EndGUI();
        }
    }

    static void HandlePanelInteraction(
        SceneView sceneView,
        Rect titleRect,
        Rect closeRect,
        Rect resizeRect,
        int dragControlId,
        int resizeControlId)
    {
        Event currentEvent = Event.current;

        if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0)
        {
            if (resizeRect.Contains(currentEvent.mousePosition))
            {
                GUIUtility.hotControl = resizeControlId;
                resizeStartMouse = currentEvent.mousePosition;
                resizeStartSize = panelRect.size;
                currentEvent.Use();
                return;
            }

            if (titleRect.Contains(currentEvent.mousePosition) && !closeRect.Contains(currentEvent.mousePosition))
            {
                GUIUtility.hotControl = dragControlId;
                panelDragOffset = currentEvent.mousePosition - panelRect.position;
                currentEvent.Use();
                return;
            }
        }

        if (currentEvent.type == EventType.MouseDrag)
        {
            if (GUIUtility.hotControl == dragControlId)
            {
                panelRect.position = currentEvent.mousePosition - panelDragOffset;
                ClampPanelRect(sceneView);
                sceneView.Repaint();
                currentEvent.Use();
                return;
            }

            if (GUIUtility.hotControl == resizeControlId)
            {
                Vector2 delta = currentEvent.mousePosition - resizeStartMouse;
                panelRect.size = new Vector2(
                    Mathf.Max(MinPanelWidth, resizeStartSize.x + delta.x),
                    Mathf.Max(MinPanelHeight, resizeStartSize.y + delta.y));

                ClampPanelRect(sceneView);
                sceneView.Repaint();
                currentEvent.Use();
                return;
            }
        }

        if (currentEvent.type is EventType.MouseUp or EventType.Ignore)
        {
            if (GUIUtility.hotControl != dragControlId && GUIUtility.hotControl != resizeControlId) return;

            GUIUtility.hotControl = 0;
            SavePanelRect();
            sceneView.Repaint();
            currentEvent.Use();
        }
    }

    static void ReleasePanelControl(int dragControlId, int resizeControlId)
    {
        if (GUIUtility.hotControl == dragControlId || GUIUtility.hotControl == resizeControlId)
            GUIUtility.hotControl = 0;
    }

    static void ClampPanelRect(SceneView sceneView)
    {
        float availableWidth = Mathf.Max(MinPanelWidth, sceneView.position.width - PanelMargin * 2f);
        float availableHeight = Mathf.Max(MinPanelHeight, sceneView.position.height - PanelMargin * 2f);

        panelRect.width = Mathf.Clamp(panelRect.width, MinPanelWidth, availableWidth);
        panelRect.height = Mathf.Clamp(panelRect.height, MinPanelHeight, availableHeight);
        panelRect.x = Mathf.Clamp(panelRect.x, PanelMargin, Mathf.Max(PanelMargin, sceneView.position.width - panelRect.width - PanelMargin));
        panelRect.y = Mathf.Clamp(panelRect.y, PanelMargin, Mathf.Max(PanelMargin, sceneView.position.height - panelRect.height - PanelMargin));
    }

    static void LoadPanelRect()
    {
        panelRect.x = EditorPrefs.GetFloat(PanelXKey, panelRect.x);
        panelRect.y = EditorPrefs.GetFloat(PanelYKey, panelRect.y);
        panelRect.width = Mathf.Max(MinPanelWidth, EditorPrefs.GetFloat(PanelWidthKey, panelRect.width));
        panelRect.height = Mathf.Max(MinPanelHeight, EditorPrefs.GetFloat(PanelHeightKey, panelRect.height));
    }

    static void SavePanelRect()
    {
        EditorPrefs.SetFloat(PanelXKey, panelRect.x);
        EditorPrefs.SetFloat(PanelYKey, panelRect.y);
        EditorPrefs.SetFloat(PanelWidthKey, panelRect.width);
        EditorPrefs.SetFloat(PanelHeightKey, panelRect.height);
    }

    static void DrawPanelGUI()
    {
        MapRoot = (TDMapRoot)EditorGUILayout.ObjectField("Map Root", MapRoot, typeof(TDMapRoot), true);
        Palette = (TDMapTilePaletteSO)EditorGUILayout.ObjectField("Palette", Palette, typeof(TDMapTilePaletteSO), false);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("새 MapRoot")) CreateMapRoot();
            if (GUILayout.Button("계층 복구")) EnsureHierarchy();
        }

        EditorGUILayout.Space(4f);
        DrawCategory();
        DrawAssets();
        DrawAssetPreview();
        DrawPanelControls();
    }

    static void DrawCategory()
    {
        string[] labels = { "F1 Ground", "F2 Path", "F3 Spawn/Base", "F4 Decoration", "F5 Erase" };
        int value = GUILayout.Toolbar((int)Category, labels);
        if (value != (int)Category) ChangeCategory((TDMapPainterCategory)value);
    }

    static void ChangeCategory(TDMapPainterCategory value)
    {
        if (Category == value) return;
        Category = value;
        SelectFirstAsset();
        SceneView.RepaintAll();
    }

    static void DrawAssets()
    {
        if (Category == TDMapPainterCategory.Erase) return;

        if (!Palette)
        {
            EditorGUILayout.HelpBox("Palette를 지정해 주세요.", MessageType.Warning);
            return;
        }

        List<TDMapPainterAsset> assets = GetAssets();

        if (assets.Count == 0)
        {
            EditorGUILayout.HelpBox("현재 분류에 등록된 프리팹이 없습니다.", MessageType.Info);
            return;
        }

        const float size = 58f;
        int columns = Mathf.Max(1, Mathf.FloorToInt((panelRect.width - 28f) / (size + 4f)));

        for (int i = 0; i < assets.Count; i += columns)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                for (int j = 0; j < columns; j++)
                {
                    int index = i + j;

                    if (index >= assets.Count)
                    {
                        GUILayout.Space(size + 4f);
                        continue;
                    }

                    TDMapPainterAsset asset = assets[index];
                    Texture preview = AssetPreview.GetAssetPreview(asset.Prefab) ?? AssetPreview.GetMiniThumbnail(asset.Prefab);
                    bool selected = SelectedPrefab == asset.Prefab && SelectedType == asset.Type;
                    Color previous = GUI.backgroundColor;

                    if (selected) GUI.backgroundColor = new Color(0.55f, 0.9f, 1f);

                    if (GUILayout.Button(new GUIContent(preview, asset.Name), GUILayout.Width(size), GUILayout.Height(size)))
                        SelectAsset(asset);

                    Rect assetRect = GUILayoutUtility.GetLastRect();
                    GUI.backgroundColor = previous;

                    string shortcutLabel = GetAssetShortcutLabel(index);

                    if (!string.IsNullOrEmpty(shortcutLabel))
                    {
                        Rect badgeRect = new(assetRect.x + 3f, assetRect.y + 3f, 18f, 18f);
                        GUI.Box(badgeRect, GUIContent.none, EditorStyles.miniButton);
                        GUI.Label(badgeRect, shortcutLabel, EditorStyles.centeredGreyMiniLabel);
                    }

                    if (assetRect.Contains(Event.current.mousePosition)) HoveredAsset = asset;
                }
            }
        }

        if (AssetPreview.IsLoadingAssetPreviews()) SceneView.RepaintAll();
    }

    static void DrawAssetPreview()
    {
        TDMapPainterAsset asset = HoveredAsset.Prefab ? HoveredAsset : SelectedAsset;
        if (!asset.Prefab) return;

        Texture preview = AssetPreview.GetAssetPreview(asset.Prefab) ?? AssetPreview.GetMiniThumbnail(asset.Prefab);

        using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
        {
            GUILayout.Label(preview, GUILayout.Width(90f), GUILayout.Height(90f));

            using (new EditorGUILayout.VerticalScope())
            {
                EditorGUILayout.LabelField(asset.Name, EditorStyles.boldLabel);
                EditorGUILayout.LabelField(asset.Type.ToString());
                EditorGUILayout.LabelField($"회전 {RotationY:0}°");
            }
        }

        if (Event.current.type == EventType.Repaint) HoveredAsset = default;
    }

    static void DrawPanelControls()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("↶ -90°")) Rotate(-90f);
            if (GUILayout.Button("↷ +90°")) Rotate(90f);
        }

        SurfaceOffset = EditorGUILayout.FloatField("표면 Offset", SurfaceOffset);
        DecorationSpacing = Mathf.Max(0.05f, EditorGUILayout.FloatField("장식 드래그 간격", DecorationSpacing));
        DrawGrid = EditorGUILayout.Toggle("그리드 표시", DrawGrid);

        Color previous = GUI.backgroundColor;
        GUI.backgroundColor = PaintEnabled ? new Color(0.6f, 1f, 0.65f) : Color.white;

        if (GUILayout.Button(PaintEnabled ? "페인팅 종료" : "페인팅 시작", GUILayout.Height(30f)))
            PaintEnabled = !PaintEnabled;

        GUI.backgroundColor = previous;

        EditorGUILayout.Space(4f);

        using (new EditorGUI.DisabledScope(!MapRoot))
        {
            GUI.backgroundColor = new Color(1f, 0.55f, 0.55f);

            if (GUILayout.Button("모두 삭제", GUILayout.Height(26f)))
                EditorApplication.delayCall += ClearAllPlacedObjects;

            GUI.backgroundColor = previous;
        }

        EditorGUILayout.HelpBox(
            "F1~F5 탭 전환 · 1~9/0 타일 선택 · Q/E 회전\n" +
            "좌클릭/드래그 배치 · Shift+클릭 삭제",
            MessageType.None);
    }

    #endregion

    #region 오버레이 데이터

    public static List<TDMapPainterAsset> GetAssets()
    {
        List<TDMapPainterAsset> assets = new();
        if (!palette) return assets;

        switch (category)
        {
            case TDMapPainterCategory.Ground:
                AddAsset(assets, "Center", palette.GroundTiles.Center, TDMapCellType.Ground);
                AddAsset(assets, "Edge", palette.GroundTiles.Edge, TDMapCellType.Ground);
                AddAsset(assets, "Corner", palette.GroundTiles.Corner, TDMapCellType.Ground);
                AddAsset(assets, "Solo", palette.GroundTiles.Solo, TDMapCellType.Ground);
                break;

            case TDMapPainterCategory.Path:
                AddAsset(assets, "Straight", palette.PathTiles.Straight, TDMapCellType.Path);
                AddAsset(assets, "Corner", palette.PathTiles.Corner, TDMapCellType.Path);
                AddAsset(assets, "End", palette.PathTiles.End, TDMapCellType.Path);
                AddAsset(assets, "Solo", palette.PathTiles.Solo, TDMapCellType.Path);

                for (int i = 0; i < palette.SpecialRules.Count; i++)
                {
                    TDMapTileRule rule = palette.SpecialRules[i];
                    if (rule != null && rule.Prefab) AddAsset(assets, rule.RuleName, rule.Prefab, rule.TargetType);
                }
                break;

            case TDMapPainterCategory.Endpoint:
                AddAsset(assets, "Spawn", palette.SpawnPrefab, TDMapCellType.Spawn);
                AddAsset(assets, "Base", palette.BasePrefab, TDMapCellType.Base);
                break;

            case TDMapPainterCategory.Decoration:
                for (int i = 0; i < palette.Decorations.Count; i++)
                {
                    TDDecorationPaletteEntry entry = palette.Decorations[i];
                    if (entry != null && entry.Prefab)
                        assets.Add(new TDMapPainterAsset(entry.DisplayName, entry.Prefab, TDMapCellType.Decoration, entry));
                }
                break;
        }

        return assets;
    }

    static void AddAsset(List<TDMapPainterAsset> assets, string name, GameObject prefab, TDMapCellType type)
    {
        if (!prefab) return;

        for (int i = 0; i < assets.Count; i++)
            if (assets[i].Prefab == prefab && assets[i].Type == type) return;

        assets.Add(new TDMapPainterAsset(name, prefab, type));
    }

    public static void SelectAsset(TDMapPainterAsset asset)
    {
        selectedAsset = asset;
        rotationY = 0f;
        DestroyPreview();
        SceneView.RepaintAll();
    }

    public static void SelectFirstAsset()
    {
        List<TDMapPainterAsset> assets = GetAssets();
        selectedAsset = assets.Count > 0 ? assets[0] : default;
        rotationY = 0f;
        DestroyPreview();
    }

    public static void Rotate(float amount)
    {
        rotationY = Mathf.Repeat(rotationY + amount, 360f);
        SceneView.RepaintAll();
    }

    #endregion

    #region Map Root

    public static void CreateMapRoot()
    {
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("TD MapRoot 생성");

        GameObject rootObject = new("TD_MapRoot");
        Undo.RegisterCreatedObjectUndo(rootObject, "TD MapRoot 생성");
        mapRoot = Undo.AddComponent<TDMapRoot>(rootObject);
        mapRoot.ConfigureGrid(1f, 0f);
        if (palette) mapRoot.SetPalette(palette);

        EnsureHierarchy();
        Selection.activeGameObject = rootObject;
        Undo.CollapseUndoOperations(group);
        SceneView.FrameLastActiveSceneView();
    }

    public static void EnsureHierarchy()
    {
        if (!mapRoot) return;

        Transform ground = GetOrCreateChild(mapRoot.transform, "Ground");
        Transform path = GetOrCreateChild(mapRoot.transform, "Path");
        Transform endpoints = GetOrCreateChild(mapRoot.transform, "SpawnBase");
        Transform decoration = GetOrCreateChild(mapRoot.transform, "Decoration");
        Transform waypoints = GetOrCreateChild(mapRoot.transform, "PathWaypoints");

        mapRoot.SetHierarchy(ground, path, endpoints, decoration, waypoints);
        EditorUtility.SetDirty(mapRoot);
    }

    static Transform GetOrCreateChild(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        if (child) return child;

        GameObject childObject = new(childName);
        Undo.RegisterCreatedObjectUndo(childObject, $"{childName} 생성");
        Undo.SetTransformParent(childObject.transform, parent, $"{childName} 부모 설정");
        childObject.transform.localPosition = Vector3.zero;
        childObject.transform.localRotation = Quaternion.identity;
        childObject.transform.localScale = Vector3.one;
        return childObject.transform;
    }

    #endregion

    #region Scene View

    static void OnSceneGUI(SceneView sceneView)
    {
        DrawScenePanel(sceneView);

        Event currentEvent = Event.current;
        if (TryHandleShortcut(currentEvent)) return;
        if (panelVisible && panelRect.Contains(currentEvent.mousePosition)) return;
        if (!mapRoot) return;
        if (drawGrid) DrawSceneGrid();
        if (!paintEnabled) { DestroyPreview(); return; }

        if (currentEvent.type == EventType.KeyDown)
        {
            if (currentEvent.keyCode == KeyCode.Q) { Rotate(-90f); currentEvent.Use(); }
            if (currentEvent.keyCode == KeyCode.E) { Rotate(90f); currentEvent.Use(); }
            if (currentEvent.keyCode == KeyCode.Escape) { PaintEnabled = false; currentEvent.Use(); return; }
        }

        if (currentEvent.alt) { ResetDrag(); return; }

        bool erase = category == TDMapPainterCategory.Erase || currentEvent.shift;

        if (!erase && !selectedAsset.Prefab)
        {
            DestroyPreview();
            return;
        }

        if (!TryGetPlacement(currentEvent.mousePosition, selectedAsset.Type, out Vector3 position,
                out Vector3 normal, out Vector2Int cell))
        {
            DestroyPreview();
            return;
        }

        if (!erase) UpdatePreview(position, normal, selectedAsset);
        else DestroyPreview();

        if (currentEvent.type == EventType.Layout)
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

        bool paintEvent = currentEvent.button == 0 &&
                          (currentEvent.type == EventType.MouseDown || currentEvent.type == EventType.MouseDrag);

        if (paintEvent)
        {
            if (erase) Erase(currentEvent.mousePosition, cell);
            else if (selectedAsset.Type == TDMapCellType.Decoration)
            {
                if (!lastDecorationPosition.HasValue ||
                    Vector3.Distance(lastDecorationPosition.Value, position) >= decorationSpacing)
                {
                    PlaceDecoration(selectedAsset, position, normal, cell);
                    lastDecorationPosition = position;
                }
            }
            else if (selectedAsset.Type is TDMapCellType.Spawn or TDMapCellType.Base)
            {
                if (currentEvent.type == EventType.MouseDown) PlaceEndpoint(selectedAsset, position, normal, cell);
            }
            else if (lastPaintedCell != cell)
            {
                PlaceTerrain(selectedAsset, cell);
                lastPaintedCell = cell;
            }

            currentEvent.Use();
        }

        if (currentEvent.type == EventType.MouseUp) ResetDrag();
        sceneView.Repaint();
    }

    static bool TryHandleShortcut(Event currentEvent)
    {
        if (!panelVisible || currentEvent.type != EventType.KeyDown) return false;
        if (currentEvent.alt || currentEvent.control || currentEvent.command || EditorGUIUtility.editingTextField) return false;

        int categoryIndex = currentEvent.keyCode switch
        {
            KeyCode.F1 => 0,
            KeyCode.F2 => 1,
            KeyCode.F3 => 2,
            KeyCode.F4 => 3,
            KeyCode.F5 => 4,
            _ => -1
        };

        if (categoryIndex >= 0)
        {
            ChangeCategory((TDMapPainterCategory)categoryIndex);
            currentEvent.Use();
            return true;
        }

        int assetIndex = currentEvent.keyCode switch
        {
            KeyCode.Alpha1 or KeyCode.Keypad1 => 0,
            KeyCode.Alpha2 or KeyCode.Keypad2 => 1,
            KeyCode.Alpha3 or KeyCode.Keypad3 => 2,
            KeyCode.Alpha4 or KeyCode.Keypad4 => 3,
            KeyCode.Alpha5 or KeyCode.Keypad5 => 4,
            KeyCode.Alpha6 or KeyCode.Keypad6 => 5,
            KeyCode.Alpha7 or KeyCode.Keypad7 => 6,
            KeyCode.Alpha8 or KeyCode.Keypad8 => 7,
            KeyCode.Alpha9 or KeyCode.Keypad9 => 8,
            KeyCode.Alpha0 or KeyCode.Keypad0 => 9,
            _ => -1
        };

        if (!TrySelectAsset(assetIndex)) return false;

        currentEvent.Use();
        return true;
    }

    static bool TrySelectAsset(int index)
    {
        if (index < 0 || Category == TDMapPainterCategory.Erase) return false;

        List<TDMapPainterAsset> assets = GetAssets();
        if (index >= assets.Count) return false;

        SelectAsset(assets[index]);
        return true;
    }

    static string GetAssetShortcutLabel(int index) => index switch
    {
        >= 0 and <= 8 => (index + 1).ToString(),
        9 => "0",
        _ => string.Empty
    };

    static bool TryGetPlacement(Vector2 mousePosition, TDMapCellType type, out Vector3 position,
        out Vector3 normal, out Vector2Int cell)
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);

        if (type is TDMapCellType.Spawn or TDMapCellType.Base or TDMapCellType.Decoration)
        {
            RaycastHit[] hits = Physics.RaycastAll(ray, 10000f, ~0, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            for (int i = 0; i < hits.Length; i++)
            {
                TDMapCellMarker marker = hits[i].collider.GetComponentInParent<TDMapCellMarker>();
                if (!marker || marker.IsDecoration || !marker.transform.IsChildOf(mapRoot.transform)) continue;

                cell = mapRoot.WorldToGrid(hits[i].point);
                normal = hits[i].normal;

                if (type == TDMapCellType.Decoration) position = hits[i].point + normal * surfaceOffset;
                else
                {
                    Vector3 center = mapRoot.GridToWorld(cell);
                    position = new Vector3(center.x, hits[i].point.y + surfaceOffset, center.z);
                }

                return true;
            }
        }

        Plane plane = new(mapRoot.transform.up, mapRoot.GridToWorld(Vector2Int.zero));

        if (!plane.Raycast(ray, out float distance))
        {
            position = default;
            normal = Vector3.up;
            cell = default;
            return false;
        }

        Vector3 point = ray.GetPoint(distance);
        cell = mapRoot.WorldToGrid(point);
        position = mapRoot.GridToWorld(cell) + mapRoot.transform.up * surfaceOffset;
        normal = mapRoot.transform.up;
        return true;
    }

    static void DrawSceneGrid()
    {
        float size = mapRoot.CellSize;
        float min = (-gridExtent - 0.5f) * size;
        float max = (gridExtent + 0.5f) * size;

        using (new Handles.DrawingScope(new Color(0.2f, 0.8f, 1f, 0.3f), mapRoot.transform.localToWorldMatrix))
        {
            for (int i = -gridExtent; i <= gridExtent + 1; i++)
            {
                float offset = (i - 0.5f) * size;
                Handles.DrawLine(new Vector3(offset, mapRoot.TileY, min), new Vector3(offset, mapRoot.TileY, max));
                Handles.DrawLine(new Vector3(min, mapRoot.TileY, offset), new Vector3(max, mapRoot.TileY, offset));
            }
        }
    }

    #endregion

    #region 배치

    static void PlaceTerrain(TDMapPainterAsset asset, Vector2Int cell)
    {
        EnsureHierarchy();

        TDMapCellMarker existing = mapRoot.FindTerrainCell(cell);
        if (existing) Undo.DestroyObjectImmediate(existing.gameObject);

        GameObject root = CreateCellRoot(asset.Type, cell, mapRoot.GetParent(asset.Type));
        TDMapCellMarker marker = Undo.AddComponent<TDMapCellMarker>(root);

        if (asset.Type == TDMapCellType.Path)
        {
            Tile tile = Undo.AddComponent<Tile>(root);
            Undo.RecordObject(tile, "Path 좌표 설정");
            tile.gridPos = GetWorldGrid(cell);
        }

        Transform visual = CreateVisual(marker, asset.Prefab, rotationY, palette ? palette.VisualScale : 1f);
        marker.Setup(asset.Type, cell, asset.Prefab, visual);
        ApplyLayer(root, asset.Type == TDMapCellType.Ground ? GetGroundLayer() : GetPathLayer(), true);

        mapRoot.MarkCellCacheDirty();
        mapRoot.RebuildCellCache();
        EditorUtility.SetDirty(marker);
    }

    static void PlaceEndpoint(TDMapPainterAsset asset, Vector3 position, Vector3 normal, Vector2Int cell)
    {
        EnsureHierarchy();

        TDMapCellMarker existing = mapRoot.FindEndpoint(asset.Type);
        if (existing) Undo.DestroyObjectImmediate(existing.gameObject);

        GameObject root = new($"{asset.Type}_{cell.x}_{cell.y}");
        Undo.RegisterCreatedObjectUndo(root, $"{asset.Type} 배치");
        Undo.SetTransformParent(root.transform, mapRoot.SpawnBaseRoot, $"{asset.Type} 부모 설정");

        root.transform.position = position;
        root.transform.rotation = Quaternion.FromToRotation(Vector3.up, normal) * Quaternion.Euler(0f, rotationY, 0f);
        root.transform.localScale = Vector3.one;

        TDMapCellMarker marker = Undo.AddComponent<TDMapCellMarker>(root);
        Transform visual = CreateVisual(marker, asset.Prefab, 0f, 1f);
        marker.Setup(asset.Type, cell, asset.Prefab, visual);
        ApplyLayer(root, GetPathLayer(), true);

        mapRoot.MarkCellCacheDirty();
        mapRoot.RefreshEndpointReferences();
        EditorUtility.SetDirty(marker);
    }

    static void PlaceDecoration(TDMapPainterAsset asset, Vector3 position, Vector3 normal, Vector2Int cell)
    {
        EnsureHierarchy();
        TDDecorationPaletteEntry entry = asset.Decoration;

        GameObject root = new($"{asset.Name}_{mapRoot.DecorationRoot.childCount:000}");
        Undo.RegisterCreatedObjectUndo(root, "Decoration 배치");
        Undo.SetTransformParent(root.transform, mapRoot.DecorationRoot, "Decoration 부모 설정");

        float randomRotation = entry != null ? entry.GetRandomYRotation() : 0f;
        float scale = entry != null ? entry.GetRandomScale() : 1f;
        bool align = entry?.AlignToSurfaceNormal == true;

        root.transform.position = position;
        root.transform.rotation = (align ? Quaternion.FromToRotation(Vector3.up, normal) : Quaternion.identity) *
                                  Quaternion.Euler(0f, rotationY + randomRotation, 0f);
        root.transform.localScale = Vector3.one;

        TDMapCellMarker marker = Undo.AddComponent<TDMapCellMarker>(root);
        Transform visual = CreateVisual(marker, asset.Prefab, 0f, scale);
        bool blocks = entry?.BlocksTower == true;

        marker.Setup(TDMapCellType.Decoration, cell, asset.Prefab, visual, blocks);
        ApplyLayer(root, entry != null && palette ? palette.GetDecorationLayer(entry) : 0,
            entry?.ApplyLayerRecursively != false);

        mapRoot.MarkCellCacheDirty();
        EditorUtility.SetDirty(marker);
    }

    static GameObject CreateCellRoot(TDMapCellType type, Vector2Int cell, Transform parent)
    {
        GameObject root = new($"{type}_{cell.x}_{cell.y}");
        Undo.RegisterCreatedObjectUndo(root, $"{type} 배치");
        Undo.SetTransformParent(root.transform, parent, $"{type} 부모 설정");
        root.transform.localPosition = mapRoot.GridToLocal(cell);
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;
        return root;
    }

    static Transform CreateVisual(TDMapCellMarker marker, GameObject prefab, float rotation, float scale)
    {
        GameObject visualRoot = new("Visual");
        Undo.RegisterCreatedObjectUndo(visualRoot, "Visual 생성");
        Undo.SetTransformParent(visualRoot.transform, marker.transform, "Visual 부모 설정");
        visualRoot.transform.localPosition = Vector3.zero;
        visualRoot.transform.localRotation = Quaternion.Euler(0f, rotation, 0f);
        visualRoot.transform.localScale = Vector3.one * scale;

        GameObject instance = CreatePrefabInstance(prefab, visualRoot.transform);
        Undo.RegisterCreatedObjectUndo(instance, "Visual Prefab 생성");
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = prefab.transform.localRotation;
        instance.transform.localScale = prefab.transform.localScale;
        return visualRoot.transform;
    }

    static GameObject CreatePrefabInstance(GameObject prefab, Transform parent)
    {
        if (PrefabUtility.IsPartOfPrefabAsset(prefab))
            return PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
        return Object.Instantiate(prefab, parent);
    }

    #endregion

    #region 삭제

    static void ClearAllPlacedObjects()
    {
        if (!mapRoot) return;

        bool confirmed = EditorUtility.DisplayDialog(
            "TD Map 모두 삭제",
            "Ground, Path, Spawn/Base, Decoration, PathWaypoints 아래의 배치물을 모두 삭제합니다.\n\n" +
            "MapRoot와 기본 계층은 유지되며 Ctrl+Z로 복구할 수 있습니다.",
            "모두 삭제",
            "취소");

        if (!confirmed) return;

        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("TD Map 배치물 모두 삭제");
        Undo.RecordObject(mapRoot, "TD Map 참조 초기화");

        DestroyChildren(mapRoot.GroundRoot);
        DestroyChildren(mapRoot.PathRoot);
        DestroyChildren(mapRoot.SpawnBaseRoot);
        DestroyChildren(mapRoot.DecorationRoot);
        DestroyChildren(mapRoot.WaypointRoot);

        mapRoot.ClearWaypoints();
        mapRoot.RefreshEndpointReferences();
        mapRoot.MarkCellCacheDirty();
        mapRoot.RebuildCellCache();
        EditorUtility.SetDirty(mapRoot);

        ResetDrag();
        DestroyPreview();
        Undo.CollapseUndoOperations(group);
        SceneView.RepaintAll();
    }

    static void DestroyChildren(Transform parent)
    {
        if (!parent) return;

        for (int i = parent.childCount - 1; i >= 0; i--)
            Undo.DestroyObjectImmediate(parent.GetChild(i).gameObject);
    }

    static void Erase(Vector2 mousePosition, Vector2Int cell)
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 10000f, ~0, QueryTriggerInteraction.Collide);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            TDMapCellMarker marker = hits[i].collider.GetComponentInParent<TDMapCellMarker>();
            if (!marker || !marker.transform.IsChildOf(mapRoot.transform)) continue;

            Undo.DestroyObjectImmediate(marker.gameObject);
            mapRoot.MarkCellCacheDirty();
            mapRoot.RefreshEndpointReferences();
            return;
        }

        TDMapCellMarker terrain = mapRoot.FindTerrainCell(cell);
        if (!terrain) return;

        Undo.DestroyObjectImmediate(terrain.gameObject);
        mapRoot.MarkCellCacheDirty();
    }

    #endregion

    #region 프리뷰

    static void UpdatePreview(Vector3 position, Vector3 normal, TDMapPainterAsset asset)
    {
        if (!previewRoot || previewPrefab != asset.Prefab) RecreatePreview(asset);
        if (!previewRoot) return;

        previewRoot.transform.position = position;

        if (asset.Type is TDMapCellType.Spawn or TDMapCellType.Base or TDMapCellType.Decoration)
            previewRoot.transform.rotation = Quaternion.FromToRotation(Vector3.up, normal) * Quaternion.Euler(0f, rotationY, 0f);
        else previewRoot.transform.rotation = Quaternion.Euler(0f, rotationY, 0f);
    }

    static void RecreatePreview(TDMapPainterAsset asset)
    {
        DestroyPreview();
        if (!asset.Prefab) return;

        previewRoot = Object.Instantiate(asset.Prefab);
        previewRoot.name = $"__TD_Preview_{asset.Prefab.name}";
        previewRoot.hideFlags = HideFlags.HideAndDontSave | HideFlags.NotEditable;
        previewPrefab = asset.Prefab;

        float scale = asset.Type is TDMapCellType.Ground or TDMapCellType.Path
            ? palette ? palette.VisualScale : 1f
            : 1f;
        previewRoot.transform.localScale = asset.Prefab.transform.localScale * scale;

        Transform[] transforms = previewRoot.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++) transforms[i].gameObject.hideFlags = previewRoot.hideFlags;

        Collider[] colliders = previewRoot.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++) colliders[i].enabled = false;

        Behaviour[] behaviours = previewRoot.GetComponentsInChildren<Behaviour>(true);
        for (int i = 0; i < behaviours.Length; i++) behaviours[i].enabled = false;
    }

    static void DestroyPreview()
    {
        if (previewRoot) Object.DestroyImmediate(previewRoot);
        previewRoot = null;
        previewPrefab = null;
    }

    #endregion

    #region 공통

    static int GetGroundLayer() => palette ? palette.FloorObjectLayer : LayerMask.NameToLayer("Ground");
    static int GetPathLayer() => palette ? palette.ObstacleObjectLayer : LayerMask.NameToLayer("EnemyTile");

    static Vector2Int GetWorldGrid(Vector2Int localCell)
    {
        Vector3 position = mapRoot.GridToWorld(localCell);
        float size = mapRoot.CellSize;
        return new Vector2Int(Mathf.RoundToInt(position.x / size), Mathf.RoundToInt(position.z / size));
    }

    static void ApplyLayer(GameObject target, int layer, bool recursive)
    {
        if (!target || layer < 0) return;

        if (!recursive)
        {
            Undo.RecordObject(target, "레이어 설정");
            target.layer = layer;
            return;
        }

        Transform[] children = target.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            Undo.RecordObject(children[i].gameObject, "레이어 설정");
            children[i].gameObject.layer = layer;
        }
    }

    static void OnSelectionChanged()
    {
        if (!Selection.activeGameObject) return;
        TDMapRoot selectedRoot = Selection.activeGameObject.GetComponentInParent<TDMapRoot>();
        if (selectedRoot) MapRoot = selectedRoot;
    }

    static void OnUndoRedo()
    {
        if (mapRoot) mapRoot.MarkCellCacheDirty();
        DestroyPreview();
        SceneView.RepaintAll();
    }

    static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode) PaintEnabled = false;
    }

    static void OnActiveSceneChanged(Scene previousScene, Scene newScene)
    {
        panelVisible = false;
        paintEnabled = false;
        mapRoot = null;
        ResetDrag();
        DestroyPreview();
        SceneView.RepaintAll();
    }

    static void ResetDrag()
    {
        lastPaintedCell = null;
        lastDecorationPosition = null;
    }

    #endregion
}
#endif
