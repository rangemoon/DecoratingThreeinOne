using System;

/// <summary>
/// 房间 AssetBundle 版本清单，对应 version.json。
/// </summary>
[Serializable]
public class RoomBundleManifest
{
    public int version;

    public string platform;

    public RoomBundleInfo[] bundles;
}

/// <summary>
/// 单个房间 bundle 的校验信息。
/// </summary>
[Serializable]
public class RoomBundleInfo
{
    public string name;

    public string hash;

    public long size;
}
