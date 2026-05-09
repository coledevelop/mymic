using System;
using System.Runtime.InteropServices;

namespace MyMic.macOS;

internal static class MacOSInterop
{
    private const string ObjC = "/usr/lib/libobjc.dylib";

    [DllImport(ObjC, EntryPoint = "objc_getClass", CharSet = CharSet.Ansi)]
    private static extern IntPtr objc_getClass(string name);

    [DllImport(ObjC, EntryPoint = "sel_registerName", CharSet = CharSet.Ansi)]
    private static extern IntPtr sel_registerName(string name);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern IntPtr objc_msgSend_get(IntPtr receiver, IntPtr selector);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern void objc_msgSend_long(IntPtr receiver, IntPtr selector, long arg1);

    // NSApplicationActivationPolicy values
    private const long NSApplicationActivationPolicyAccessory = 1;

    /// <summary>
    /// Hides the app from the macOS Dock and Cmd-Tab switcher. Status bar icon stays visible.
    /// Call after the app has launched (Cocoa is already initialized by Avalonia).
    /// </summary>
    public static void HideFromDock()
    {
        if (!OperatingSystem.IsMacOS()) return;

        var nsAppClass = objc_getClass("NSApplication");
        if (nsAppClass == IntPtr.Zero) return;

        var sharedAppSel = sel_registerName("sharedApplication");
        var app = objc_msgSend_get(nsAppClass, sharedAppSel);
        if (app == IntPtr.Zero) return;

        var setPolicySel = sel_registerName("setActivationPolicy:");
        objc_msgSend_long(app, setPolicySel, NSApplicationActivationPolicyAccessory);
    }
}
