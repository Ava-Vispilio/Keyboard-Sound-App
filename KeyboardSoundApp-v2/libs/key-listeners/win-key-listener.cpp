// Keyboard Sound App – Windows key listener (simplified: every keydown → one JSON line).
// Build (MinGW): g++ -o keyboard-sound-listener.exe win-key-listener.cpp -mwindows -static
// Build (MSVC): cl /Fe:keyboard-sound-listener.exe win-key-listener.cpp user32.lib

#include <windows.h>
#include <iostream>
#include <cstdio>

static HHOOK g_hook = NULL;

static LRESULT CALLBACK LowLevelKeyboardProc(int nCode, WPARAM wParam, LPARAM lParam) {
    if (nCode >= 0 && (wParam == WM_KEYDOWN || wParam == WM_SYSKEYDOWN)) {
        KBDLLHOOKSTRUCT* p = (KBDLLHOOKSTRUCT*)lParam;
        printf("{\"keydown\": %u}\n", (unsigned)p->vkCode);
        fflush(stdout);
    }
    return CallNextHookEx(g_hook, nCode, wParam, lParam);
}

int main() {
    g_hook = SetWindowsHookEx(WH_KEYBOARD_LL, LowLevelKeyboardProc, GetModuleHandle(NULL), 0);
    if (!g_hook) {
        fprintf(stderr, "SetWindowsHookEx failed\n");
        return 1;
    }

    MSG msg;
    while (GetMessage(&msg, NULL, 0, 0) > 0) {
        TranslateMessage(&msg);
        DispatchMessage(&msg);
    }

    UnhookWindowsHookEx(g_hook);
    return 0;
}
