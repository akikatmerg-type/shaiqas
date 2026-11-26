// SHAIQAS · Енгізу: пернетақта, тінтуір және көп саусақты сенсор.
// Unity-дің ескі (Input Manager) және жаңа (Input System) жүйелерінің екеуімен де жұмыс істейді.
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public static class Inp
{
    public const int A = 0, D = 1, W = 2, S = 3, J = 4, K = 5, L = 6, LEFT = 7, RIGHT = 8, UP = 9, DOWN = 10, SPACE = 11, ENTER = 12, ESC = 13, F12 = 14, F11 = 15, F10 = 16, U = 17, F9 = 18, F8 = 19, N1 = 20, N2 = 21, N3 = 22, N4 = 23, N = 24;
    static bool[] now = new bool[N], prev = new bool[N];
    public static Vector2[] touches = new Vector2[10];   // экран координаттары (y — жоғарыдан төмен)
    public static int touchCount;

    public static void Poll()
    {
        for (int i = 0; i < N; i++) prev[i] = now[i];
        touchCount = 0;
#if ENABLE_INPUT_SYSTEM
        Keyboard kb = Keyboard.current;
        if (kb != null)
        {
            now[A] = kb.aKey.isPressed; now[D] = kb.dKey.isPressed; now[W] = kb.wKey.isPressed; now[S] = kb.sKey.isPressed;
            now[J] = kb.jKey.isPressed; now[K] = kb.kKey.isPressed; now[L] = kb.lKey.isPressed;
            now[LEFT] = kb.leftArrowKey.isPressed; now[RIGHT] = kb.rightArrowKey.isPressed; now[UP] = kb.upArrowKey.isPressed; now[DOWN] = kb.downArrowKey.isPressed;
            now[SPACE] = kb.spaceKey.isPressed; now[ENTER] = kb.enterKey.isPressed; now[ESC] = kb.escapeKey.isPressed; now[F12] = kb.f12Key.isPressed; now[F11] = kb.f11Key.isPressed; now[F10] = kb.f10Key.isPressed; now[U] = kb.uKey.isPressed; now[F9] = kb.f9Key.isPressed; now[F8] = kb.f8Key.isPressed;
            now[N1] = kb.digit1Key.isPressed || kb.numpad1Key.isPressed; now[N2] = kb.digit2Key.isPressed || kb.numpad2Key.isPressed;
            now[N3] = kb.digit3Key.isPressed || kb.numpad3Key.isPressed; now[N4] = kb.digit4Key.isPressed || kb.numpad4Key.isPressed;
        }
        else for (int i = 0; i < N; i++) now[i] = false;
        Touchscreen ts = Touchscreen.current;
        if (ts != null)
        {
            for (int i = 0; i < ts.touches.Count && touchCount < touches.Length; i++)
            {
                var t = ts.touches[i];
                if (!t.press.isPressed) continue;
                Vector2 p = t.position.ReadValue();
                touches[touchCount++] = new Vector2(p.x, Screen.height - p.y);
            }
        }
        Mouse ms = Mouse.current;
        if (touchCount == 0 && ms != null && ms.leftButton.isPressed)
        {
            Vector2 p = ms.position.ReadValue();
            touches[touchCount++] = new Vector2(p.x, Screen.height - p.y);
        }
#else
        now[A] = Input.GetKey(KeyCode.A); now[D] = Input.GetKey(KeyCode.D); now[W] = Input.GetKey(KeyCode.W); now[S] = Input.GetKey(KeyCode.S);
        now[J] = Input.GetKey(KeyCode.J); now[K] = Input.GetKey(KeyCode.K); now[L] = Input.GetKey(KeyCode.L);
        now[LEFT] = Input.GetKey(KeyCode.LeftArrow); now[RIGHT] = Input.GetKey(KeyCode.RightArrow); now[UP] = Input.GetKey(KeyCode.UpArrow); now[DOWN] = Input.GetKey(KeyCode.DownArrow);
        now[SPACE] = Input.GetKey(KeyCode.Space); now[ENTER] = Input.GetKey(KeyCode.Return); now[ESC] = Input.GetKey(KeyCode.Escape); now[F12] = Input.GetKey(KeyCode.F12); now[F11] = Input.GetKey(KeyCode.F11); now[F10] = Input.GetKey(KeyCode.F10); now[U] = Input.GetKey(KeyCode.U); now[F9] = Input.GetKey(KeyCode.F9); now[F8] = Input.GetKey(KeyCode.F8);
        now[N1] = Input.GetKey(KeyCode.Alpha1) || Input.GetKey(KeyCode.Keypad1); now[N2] = Input.GetKey(KeyCode.Alpha2) || Input.GetKey(KeyCode.Keypad2);
        now[N3] = Input.GetKey(KeyCode.Alpha3) || Input.GetKey(KeyCode.Keypad3); now[N4] = Input.GetKey(KeyCode.Alpha4) || Input.GetKey(KeyCode.Keypad4);
        for (int i = 0; i < Input.touchCount && touchCount < touches.Length; i++)
        {
            Touch t = Input.GetTouch(i);
            if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) continue;
            touches[touchCount++] = new Vector2(t.position.x, Screen.height - t.position.y);
        }
        if (touchCount == 0 && Input.GetMouseButton(0))
            touches[touchCount++] = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
#endif
    }
    public static bool Down(int k) { return now[k]; }
    public static bool Tap(int k) { return now[k] && !prev[k]; }
    /// <summary>Осы кадрдағы басуды «жұтып» қою — астындағы экран оны екінші рет өңдемесін.</summary>
    public static void Eat(int k) { prev[k] = now[k]; }
    public static bool AnyTouchIn(Rect screenRect)
    {
        for (int i = 0; i < touchCount; i++) if (screenRect.Contains(touches[i])) return true;
        return false;
    }
}
