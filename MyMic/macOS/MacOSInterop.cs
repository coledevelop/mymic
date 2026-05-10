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

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern void objc_msgSend_bool(IntPtr receiver, IntPtr selector, [MarshalAs(UnmanagedType.U1)] bool arg1);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern ulong objc_msgSend_ulong(IntPtr receiver, IntPtr selector);

    private const long NSApplicationActivationPolicyRegular = 0;
    private const long NSApplicationActivationPolicyAccessory = 1;

    private static IntPtr SharedApp()
    {
        var nsAppClass = objc_getClass("NSApplication");
        if (nsAppClass == IntPtr.Zero) return IntPtr.Zero;
        var sharedSel = sel_registerName("sharedApplication");
        return objc_msgSend_get(nsAppClass, sharedSel);
    }

    private static void SetPolicy(long policy)
    {
        if (!OperatingSystem.IsMacOS()) return;
        var app = SharedApp();
        if (app == IntPtr.Zero) return;
        var sel = sel_registerName("setActivationPolicy:");
        objc_msgSend_long(app, sel, policy);
    }

    /// <summary>Hide the Dock icon and Cmd-Tab entry. Status bar icon stays.</summary>
    public static void HideFromDock() => SetPolicy(NSApplicationActivationPolicyAccessory);

    /// <summary>Show the Dock icon and Cmd-Tab entry.</summary>
    public static void ShowInDock() => SetPolicy(NSApplicationActivationPolicyRegular);

    private const ulong NSEventModifierFlagOption = 1UL << 19;

    /// <summary>True if the Option (Alt) key is currently held down.</summary>
    public static bool IsOptionKeyDown()
    {
        if (!OperatingSystem.IsMacOS()) return false;
        var nsEventClass = objc_getClass("NSEvent");
        if (nsEventClass == IntPtr.Zero) return false;
        var sel = sel_registerName("modifierFlags");
        var flags = objc_msgSend_ulong(nsEventClass, sel);
        return (flags & NSEventModifierFlagOption) != 0;
    }

    /// <summary>Bring the app to the foreground (e.g. after switching to Regular policy).</summary>
    public static void ActivateApp()
    {
        if (!OperatingSystem.IsMacOS()) return;
        var app = SharedApp();
        if (app == IntPtr.Zero) return;
        var sel = sel_registerName("activateIgnoringOtherApps:");
        objc_msgSend_bool(app, sel, true);
    }
}
