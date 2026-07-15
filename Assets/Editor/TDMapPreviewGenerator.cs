#if UNITY_EDITOR

using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class TDMapPreviewGenerator
{
    const string DefaultPreviewFolder = "Assets/MapSystem/Previews";
    const int PreviewSize = 1024;
    const float BoundsPadding = 1.15f;

    public static Sprite GenerateAndAssign(GameObject mapPrefab, StageDataSO stageData)
    {
        if (!mapPrefab)
        {
            Debug.LogError("[TD Map Preview] Map Prefab이 없습니다");
            return null;
        }

        if (!stageData)
        {
            Debug.LogError("[TD Map Preview] StageDataSO가 없습니다");
            return null;
        }

        if (!PrefabUtility.IsPartOfPrefabAsset(mapPrefab))
        {
            Debug.LogError("[TD Map Preview] Project의 Prefab Asset을 지정해야 합니다.", mapPrefab);
            return null;
        }

        EnsureFolder(DefaultPreviewFolder);

        string fileName = MakeSafeFileName(string.IsNullOrWhiteSpace(stageData.StageId) ? stageData.name : stageData.StageId);

        string assetPath = $"{DefaultPreviewFolder}/{fileName}_MapPreview.png";
        Scene previewScene = EditorSceneManager.NewPreviewScene();

        GameObject mapInstance = null;
        GameObject cameraObject = null;
        GameObject mainLightObject = null;
        GameObject fillLightObject = null;
        RenderTexture renderTexture = null;
        Texture2D texture = null;

        try
        {
            mapInstance = PrefabUtility.InstantiatePrefab(mapPrefab) as GameObject;

            if (!mapInstance) throw new InvalidOperationException("Map Prefab 인스턴스 생성에 실패");

            SceneManager.MoveGameObjectToScene(mapInstance, previewScene);

            mapInstance.name = mapPrefab.name;
            mapInstance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            mapInstance.transform.localScale = Vector3.one;

            DisableRuntimeRenderers(mapInstance);

            if (!TryCalculateBounds(mapInstance, out Bounds bounds))
                throw new InvalidOperationException("맵의 Renderer Bounds를 계산할 수 없습니다");

            cameraObject = new GameObject("MapPreviewCamera");
            SceneManager.MoveGameObjectToScene(cameraObject, previewScene);

            Camera camera = cameraObject.AddComponent<Camera>();
            ConfigureCamera(camera, bounds);

            mainLightObject = CreateDirectionalLight(previewScene, "MapPreviewMainLight",
                new Vector3(50f, -35f, 0f), 1.15f);
            fillLightObject = CreateDirectionalLight( previewScene, "MapPreviewFillLight",
                new Vector3(65f, 140f, 0f), 0.45f);

            renderTexture = new RenderTexture(PreviewSize, PreviewSize, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB)
            {
                name = "TDMapPreviewRenderTexture",
                antiAliasing = 4,
                useMipMap = false,
                autoGenerateMips = false
            };

            renderTexture.Create();
            camera.targetTexture = renderTexture;

            RenderTexture previous = RenderTexture.active;

            try
            {
                camera.Render();

                RenderTexture.active = renderTexture;

                texture = new Texture2D(PreviewSize, PreviewSize, TextureFormat.RGBA32, false, false);

                texture.ReadPixels(new Rect(0f, 0f, PreviewSize, PreviewSize), 0, 0);

                texture.Apply(false, false);

                string relativePath = assetPath.StartsWith("Assets/") ? assetPath.Substring("Assets/".Length) : assetPath;
                string absolutePath = Path.Combine(Application.dataPath, relativePath);
                string directoryPath = Path.GetDirectoryName(absolutePath);

                if (!string.IsNullOrWhiteSpace(directoryPath)) Directory.CreateDirectory(directoryPath);

                File.WriteAllBytes(absolutePath, texture.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                camera.targetTexture = null;
            }

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            ConfigureTextureImporter(assetPath);

            Sprite previewSprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);

            if (!previewSprite) throw new InvalidOperationException("생성된 맵 프리뷰 Sprite를 불러오지 못했습니다");

            Undo.RecordObject(stageData, "Stage 맵 프리뷰 할당");
            stageData.MapPreview = previewSprite;

            EditorUtility.SetDirty(stageData);
            AssetDatabase.SaveAssetIfDirty(stageData);

            Selection.activeObject = previewSprite;
            EditorGUIUtility.PingObject(previewSprite);

            Debug.Log(
                $"[TD Map Preview] 생성 완료\n" +
                $"Map: {mapPrefab.name}\n" +
                $"StageData: {stageData.name}\n" +
                $"Path: {assetPath}",
                previewSprite);

            return previewSprite;
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);

            EditorUtility.DisplayDialog(
                "맵 프리뷰 생성 실패",
                exception.Message,
                "확인");

            return null;
        }
        finally
        {
            if (texture) Object.DestroyImmediate(texture);

            if (renderTexture)
            {
                renderTexture.Release();
                Object.DestroyImmediate(renderTexture);
            }

            if (fillLightObject) Object.DestroyImmediate(fillLightObject);
            if (mainLightObject) Object.DestroyImmediate(mainLightObject);
            if (cameraObject) Object.DestroyImmediate(cameraObject);
            if (mapInstance) Object.DestroyImmediate(mapInstance);

            if (previewScene.IsValid()) EditorSceneManager.ClosePreviewScene(previewScene);
        }
    }

    static void ConfigureCamera(Camera camera, Bounds bounds)
    {
        float horizontalSize = Mathf.Max(1f, bounds.extents.x);
        float verticalSize = Mathf.Max(1f, bounds.extents.z);
        float cameraHeight = Mathf.Max(0f, bounds.size.magnitude);

        camera.transform.SetPositionAndRotation(
            new Vector3(bounds.center.x, bounds.max.y + cameraHeight, bounds.center.z),
            Quaternion.Euler(90f, 0f, 0f));

        camera.orthographic = true;
        camera.orthographicSize = Mathf.Max(horizontalSize, verticalSize) * BoundsPadding;
        camera.nearClipPlane = 0.01f;
        camera.farClipPlane = cameraHeight + bounds.size.y + 20f;

        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0f, 0f, 0f, 0f);

        camera.allowHDR = false;
        camera.allowMSAA = true;
        camera.useOcclusionCulling = false;
    }

    static GameObject CreateDirectionalLight(Scene scene, string objectName, Vector3 rotation, float intensity)
    {
        GameObject lightObject = new(objectName);
        SceneManager.MoveGameObjectToScene(lightObject, scene);

        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = Color.white;
        light.intensity = intensity;
        light.shadows = LightShadows.Soft;

        lightObject.transform.rotation = Quaternion.Euler(rotation);

        return lightObject;
    }

    static bool TryCalculateBounds(GameObject root, out Bounds bounds)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        bool initialized = false;

        bounds = default;

        foreach (Renderer renderer in renderers)
        {
            if (!renderer || !renderer.enabled) continue;
            if (!renderer.gameObject.activeInHierarchy) continue;
            if (renderer is ParticleSystemRenderer) continue;
            if (renderer is LineRenderer) continue;

            if (!initialized)
            {
                bounds = renderer.bounds;
                initialized = true;
                continue;
            }

            bounds.Encapsulate(renderer.bounds);
        }

        return initialized;
    }
    static void DisableRuntimeRenderers(GameObject mapInstance)
    {
        Transform runtimeRoot = mapInstance.transform.Find("MapRuntime");
        if (!runtimeRoot) return;

        foreach (Renderer renderer in runtimeRoot.GetComponentsInChildren<Renderer>(true))
            renderer.enabled = false;
    }
    static void ConfigureTextureImporter(string assetPath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (!importer) return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100f;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.sRGBTexture = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;

        importer.SaveAndReimport();
    }
    static string MakeSafeFileName(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "Stage";

        foreach (char invalidCharacter in Path.GetInvalidFileNameChars())
            value = value.Replace(invalidCharacter, '_');

        return value.Trim();
    }
    static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath)) return;

        string[] parts = folderPath.Split('/');
        string currentPath = parts[0];

        for (int i = 0; i < parts.Length; i++)
        {
            string nextPath = $"{currentPath}/{parts[i]}";

            if (!AssetDatabase.IsValidFolder(nextPath))
                AssetDatabase.CreateFolder(currentPath, parts[i]);

            currentPath = nextPath;
        }
    }
}

#endif