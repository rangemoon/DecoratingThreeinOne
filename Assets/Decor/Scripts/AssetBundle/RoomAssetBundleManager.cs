using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// 房间 AssetBundle 的准备、热更和加载。编辑器默认读源资源；真机先对比 version.json，再从缓存或包内加载。
/// </summary>
public class RoomAssetBundleManager : SingletonMonoBehaviour<RoomAssetBundleManager>
{
    static readonly string[] SpriteFolders = { "UI", "Texture2D", "Final", "Sprite" };

    static readonly string[] SpriteExtensions = { ".png", ".jpg", ".jpeg", ".tga", ".psd" };

    readonly Dictionary<string, AssetBundle> loadedBundles = new Dictionary<string, AssetBundle>();

    RoomBundleManifest manifest;

    bool manifestLoaded;

    /// <summary>
    /// 按当前存档房间下载或打开对应 AssetBundle。加载界面在进装饰场景前调用，避免进房后才卡下载。
    /// </summary>
    /// <param name="done">准备完成回调，参数为是否成功。</param>
    /// <returns>协程迭代器。</returns>
    public IEnumerator PrepareCurrentRoom(Action<bool> done)
    {
        int roomId = PlayerData.current.homeDesignData.currentRoomId;
        RoomData room = RoomDataTable.Instance.GetRoomDataWithId(roomId);
        if (room == null)
        {
            Debug.LogError("RoomData missing for id " + roomId);
            done?.Invoke(false);
            yield break;
        }

        yield return EnsureRoomReady(room.assetBundleName, done);
    }

    /// <summary>
    /// 确保指定房间的 bundle 已更新并加载到内存。
    /// </summary>
    /// <param name="assetBundleName">RoomData.assetBundleName。</param>
    /// <param name="done">完成回调，参数为是否成功。</param>
    /// <returns>协程迭代器。</returns>
    public IEnumerator EnsureRoomReady(string assetBundleName, Action<bool> done)
    {
#if UNITY_EDITOR
        if (RoomAssetBundlePaths.UseEditorAssets)
        {
            bool prefabExists = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                RoomAssetBundlePaths.GetEditorRoomFolder(assetBundleName) + "/" + RoomData.RoomPrefabName + ".prefab") != null;
            if (!prefabExists)
                Debug.LogError("Editor room prefab missing: " + assetBundleName);
            done?.Invoke(prefabExists);
            yield break;
        }
#endif

        string bundleName = RoomAssetBundlePaths.GetBundleName(assetBundleName);
        if (string.IsNullOrEmpty(bundleName))
        {
            done?.Invoke(false);
            yield break;
        }

        AssetBundle alreadyLoaded;
        if (loadedBundles.TryGetValue(bundleName, out alreadyLoaded) && alreadyLoaded != null)
        {
            done?.Invoke(true);
            yield break;
        }

        yield return EnsureManifestLoaded();

        string persistentPath = RoomAssetBundlePaths.GetPersistentBundlePath(bundleName);
        RoomBundleInfo info = FindBundleInfo(bundleName);
        bool cacheExists = File.Exists(persistentPath);
        string targetHash = info != null ? info.hash : string.Empty;
        bool hashOk = cacheExists && (string.IsNullOrEmpty(targetHash) ||
            string.Equals(RoomAssetBundlePaths.ComputeFileHash(persistentPath), targetHash, StringComparison.OrdinalIgnoreCase));

        if (!hashOk)
        {
            if (loadedBundles.ContainsKey(bundleName))
                UnloadBundle(bundleName);

            bool updated = false;
            string remoteUrl = RoomAssetBundlePaths.GetRemoteBaseUrl();
            bool hasRemote = !string.IsNullOrEmpty(remoteUrl) && ServiceUtility.InternetAvailable;
            if (hasRemote)
                yield return DownloadFile(remoteUrl + "/" + bundleName, persistentPath, success => updated = success);

            // 无远程、或本地还没有缓存时，回退到 APK 内置 StreamingAssets。
            if (!updated && (!cacheExists || !hasRemote))
                yield return CopyFromStreamingAssets(bundleName, persistentPath, success => updated = success);

            if (!File.Exists(persistentPath))
            {
                Debug.LogError("Room bundle not available: " + bundleName);
                done?.Invoke(false);
                yield break;
            }
        }

