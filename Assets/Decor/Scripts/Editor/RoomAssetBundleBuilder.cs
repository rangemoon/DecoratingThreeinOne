using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

/// <summary>
/// 按房间文件夹打 Android AssetBundle，写入 Bundles/ 并复制到 StreamingAssets 供 APK 内置。
/// </summary>
public static class RoomAssetBundleBuilder
{
    const string AndroidMenu = "Decor/Build Room AssetBundles/Android";
    const string ActiveMenu = "Decor/Build Room AssetBundles/Active Build Target";
    const string EditorAssetsMenu = "Decor/Room AssetBundles/Use Editor Assets";

    /// <summary>
    /// 为 Android 目标打房间 AB，供 APK 使用。
    /// </summary>
    [MenuItem(AndroidMenu)]
    public static void BuildAndroid()
    {
        Build(BuildTarget.Android, true);
    }

    /// <summary>
    /// 为当前激活的构建设备打房间 AB，便于编辑器里验证真 bundle。
    /// </summary>
    [MenuItem(ActiveMenu)]
    public static void BuildActiveTarget()
    {
        Build(EditorUserBuildSettings.activeBuildTarget, true);
    }

    [MenuItem(EditorAssetsMenu, true)]
    static bool UseEditorAssetsValidate()
    {
        Menu.SetChecked(EditorAssetsMenu, EditorPrefs.GetBool(RoomAssetBundlePaths.UseEditorAssetsPrefKey, true));
        return true;
    }

    [MenuItem(EditorAssetsMenu)]
    static void ToggleUseEditorAssets()
    {
        bool current = EditorPrefs.GetBool(RoomAssetBundlePaths.UseEditorAssetsPrefKey, true);
        EditorPrefs.SetBool(RoomAssetBundlePaths.UseEditorAssetsPrefKey, !current);
    }

    /// <summary>
    /// 收集每个房间文件夹中的资源，打成独立 bundle，并生成 version.json。
    /// </summary>
    /// <param name="target">Unity 构建目标。</param>
    /// <param name="refreshAssets">为 true 时刷新 AssetDatabase。出包预处理里必须为 false，避免构建中途 Refresh。</param>
    public static void Build(BuildTarget target, bool refreshAssets)
    {
        if (!AssetDatabase.IsValidFolder(RoomAssetBundlePaths.SourceRoot))
            throw new DirectoryNotFoundException("Room source folder missing: " + RoomAssetBundlePaths.SourceRoot);

        AssetBundleBuild[] builds = CollectRoomBuilds();
        if (builds.Length == 0)
            throw new InvalidOperationException("No room folders found under " + RoomAssetBundlePaths.SourceRoot);

        string platformName = target.ToString();
        string outputDir = RoomAssetBundlePaths.GetProjectBuildOutputDir(platformName);
        Directory.CreateDirectory(outputDir);

        AssetBundleManifest unityManifest = BuildPipeline.BuildAssetBundles(
            outputDir,
            builds,
            BuildAssetBundleOptions.ChunkBasedCompression,
            target);

        if (unityManifest == null)
            throw new Exception("Room AssetBundle build failed for " + target + ". Console 若有 error CSxxxx，先修好脚本再打 AB。");

        WriteVersionJson(outputDir, platformName, builds);
        CopyToStreamingAssets(outputDir, platformName, builds);
        if (refreshAssets)
            AssetDatabase.Refresh();
        Debug.Log("Built " + builds.Length + " room AssetBundles for " + target + " -> " + outputDir);
    }

    /// <summary>
    /// 确认 APK 能带上已打好的房间 AB。Player 构建进行中不能再调用 BuildAssetBundles，所以这里只检查或复制现成文件。
    /// </summary>
    /// <param name="target">Unity 构建目标。</param>
    public static void EnsureStreamingAssetsForPlayerBuild(BuildTarget target)
    {
        string platformName = target.ToString();
        string streamingManifest = Path.Combine(
            Application.dataPath,
            "StreamingAssets",
            RoomAssetBundlePaths.StreamingFolderName,
            platformName,
            RoomAssetBundlePaths.ManifestFileName);

        if (File.Exists(streamingManifest))
            return;

        string outputDir = RoomAssetBundlePaths.GetProjectBuildOutputDir(platformName);
        string builtManifest = Path.Combine(outputDir, RoomAssetBundlePaths.ManifestFileName);
        if (!File.Exists(builtManifest))
        {
            throw new BuildFailedException(
                "房间 AssetBundle 尚未生成。请先执行菜单 Decor/Build Room AssetBundles/Android，完成后再打 APK。");
        }

        string destDir = Path.Combine(Application.dataPath, "StreamingAssets", RoomAssetBundlePaths.StreamingFolderName, platformName);
        Directory.CreateDirectory(destDir);

        string[] oldFiles = Directory.GetFiles(destDir);
        for (int i = 0; i < oldFiles.Length; i++)
            File.Delete(oldFiles[i]);

        CopyFileToDir(builtManifest, destDir);
        string[] builtFiles = Directory.GetFiles(outputDir);
        for (int i = 0; i < builtFiles.Length; i++)
        {
            string fileName = Path.GetFileName(builtFiles[i]);
            if (!fileName.StartsWith("room_", StringComparison.OrdinalIgnoreCase))
                continue;
            CopyFileToDir(builtFiles[i], destDir);
        }
    }

