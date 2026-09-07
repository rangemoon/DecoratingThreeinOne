using UnityEngine;

/// <summary>
/// 统一输入服务接口。
/// 将鼠标左键和主触控点抽象为 Pointer，游戏代码不再区分平台。
/// </summary>
public interface IInputService
{
    /// <summary>指针刚按下的一帧（鼠标左键按下 / 触控 Began）</summary>
    bool PointerDown { get; }

    /// <summary>指针持续按住（鼠标左键按住 / 触控 Moved/Stationary）</summary>
    bool PointerHeld { get; }

    /// <summary>指针抬起的一帧（鼠标左键抬起 / 触控 Ended/Cancelled）</summary>
    bool PointerUp { get; }

    /// <summary>指针当前屏幕坐标（鼠标位置 / 触控位置）</summary>
    Vector2 PointerPosition { get; }

    /// <summary>指定按键刚按下的一帧</summary>
    bool GetKeyDown(KeyCode keyCode);
}
