using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

/// <summary>
/// Android 出包前确认 StreamingAssets 里已有房间 AB。不能在 Player 构建过程中调用 BuildAssetBundles。
/// </summary>
public class RoomAssetBundleBuildProcessor : IPreprocessBuildWithReport
{
    public int callbackOrder
    {
        get { return 0; }
    }

    /// <summary>
    /// 仅检查或复制已打好的房间包，缺少时中止构建并提示先跑打包菜单。
    /// </summary>
    /// <param name="report">Unity 构建报告。</param>
    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.Android)
            return;

        RoomAssetBundleBuilder.EnsureStreamingAssetsForPlayerBuild(BuildTarget.Android);
    }
}
