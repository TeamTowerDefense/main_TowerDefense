using IGameFlowInterface;
using System;
using System.IO;
using UnityEngine;

public class JsonSaveService : GlobalServiceBase, ISaveService
{
    const string SaveFolderName = "Saves";
    const string JsonExtension = ".json";

    string SaveFolderPath => Path.Combine(Application.persistentDataPath, SaveFolderName);

    public bool Exists(string saveKey)
    {
        if (!IsValidKey(saveKey)) return false;
        return File.Exists(GetJsonPath(saveKey));
    }
    public void Save<T>(string saveKey, T data)
    {
        if (!IsValidKey(saveKey))
        {
            Debug.LogError("[JsonSaveService] 저장 키가 비어 있습니다.", this);
            return;
        }
        if (data == null)
        {
            Debug.LogError($"[JsonSaveService] 저장 데이터가 null입니다. key: {saveKey}", this);
            return;
        }

        try
        {
            Directory.CreateDirectory(SaveFolderPath);

            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(GetJsonPath(saveKey), json);

            Debug.Log($"[JsonSaveService] Save 완료: {saveKey}", this);
        }
        catch (Exception e)
        {
            Debug.LogError($"[JsonSaveService] Save 실패: {saveKey}\n{e}", this);

        }

    }
    public bool TryLoad<T>(string saveKey, out T data)
    {
        data = default;

        if (!IsValidKey(saveKey))
        {
            Debug.LogError("[JsonSaveService] 로드 키가 비었습니다", this);
            return false;
        }

        string path = GetJsonPath(saveKey);

        if (!File.Exists(path)) return false;

        try
        {
            string json = File.ReadAllText(path);
            data = JsonUtility.FromJson<T>(json);

            return data != null;
        }
        catch (Exception e)
        {
            Debug.LogError($"[JsonSaveService] Load 실패: {saveKey}\n{e}", this);
            return false;
        }
    }
    public T LoadOrCreate<T>(string saveKey) where T : new()
    {
        if (TryLoad(saveKey, out T data)) return data;

        data = new T();
        Save(saveKey, data);

        return data;
    }
    public void Delete(string saveKey)
    {
        if (!IsValidKey(saveKey)) return;

        string path = GetJsonPath(saveKey);

        if (!File.Exists(path)) return;

        try
        {
            File.Delete(path);
            Debug.Log($"[JsonSaveService] Delete 완료: {saveKey}", this);
        }
        catch (Exception e)
        {
            Debug.LogError($"[JsonSaveService] Delete 실패: {saveKey}\n{e}", this);
        }
    }

    #region 내부 유틸
    string GetJsonPath(string saveKey)
    {
        string safeKey = SanitizeKey(saveKey);
        return Path.Combine(SaveFolderPath, $"{saveKey}{JsonExtension}");
    }
    static bool IsValidKey(string saveKey) => !string.IsNullOrWhiteSpace(saveKey);
    static string SanitizeKey(string saveKey)
    {
        saveKey = saveKey.Trim();

        foreach (char invalidChar in Path.GetInvalidFileNameChars())
            saveKey = saveKey.Replace(invalidChar, '_');

        return saveKey;
    }
    #endregion
}