    /// <summary>
    /// 每个房间文件夹打成一个独立 bundle，依赖资源打进该房间包内，避免房间之间互相依赖。
    /// </summary>
    /// <returns>待构建的 AssetBundleBuild 列表。</returns>
    static AssetBundleBuild[] CollectRoomBuilds()
    {
        string[] roomFolders = AssetDatabase.GetSubFolders(RoomAssetBundlePaths.SourceRoot);
        var builds = new List<AssetBundleBuild>(roomFolders.Length);

        for (int i = 0; i < roomFolders.Length; i++)
        {
            string roomPath = roomFolders[i];
            string folderName = Path.GetFileName(roomPath);
            string[] guids = AssetDatabase.FindAssets(string.Empty, new[] { roomPath });
            var assets = new List<string>(guids.Length);

            for (int g = 0; g < guids.Length; g++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[g]);
                if (string.IsNullOrEmpty(assetPath) || AssetDatabase.IsValidFolder(assetPath))
                    continue;
                if (assetPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                    continue;

                assets.Add(assetPath);
            }

            if (assets.Count == 0)
            {
                Debug.LogWarning("Skip empty room folder: " + roomPath);
                continue;
            }

            builds.Add(new AssetBundleBuild
            {
                assetBundleName = RoomAssetBundlePaths.GetBundleNameFromFolder(folderName),
                assetNames = assets.ToArray()
            });
        }

        return builds.ToArray();
    }

    /// <summary>
    /// 写入 version.json，记录每个房间 bundle 的 MD5 和大小，供运行时热更对比。
    /// </summary>
    /// <param name="outputDir">bundle 输出目录。</param>
    /// <param name="platformName">平台目录名。</param>
    /// <param name="builds">本次打出的房间 bundle 列表。</param>
    static void WriteVersionJson(string outputDir, string platformName, AssetBundleBuild[] builds)
    {
        var infos = new RoomBundleInfo[builds.Length];
        for (int i = 0; i < builds.Length; i++)
        {
            string bundleName = builds[i].assetBundleName;
            string filePath = Path.Combine(outputDir, bundleName);
            var fileInfo = new FileInfo(filePath);
            infos[i] = new RoomBundleInfo
            {
                name = bundleName,
                hash = fileInfo.Exists ? RoomAssetBundlePaths.ComputeFileHash(filePath) : string.Empty,
                size = fileInfo.Exists ? fileInfo.Length : 0
            };
        }

        var manifest = new RoomBundleManifest
        {
            version = 1,
            platform = platformName,
            bundles = infos
        };

        File.WriteAllText(Path.Combine(outputDir, RoomAssetBundlePaths.ManifestFileName), JsonUtility.ToJson(manifest, true));
    }

    /// <summary>
    /// 把房间 bundle 和 version.json 复制进 StreamingAssets，打进 APK。
    /// </summary>
    /// <param name="outputDir">构建输出目录。</param>
    /// <param name="platformName">平台目录名。</param>
    /// <param name="builds">本次打出的房间 bundle 列表。</param>
    static void CopyToStreamingAssets(string outputDir, string platformName, AssetBundleBuild[] builds)
    {
        string destDir = Path.Combine(Application.dataPath, "StreamingAssets", RoomAssetBundlePaths.StreamingFolderName, platformName);
        Directory.CreateDirectory(destDir);

        string[] oldFiles = Directory.GetFiles(destDir);
        for (int i = 0; i < oldFiles.Length; i++)
            File.Delete(oldFiles[i]);

        CopyFileToDir(Path.Combine(outputDir, RoomAssetBundlePaths.ManifestFileName), destDir);
        for (int i = 0; i < builds.Length; i++)
            CopyFileToDir(Path.Combine(outputDir, builds[i].assetBundleName), destDir);
    }

    /// <summary>
    /// 复制单个已构建文件到 StreamingAssets 目标目录。
    /// </summary>
    /// <param name="sourceFile">源文件路径。</param>
    /// <param name="destDir">目标目录。</param>
    static void CopyFileToDir(string sourceFile, string destDir)
    {
        if (!File.Exists(sourceFile))
            throw new FileNotFoundException("Built room bundle missing", sourceFile);

        File.Copy(sourceFile, Path.Combine(destDir, Path.GetFileName(sourceFile)), true);
    }
}
