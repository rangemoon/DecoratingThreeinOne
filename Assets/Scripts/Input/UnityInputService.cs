using UnityEngine;

/// <summary>
/// 默认输入实现，直接包装 UnityEngine.Input。
/// 适用于 Editor、Standalone、Android APK。
/// 移动端 Unity 自动将触控合成为鼠标事件，GetMouseButtonDown(0) 即可响应手指点击。
/// </summary>
public class UnityInputService : IInputService
{
    public bool PointerDown => Input.GetMouseButtonDown(0);
    public bool PointerHeld => Input.GetMouseButton(0);
    public bool PointerUp => Input.GetMouseButtonUp(0);
    public Vector2 PointerPosition => Input.mousePosition;
    public bool GetKeyDown(KeyCode keyCode) => Input.GetKeyDown(keyCode);
}
