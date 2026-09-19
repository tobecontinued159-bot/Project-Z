using UnityEngine;

public static class PlayerInputLock
{
    public static bool IsTerminalOpen { get; private set; }

    private static CursorLockMode _savedLockMode = CursorLockMode.None;
    private static bool _savedCursorVisible = true;

    public static void SetTerminalOpen(bool open)
    {
        if (IsTerminalOpen == open)
        {
            return;
        }

        IsTerminalOpen = open;

        if (open)
        {
            _savedLockMode = Cursor.lockState;
            _savedCursorVisible = Cursor.visible;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        Cursor.lockState = _savedLockMode;
        Cursor.visible = _savedCursorVisible;
    }
}
