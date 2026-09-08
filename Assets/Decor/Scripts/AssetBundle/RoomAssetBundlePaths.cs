using System;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;

/// <summary>
/// 房间 AssetBundle 的命名、目录和远程地址约定。
/// RoomData.assetBundleName 仍使用原来的 Resources 路径（例如 Room/ApMai），由此换算出文件夹名和 bundle 文件名。
/// </summary>
public static class RoomAssetBundlePaths
{
    public const string SourceRoot = "Assets/Decor/RoomBundles";

    public const string StreamingFolderName = "ab";

    public const string ManifestFileName = "version.json";

    public const string RemoteUrlResourceName = "room_ab_remote_url";

    public const string UseEditorAssetsPrefKey = "Decor.RoomAB.UseEditorAssets";

    const string RoomPrefix = "Room/";

    /// <summary>
    /// 编辑器下默认直接读 RoomBundles 源资源，避免每次进房间都先打 AB。
    /// </summary>
    public static bool UseEditorAssets
    {
        get
        {
#if UNITY_EDITOR
            return UnityEditor.EditorPrefs.GetBool(UseEditorAssetsPrefKey, true);
#else
            return false;
#endif
        }
    }

    /// <summary>
    /// 从 RoomData.assetBundleName 取出房间文件夹名。
    /// </summary>
    /// <param name="assetBundleName">RoomData 上的路径，例如 Room/1_New_Living room。</param>
    /// <returns>对应 RoomBundles 下的文件夹名。</returns>
    public static string GetFolderName(string assetBundleName)
    {
        if (string.IsNullOrEmpty(assetBundleName))
            return string.Empty;

        if (assetBundleName.StartsWith(RoomPrefix))
            return assetBundleName.Substring(RoomPrefix.Length);

        return assetBundleName;
    }

    /// <summary>
    /// 把房间文件夹名转成可下载的 bundle 文件名。
    /// </summary>
    /// <param name="folderName">房间文件夹名。</param>
    /// <returns>小写、无空格的 bundle 名，例如 room_1_new_living_room。</returns>
    public static string GetBundleNameFromFolder(string folderName)
    {
        if (string.IsNullOrEmpty(folderName))
            return string.Empty;

        return "room_" + folderName.ToLowerInvariant().Replace(' ', '_');
    }

    /// <summary>
    /// 从 RoomData.assetBundleName 得到 bundle 文件名。
    /// </summary>
    /// <param name="assetBundleName">RoomData 上的路径。</param>
    /// <returns>bundle 文件名。</returns>
    public static string GetBundleName(string assetBundleName)
    {
        return GetBundleNameFromFolder(GetFolderName(assetBundleName));
    }

    /// <summary>
    /// 编辑器里房间源资源的根路径。
    /// </summary>
    /// <param name="assetBundleName">RoomData 上的路径。</param>
    /// <returns>Assets/Decor/RoomBundles 下的房间目录。</returns>
    public static string GetEditorRoomFolder(string assetBundleName)
    {
        return SourceRoot + "/" + GetFolderName(assetBundleName);
    }

    /// <summary>
    /// 当前运行平台对应的 AB 子目录名。APK 固定为 Android。
    /// </summary>
    /// <returns>平台目录名。</returns>
    public static string GetRuntimePlatformName()
    {
#if UNITY_EDITOR
        return UnityEditor.EditorUserBuildSettings.activeBuildTarget.ToString();
#elif UNITY_ANDROID
        return "Android";
#else
        return Application.platform.ToString();
#endif
    }

    /// <summary>
    /// 工程内打好的 AB 输出目录（不进 APK，供上传 CDN）。
    /// </summary>
    /// <param name="platformName">平台目录名。</param>
    /// <returns>项目根下 Bundles/平台 路径。</returns>
    public static string GetProjectBuildOutputDir(string platformName)
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        return Path.Combine(projectRoot, "Bundles", platformName);
    }

    /// <summary>
    /// APK 内置 AB 在 StreamingAssets 中的目录。
    /// </summary>
    /// <param name="platformName">平台目录名。</param>
    /// <returns>StreamingAssets/ab/平台 路径。</returns>
    public static string GetStreamingDir(string platformName)
    {
        return Path.Combine(Application.streamingAssetsPath, StreamingFolderName, platformName);
    }

    /// <summary>
    /// 热更后的本地缓存目录。
    /// </summary>
    /// <param name="platformName">平台目录名。</param>
    /// <returns>persistentDataPath/ab/平台 路径。</returns>
    public static string GetPersistentDir(string platformName)
    {
        return Path.Combine(Application.persistentDataPath, StreamingFolderName, platformName);
    }

    /// <summary>
    /// 缓存中某个房间 bundle 的文件路径。
    /// </summary>
    /// <param name="bundleName">bundle 文件名。</param>
    /// <returns>可给 AssetBundle.LoadFromFile 使用的路径。</returns>
    public static string GetPersistentBundlePath(string bundleName)
    {
        return Path.Combine(GetPersistentDir(GetRuntimePlatformName()), bundleName);
    }

    /// <summary>
    /// StreamingAssets 中某个房间 bundle 的访问路径。Android 上是 jar:file URL。
    /// </summary>
    /// <param name="bundleName">bundle 文件名。</param>
    /// <returns>文件路径或 jar URL。</returns>
    public static string GetStreamingBundleUrl(string bundleName)
    {
        string relative = StreamingFolderName + "/" + GetRuntimePlatformName() + "/" + bundleName;
#if UNITY_ANDROID && !UNITY_EDITOR
        return Application.streamingAssetsPath + "/" + relative;
#else
        return Path.Combine(Application.streamingAssetsPath, StreamingFolderName, GetRuntimePlatformName(), bundleName);
#endif
    }

    /// <summary>
    /// 读取远程 AB 根地址。空字符串表示只用包内资源，不检查热更。
    /// </summary>
    /// <returns>去掉末尾斜杠的 URL；未配置时为空。</returns>
    public static string GetRemoteBaseUrl()
    {
        TextAsset asset = Resources.Load<TextAsset>(RemoteUrlResourceName);
        if (asset == null)
            return string.Empty;

        string[] lines = asset.text.Split(new[] { '\r', '\n' });
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (line.Length == 0 || line.StartsWith("#"))
                continue;

            return line.TrimEnd('/');
        }

        return string.Empty;
    }

    /// <summary>
    /// 计算 bundle 文件的 MD5，用于 version.json 和本地缓存对比。
    /// </summary>
    /// <param name="path">文件完整路径。</param>
    /// <returns>小写十六进制 MD5。</returns>
    public static string ComputeFileHash(string path)
    {
        using (MD5 md5 = MD5.Create())
        using (FileStream stream = File.OpenRead(path))
        {
            byte[] hash = md5.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
    }
}
