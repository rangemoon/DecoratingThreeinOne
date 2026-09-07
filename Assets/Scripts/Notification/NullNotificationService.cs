/// <summary>
/// 空通知实现。用于不支持推送通知的平台（WebGL、Editor 等）。
/// </summary>
public class NullNotificationService : INotificationService
{
    public void Initialize() { }
    public void OnApplicationQuit() { }
}
