/// <summary>
/// 输入服务静态定位器。
/// 由 InputServiceInitializer 在启动时赋值，游戏代码统一通过 GameInput.Service 访问输入。
/// </summary>
public static class GameInput
{
    public static IInputService Service { get; set; }
}
