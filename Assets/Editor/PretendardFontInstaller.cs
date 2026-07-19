using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

public static class PretendardFontInstaller
{
    const string SourceFontPath = "Assets/Fonts/Pretendard-Regular.ttf";
    const string OutputFontPath = "Assets/Fonts/Pretendard-Regular HQ SDF.asset";

    static readonly string[] TargetPrefabPaths =
    {
        "Assets/Prefabs/UI/SettingsPanel.prefab",
        "Assets/Prefabs/UI/InGamePauseMenu.prefab",
        "Assets/Prefabs/UI/LobbyExitButton.prefab"
    };

    const string PrewarmCharacters =
        "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789" +
        "가나다라마바사아자차카타파하" +
        "설정재시작나가기게임종료화면해상도전체음량배경음악효과음적용닫기" +
        "소리은바로저장됩니다이전다음전체화면PAUSE xX/.-+%()[]";

    [DidReloadScripts]
    static void QueueAutomaticInstall()
    {
        EditorApplication.delayCall += InstallIfMissing;
    }

    [MenuItem("Tools/UI/Generate Pretendard HQ SDF")]
    public static void GenerateFromMenu()
    {
        GenerateAndApply(true);
    }

    static void InstallIfMissing()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(OutputFontPath) != null)
            return;

        GenerateAndApply(false);
    }

    static void GenerateAndApply(bool confirmOverwrite)
    {
        AssetDatabase.ImportAsset(SourceFontPath, ImportAssetOptions.ForceSynchronousImport);
        Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);

        if (sourceFont == null)
        {
            Debug.LogError($"[PretendardFontInstaller] 원본 폰트를 찾을 수 없습니다: {SourceFontPath}");
            return;
        }

        TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(OutputFontPath);
        if (existing != null)
        {
            if (confirmOverwrite && !EditorUtility.DisplayDialog(
                    "Pretendard HQ SDF",
                    "기존 HQ SDF를 다시 생성하고 대상 프리팹에 적용할까요?",
                    "생성",
                    "취소"))
                return;

            AssetDatabase.DeleteAsset(OutputFontPath);
        }

        TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
            sourceFont,
            72,
            9,
            GlyphRenderMode.SDFAA,
            2048,
            2048,
            AtlasPopulationMode.Dynamic,
            true);

        if (fontAsset == null)
        {
            Debug.LogError("[PretendardFontInstaller] TMP 폰트 생성에 실패했습니다.");
            return;
        }

        fontAsset.name = "Pretendard-Regular HQ SDF";
        AssetDatabase.CreateAsset(fontAsset, OutputFontPath);

        if (fontAsset.atlasTextures != null && fontAsset.atlasTextures.Length > 0)
        {
            Texture2D atlas = fontAsset.atlasTextures[0];
            atlas.name = "Pretendard-Regular HQ SDF Atlas";
            AssetDatabase.AddObjectToAsset(atlas, fontAsset);
        }

        if (fontAsset.material != null)
        {
            fontAsset.material.name = "Pretendard-Regular HQ SDF Material";
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
        }

        fontAsset.TryAddCharacters(PrewarmCharacters, out string missingCharacters);
        if (!string.IsNullOrEmpty(missingCharacters))
            Debug.LogWarning($"[PretendardFontInstaller] 미리 생성하지 못한 문자: {missingCharacters}");

        EditorUtility.SetDirty(fontAsset);
        AssetDatabase.SaveAssets();

        foreach (string prefabPath in TargetPrefabPaths)
            ApplyToPrefab(prefabPath, fontAsset);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[PretendardFontInstaller] 생성 및 적용 완료: {OutputFontPath}");
    }

    static void ApplyToPrefab(string prefabPath, TMP_FontAsset fontAsset)
    {
        if (!File.Exists(prefabPath))
            return;

        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);

        try
        {
            foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                text.font = fontAsset;
                text.fontSharedMaterial = fontAsset.material;
                EditorUtility.SetDirty(text);
            }

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
