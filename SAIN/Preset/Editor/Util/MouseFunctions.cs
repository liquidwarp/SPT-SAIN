using UnityEngine;

namespace SAIN.Editor.Util;

public static class MouseFunctions
{
    public static void Update()
    {
        checkMouseEvents();
    }

    private const float MOUSE_FUNC_TIME = 0.2f;

    private static Vector2 _lastMousePos;

    private static void checkMouseEvents()
    {
        Vector2 mousePos = Event.current.mousePosition;
        if ((mousePos - _lastMousePos).sqrMagnitude > 0.001f)
        {
            _mouseMoveTime = Time.time + MOUSE_FUNC_TIME;
        }
        _lastMousePos = mousePos;
        if (Event.current.type == EventType.MouseDown && Event.current.button == 1)
        {
            Sounds.PlaySound(EFT.UI.EUISoundType.ButtonBottomBarClick);
        }
    }

    public static bool MouseIsMoving
    {
        get { return _mouseMoveTime > Time.time; }
    }

    private static float _mouseMoveTime;

    public static bool IsMouseInside(Rect rect)
    {
        return rect.Contains(Event.current.mousePosition);
    }
}