        if (!loadedBundles.ContainsKey(bundleName) || loadedBundles[bundleName] == null)
        {
            AssetBundle bundle = AssetBundle.LoadFromFile(persistentPath);
            if (bundle == null)
            {
                Debug.LogError("LoadFromFile failed: " + persistentPath);
                done?.Invoke(false);
                yield break;
            }

            loadedBundles[bundleName] = bundle;
        }

        done?.Invoke(true);
    }

    /// <summary>
    /// 从已准备好的房间 bundle（或编辑器源资源）加载房间 Prefab。
    /// </summary>
    /// <param name="assetBundleName">RoomData.assetBundleName。</param>
    /// <returns>房间 Prefab；失败时为 null。</returns>
    public GameObject LoadRoomPrefab(string assetBundleName)
    {
#if UNITY_EDITOR
        if (RoomAssetBundlePaths.UseEditorAssets)
            return LoadEditorAsset<GameObject>(assetBundleName, RoomData.RoomPrefabName, new[] { "" }, new[] { ".prefab" });
#endif
        AssetBundle bundle = GetLoadedBundle(assetBundleName);
        return bundle != null ? bundle.LoadAsset<GameObject>(RoomData.RoomPrefabName) : null;
    }

    /// <summary>
    /// 按旧 Resources 目录顺序查找变体预览图：先 *_mini，再原名。
    /// </summary>
    /// <param name="assetBundleName">RoomData.assetBundleName。</param>
    /// <param name="spriteName">道具变体名，例如 c1_r1_sofa_1。</param>
    /// <returns>找到的 Sprite；都没有则为 null。</returns>
    public Sprite LoadVariantPreviewSprite(string assetBundleName, string spriteName)
    {
        Sprite sprite = LoadSprite(assetBundleName, spriteName + "_mini");
        if (sprite != null)
            return sprite;

        return LoadSprite(assetBundleName, spriteName);
    }

    /// <summary>
    /// 卸载指定房间 bundle。场景销毁后再调，避免正在显示的 Sprite 被卸掉。
    /// </summary>
    /// <param name="assetBundleName">RoomData.assetBundleName。</param>
    public void UnloadRoom(string assetBundleName)
    {
        UnloadBundle(RoomAssetBundlePaths.GetBundleName(assetBundleName));
    }

    Sprite LoadSprite(string assetBundleName, string spriteName)
    {
#if UNITY_EDITOR
        if (RoomAssetBundlePaths.UseEditorAssets)
            return LoadEditorAsset<Sprite>(assetBundleName, spriteName, SpriteFolders, SpriteExtensions);
#endif
        AssetBundle bundle = GetLoadedBundle(assetBundleName);
        if (bundle == null)
            return null;

        Sprite sprite = bundle.LoadAsset<Sprite>(spriteName);
        if (sprite != null)
            return sprite;

        UnityEngine.Object[] subAssets = bundle.LoadAssetWithSubAssets(spriteName);
        if (subAssets == null)
            return null;

        for (int i = 0; i < subAssets.Length; i++)
        {
            sprite = subAssets[i] as Sprite;
            if (sprite != null)
                return sprite;
        }

        return null;
    }

    AssetBundle GetLoadedBundle(string assetBundleName)
    {
        AssetBundle bundle;
        loadedBundles.TryGetValue(RoomAssetBundlePaths.GetBundleName(assetBundleName), out bundle);
        return bundle;
    }

    void UnloadBundle(string bundleName)
    {
        AssetBundle bundle;
        if (!loadedBundles.TryGetValue(bundleName, out bundle))
            return;

        if (bundle != null)
            bundle.Unload(false);

        loadedBundles.Remove(bundleName);
    }

    IEnumerator EnsureManifestLoaded()
    {
        if (manifestLoaded)
            yield break;

        manifestLoaded = true;

        RoomBundleManifest local = null;
        yield return LoadManifest(RoomAssetBundlePaths.GetStreamingBundleUrl(RoomAssetBundlePaths.ManifestFileName), loaded => local = loaded);

        string remoteUrl = RoomAssetBundlePaths.GetRemoteBaseUrl();
        RoomBundleManifest remote = null;
        if (!string.IsNullOrEmpty(remoteUrl) && ServiceUtility.InternetAvailable)
            yield return LoadManifest(remoteUrl + "/" + RoomAssetBundlePaths.ManifestFileName, loaded => remote = loaded);

        manifest = remote != null ? remote : local;
    }

    RoomBundleInfo FindBundleInfo(string bundleName)
    {
        if (manifest == null || manifest.bundles == null)
            return null;

        for (int i = 0; i < manifest.bundles.Length; i++)
        {
            if (manifest.bundles[i] != null && manifest.bundles[i].name == bundleName)
                return manifest.bundles[i];
        }

        return null;
    }

    IEnumerator LoadManifest(string url, Action<RoomBundleManifest> done)
    {
        if (string.IsNullOrEmpty(url))
        {
            done?.Invoke(null);
            yield break;
        }

        if (!url.Contains("://"))
        {
            if (!File.Exists(url))
            {
                done?.Invoke(null);
                yield break;
            }

            try
            {
                done?.Invoke(JsonUtility.FromJson<RoomBundleManifest>(File.ReadAllText(url)));
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Room bundle manifest parse failed: " + ex.Message);
                done?.Invoke(null);
            }

            yield break;
        }

        using (UnityWebRequest request = UnityWebRequest.Get(ToRequestUrl(url)))
        {
            request.timeout = 15;
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("Room bundle manifest skip: " + url + " " + request.error);
                done?.Invoke(null);
                yield break;
            }

            try
            {
                done?.Invoke(JsonUtility.FromJson<RoomBundleManifest>(request.downloadHandler.text));
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Room bundle manifest parse failed: " + ex.Message);
                done?.Invoke(null);
            }
        }
    }

    IEnumerator DownloadFile(string url, string destPath, Action<bool> done)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destPath));
        string tempPath = destPath + ".tmp";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.timeout = 120;
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("Download room bundle failed: " + url + " " + request.error);
                done?.Invoke(false);
                yield break;
            }

            File.WriteAllBytes(tempPath, request.downloadHandler.data);
        }

        ReplaceFile(tempPath, destPath);
        done?.Invoke(true);
    }

    IEnumerator CopyFromStreamingAssets(string bundleName, string destPath, Action<bool> done)
    {
        string src = RoomAssetBundlePaths.GetStreamingBundleUrl(bundleName);
        Directory.CreateDirectory(Path.GetDirectoryName(destPath));

        if (src.Contains("://"))
        {
            yield return DownloadFile(src, destPath, done);
            yield break;
        }

        if (!File.Exists(src))
        {
            done?.Invoke(false);
            yield break;
        }

        File.Copy(src, destPath, true);
        done?.Invoke(true);
    }

    static void ReplaceFile(string tempPath, string destPath)
    {
        if (File.Exists(destPath))
            File.Delete(destPath);

        File.Move(tempPath, destPath);
    }

    static string ToRequestUrl(string pathOrUrl)
    {
        if (pathOrUrl.Contains("://"))
            return pathOrUrl.Replace('\\', '/');

        return "file://" + pathOrUrl.Replace('\\', '/');
    }

#if UNITY_EDITOR
    static T LoadEditorAsset<T>(string assetBundleName, string assetName, string[] subFolders, string[] extensions) where T : UnityEngine.Object
    {
        string root = RoomAssetBundlePaths.GetEditorRoomFolder(assetBundleName);
        for (int i = 0; i < subFolders.Length; i++)
        {
            string dir = string.IsNullOrEmpty(subFolders[i]) ? root : root + "/" + subFolders[i];
            for (int j = 0; j < extensions.Length; j++)
            {
                T asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(dir + "/" + assetName + extensions[j]);
                if (asset != null)
                    return asset;
            }
        }

        return null;
    }
#endif
}
