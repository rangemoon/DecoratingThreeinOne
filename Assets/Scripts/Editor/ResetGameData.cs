using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 测试工具：一键清除所有游戏进度，回到新手初始状态。
/// 菜单栏 → Tools → Reset Game Data
/// </summary>
public static class ResetGameData
{
    [MenuItem("Tools/Reset Game Data")]
    public static void Reset()
    {
        // 1. 清除 PlayerPrefs（音频设置、广告计数、语言等）
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        // 2. 删除 playerdata 存档文件
        string filePath = PlayerData.GetFilePath();
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            Debug.Log("[ResetGameData] Deleted: " + filePath);
        }
        else
        {
            Debug.LogWarning("[ResetGameData] File not found: " + filePath);
        }

        // 3. 清除 persistentDataPath 下可能的残留文件
        string dataPath = Application.persistentDataPath;
        if (Directory.Exists(dataPath))
        {
            var files = Directory.GetFiles(dataPath, "playerdata*");
            foreach (var f in files)
            {
                File.Delete(f);
                Debug.Log("[ResetGameData] Deleted: " + f);
            }
        }

        // 4. 清除 Editor 本地缓存（如果存在）
        string editorPath = "";
        if (File.Exists(editorPath + "playerdata.txt"))
        {
            File.Delete(editorPath + "playerdata.txt");
            Debug.Log("[ResetGameData] Deleted editor cache: " + editorPath + "playerdata.txt");
        }

        Debug.Log("<color=green>[ResetGameData] Done! Restart the game to see the effect.</color>");
        EditorUtility.DisplayDialog("Reset Game Data",
            "All game data has been cleared.\n\nRestart the game (stop & play) to start fresh.",
            "OK");
    }
}
