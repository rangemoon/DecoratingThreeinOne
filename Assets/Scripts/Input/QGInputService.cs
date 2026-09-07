#if MINIGAME_HONOR || MINIGAME_VIVO
using System.Collections.Generic;
using UnityEngine;
using QGMiniGame;

/// <summary>
/// WebGL 专用输入实现（Honor/vivo/oppo 小游戏平台）。
/// 自行注册 QG SDK 触控事件，独立于 QGTouchInputOverride（后者服务于 uGUI EventSystem）。
/// 当 QG 运行时不可用时（桌面浏览器调试），自动回退到 Unity 鼠标输入。
/// </summary>
public class QGInputService : MonoBehaviour, IInputService
{
    private class QGTouch
    {
        public int fingerId;
        public Vector2 position;
        public TouchPhase phase;
        public bool phaseChangedThisFrame;
    }

    private readonly List<QGTouch> _touches = new List<QGTouch>();
    private bool _qgAvailable;
    private bool _pointerDown;
    private bool _pointerUp;
    private bool _pointerHeld;
    private Vector2 _pointerPosition;

    private void OnEnable()
    {
        _qgAvailable = false;
        try
        {
            QG.OnTouchStart(OnQGTouchStart);
            QG.OnTouchMove(OnQGTouchMove);
            QG.OnTouchEnd(OnQGTouchEnd);
            QG.OnTouchCancel(OnQGTouchCancel);
            _qgAvailable = true;
        }
        catch (System.Exception)
        {
            // QG 运行时不可用（桌面浏览器调试），回退到鼠标输入
            _qgAvailable = false;
        }
    }

    private void OnDisable()
    {
        if (_qgAvailable)
        {
            QG.OffTouchStart(OnQGTouchStart);
            QG.OffTouchMove(OnQGTouchMove);
            QG.OffTouchEnd(OnQGTouchEnd);
            QG.OffTouchCancel(OnQGTouchCancel);
        }
    }

    private void Update()
    {
        if (_qgAvailable && _touches.Count > 0)
        {
            var t = _touches[0];
            _pointerDown = t.phaseChangedThisFrame && t.phase == TouchPhase.Began;
            _pointerUp = (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled);
            _pointerHeld = !_pointerDown && !_pointerUp;
            _pointerPosition = t.position;
        }
        else
        {
            // 回退：Unity 鼠标输入（WebGL 桌面浏览器测试）
            _pointerDown = Input.GetMouseButtonDown(0);
            _pointerHeld = Input.GetMouseButton(0);
            _pointerUp = Input.GetMouseButtonUp(0);
            _pointerPosition = Input.mousePosition;
        }
    }

    #region QG SDK Touch Events

    private void OnQGTouchStart(OnTouchStartCallbackResult e)
    {
        foreach (var qgTouch in e.changedTouches)
        {
            var data = FindOrCreateTouch(qgTouch.identifier);
            data.phase = TouchPhase.Began;
            data.position = new Vector2(qgTouch.clientX, qgTouch.clientY);
            data.phaseChangedThisFrame = true;
        }
    }

    private void OnQGTouchMove(OnTouchStartCallbackResult e)
    {
        foreach (var qgTouch in e.changedTouches)
        {
            var data = FindTouch(qgTouch.identifier);
            if (data == null) continue;
            data.phase = TouchPhase.Moved;
            data.position = new Vector2(qgTouch.clientX, qgTouch.clientY);
            data.phaseChangedThisFrame = true;
        }
    }

    private void OnQGTouchEnd(OnTouchStartCallbackResult e)
    {
        foreach (var qgTouch in e.changedTouches)
        {
            var data = FindTouch(qgTouch.identifier);
            if (data == null) continue;
            data.phase = TouchPhase.Ended;
            data.position = new Vector2(qgTouch.clientX, qgTouch.clientY);
            data.phaseChangedThisFrame = true;
        }
    }

    private void OnQGTouchCancel(OnTouchStartCallbackResult e)
    {
        foreach (var qgTouch in e.changedTouches)
        {
            var data = FindTouch(qgTouch.identifier);
            if (data == null) continue;
            data.phase = TouchPhase.Canceled;
            data.position = new Vector2(qgTouch.clientX, qgTouch.clientY);
            data.phaseChangedThisFrame = true;
        }
    }

    #endregion

    #region Touch Data Management

    private QGTouch FindOrCreateTouch(int identifier)
    {
        var data = FindTouch(identifier);
        if (data != null && data.phase != TouchPhase.Ended && data.phase != TouchPhase.Canceled)
            return data;

        data = new QGTouch
        {
            fingerId = identifier,
            phase = TouchPhase.Canceled,
            phaseChangedThisFrame = false
        };
        _touches.Add(data);
        return data;
    }

    private QGTouch FindTouch(int identifier)
    {
        for (int i = 0; i < _touches.Count; i++)
        {
            if (_touches[i].fingerId == identifier)
                return _touches[i];
        }
        return null;
    }

    /// <summary>每帧结束时清理已结束/取消的触控点</summary>
    private void LateUpdate()
    {
        // 重置 phaseChangedThisFrame 标记
        for (int i = 0; i < _touches.Count; i++)
        {
            _touches[i].phaseChangedThisFrame = false;
        }

        // 移除已结束的触控点
        _touches.RemoveAll(t => t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled);
    }

    #endregion

    #region IInputService

    public bool PointerDown => _pointerDown;
    public bool PointerHeld => _pointerHeld;
    public bool PointerUp => _pointerUp;
    public Vector2 PointerPosition => _pointerPosition;

    /// <summary>
    /// QG 小游戏平台（Honor/vivo/oppo）运行在宿主 App 内，无物理键盘，始终返回 false。
    /// </summary>
    public bool GetKeyDown(KeyCode keyCode) => false;

    #endregion
}

#endif