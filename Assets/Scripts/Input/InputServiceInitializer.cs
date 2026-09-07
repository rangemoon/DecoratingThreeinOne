using UnityEngine;

/// <summary>
/// 启动时根据平台自动选择输入服务实现。
/// 挂载到启动场景的持久 GameObject 上，Awake 中完成服务注册。
/// </summary>
public class InputServiceInitializer : MonoBehaviour
{
    [Tooltip("强制使用 Unity 原生输入（WebGL 桌面浏览器调试时勾选）")]
    [SerializeField] private bool forceUnityInput = false;

    private void Awake()
    {
        if (forceUnityInput)
        {
            GameInput.Service = new UnityInputService();
        }
        #if MINIGAME_HONOR || MINIGAME_VIVO
        else if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            var go = new GameObject("QGInputService");
            DontDestroyOnLoad(go);
            GameInput.Service = go.AddComponent<QGInputService>();
        }
        #endif
        else
        {
            GameInput.Service = new UnityInputService();
        }
    }
}
