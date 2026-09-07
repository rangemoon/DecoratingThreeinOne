/// <summary>
/// 通知服务接口。平台适配器实现此接口，游戏代码不再依赖具体平台 API。
/// </summary>
public interface INotificationService
{
    void Initialize();
    void OnApplicationQuit();
}
