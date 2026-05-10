using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MyMic.macOS;

public sealed class TrayMenuItem
{
    public string? Title { get; init; }
    public Action? Click { get; init; }
    public bool IsSeparator => string.IsNullOrEmpty(Title);
    public static TrayMenuItem Separator { get; } = new() { Title = null };
}

/// <summary>
/// Native macOS NSStatusItem wrapper. We bypass Avalonia's TrayIcon so we can:
/// - distinguish modifier-held clicks (Option+click) and respond without showing the menu
/// - retain explicit control over click routing on macOS
/// </summary>
public sealed class MacTrayIcon : IDisposable
{
    private static MacTrayIcon? _instance;
    private static IntPtr _trayTargetClass;

    private IntPtr _statusItem;
    private IntPtr _button;
    private IntPtr _target;
    private IntPtr _menu;
    private readonly List<TrayMenuItem?> _menuEntries = new();

    /// <summary>Fires when the user Option+clicks the tray icon (no menu shown).</summary>
    public Action? OnOptionClick;

    public MacTrayIcon()
    {
        if (_instance is not null)
            throw new InvalidOperationException("Only one MacTrayIcon supported");
        _instance = this;

        EnsureTargetClass();

        // [[NSStatusBar systemStatusBar] statusItemWithLength:NSVariableStatusItemLength]
        var statusBar = SendId(GetClass("NSStatusBar"), Sel("systemStatusBar"));
        _statusItem = SendIdD(statusBar, Sel("statusItemWithLength:"), -1.0);
        SendId(_statusItem, Sel("retain")); // we own it

        _button = SendId(_statusItem, Sel("button"));

        _target = SendId(_trayTargetClass, Sel("alloc"));
        _target = SendId(_target, Sel("init"));

        SendVoid1(_button, Sel("setTarget:"), _target);
        SendVoid1(_button, Sel("setAction:"), Sel("onClick:"));

        // Fire on left+right mouse down: bit 1 = LeftMouseDown, bit 3 = RightMouseDown.
        const ulong mask = (1UL << 1) | (1UL << 3);
        SendVoidUL(_button, Sel("sendActionOn:"), mask);
    }

    public void SetImage(string filePath)
    {
        var nsPath = MakeNSString(filePath);
        var image = SendId(GetClass("NSImage"), Sel("alloc"));
        image = SendId1(image, Sel("initWithContentsOfFile:"), nsPath);
        Release(nsPath);
        if (image == IntPtr.Zero) return;

        SendVoid_CGSize(image, Sel("setSize:"), new CGSize { Width = 18, Height = 18 });
        SendVoid1(_button, Sel("setImage:"), image);
        Release(image); // button retains it
    }

    public void SetTooltip(string text)
    {
        var ns = MakeNSString(text);
        SendVoid1(_button, Sel("setToolTip:"), ns);
        Release(ns);
    }

    public void SetMenu(IEnumerable<TrayMenuItem> items)
    {
        if (_menu != IntPtr.Zero) Release(_menu);
        _menuEntries.Clear();

        var menu = SendId(GetClass("NSMenu"), Sel("alloc"));
        menu = SendId(menu, Sel("init"));
        _menu = menu;

        var emptyKey = MakeNSString("");
        int tag = 0;
        foreach (var entry in items)
        {
            if (entry.IsSeparator)
            {
                var sep = SendId(GetClass("NSMenuItem"), Sel("separatorItem"));
                SendVoid1(menu, Sel("addItem:"), sep);
                _menuEntries.Add(null);
            }
            else
            {
                var title = MakeNSString(entry.Title ?? "");
                var menuItem = SendId(GetClass("NSMenuItem"), Sel("alloc"));
                menuItem = SendId3(menuItem, Sel("initWithTitle:action:keyEquivalent:"),
                                   title, Sel("onMenu:"), emptyKey);
                SendVoid1(menuItem, Sel("setTarget:"), _target);
                SendVoidL(menuItem, Sel("setTag:"), tag);
                SendVoid1(menu, Sel("addItem:"), menuItem);
                Release(title);
                Release(menuItem); // menu retains it
                _menuEntries.Add(entry);
            }
            tag++;
        }
        Release(emptyKey);
    }

    /// <summary>Update the title of the menu item at the given index.</summary>
    public void UpdateMenuItemTitle(int index, string newTitle)
    {
        if (_menu == IntPtr.Zero) return;
        if (index < 0 || index >= _menuEntries.Count) return;
        var item = SendIdL(_menu, Sel("itemAtIndex:"), index);
        if (item == IntPtr.Zero) return;
        var title = MakeNSString(newTitle);
        SendVoid1(item, Sel("setTitle:"), title);
        Release(title);
        if (_menuEntries[index] is { } entry)
        {
            _menuEntries[index] = new TrayMenuItem { Title = newTitle, Click = entry.Click };
        }
    }

    private void HandleClick()
    {
        // [NSApp currentEvent].modifierFlags
        var nsApp = SendId(GetClass("NSApplication"), Sel("sharedApplication"));
        var currentEvent = SendId(nsApp, Sel("currentEvent"));
        ulong flags = currentEvent != IntPtr.Zero ? SendUL(currentEvent, Sel("modifierFlags")) : 0;
        const ulong NSEventModifierFlagOption = 1UL << 19;
        bool optionHeld = (flags & NSEventModifierFlagOption) != 0;

        if (optionHeld)
        {
            try { OnOptionClick?.Invoke(); } catch { /* don't propagate into ObjC */ }
            return;
        }

        if (_menu != IntPtr.Zero)
        {
            // popUpStatusItemMenu: is technically deprecated since 10.10 but still works
            // and gives correct positioning beneath the status bar item.
            SendVoid1(_statusItem, Sel("popUpStatusItemMenu:"), _menu);
        }
    }

    private void HandleMenuClick(int tag)
    {
        if (tag < 0 || tag >= _menuEntries.Count) return;
        var entry = _menuEntries[tag];
        try { entry?.Click?.Invoke(); } catch { /* don't propagate into ObjC */ }
    }

    public void Dispose()
    {
        if (_statusItem != IntPtr.Zero)
        {
            var statusBar = SendId(GetClass("NSStatusBar"), Sel("systemStatusBar"));
            SendVoid1(statusBar, Sel("removeStatusItem:"), _statusItem);
            Release(_statusItem);
            _statusItem = IntPtr.Zero;
        }
        if (_menu != IntPtr.Zero) { Release(_menu); _menu = IntPtr.Zero; }
        if (_target != IntPtr.Zero) { Release(_target); _target = IntPtr.Zero; }
        if (ReferenceEquals(_instance, this)) _instance = null;
    }

    // --- ObjC class registration ---

    private static unsafe void EnsureTargetClass()
    {
        if (_trayTargetClass != IntPtr.Zero) return;

        var existing = GetClass("MyMicTrayTarget");
        if (existing != IntPtr.Zero) { _trayTargetClass = existing; return; }

        var nsObject = GetClass("NSObject");
        var cls = objc_allocateClassPair(nsObject, "MyMicTrayTarget", IntPtr.Zero);
        if (cls == IntPtr.Zero) return;

        delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, void> click = &ClickThunk;
        delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, void> menu = &MenuThunk;
        class_addMethod(cls, Sel("onClick:"), (IntPtr)click, "v@:@");
        class_addMethod(cls, Sel("onMenu:"), (IntPtr)menu, "v@:@");

        objc_registerClassPair(cls);
        _trayTargetClass = cls;
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void ClickThunk(IntPtr self, IntPtr cmd, IntPtr sender)
    {
        try { _instance?.HandleClick(); } catch { /* must not throw across native */ }
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void MenuThunk(IntPtr self, IntPtr cmd, IntPtr sender)
    {
        try
        {
            if (_instance is null) return;
            var tag = SendL(sender, Sel("tag"));
            _instance.HandleMenuClick((int)tag);
        }
        catch { /* must not throw across native */ }
    }

    // --- ObjC P/Invoke ---

    private const string ObjC = "/usr/lib/libobjc.dylib";

    [DllImport(ObjC, EntryPoint = "objc_getClass", CharSet = CharSet.Ansi)]
    private static extern IntPtr objc_getClass(string name);

    [DllImport(ObjC, EntryPoint = "sel_registerName", CharSet = CharSet.Ansi)]
    private static extern IntPtr sel_registerName(string name);

    [DllImport(ObjC, EntryPoint = "objc_allocateClassPair")]
    private static extern IntPtr objc_allocateClassPair(IntPtr superclass,
        [MarshalAs(UnmanagedType.LPStr)] string name, IntPtr extraBytes);

    [DllImport(ObjC, EntryPoint = "objc_registerClassPair")]
    private static extern void objc_registerClassPair(IntPtr cls);

    [DllImport(ObjC, EntryPoint = "class_addMethod")]
    [return: MarshalAs(UnmanagedType.U1)]
    private static extern bool class_addMethod(IntPtr cls, IntPtr name, IntPtr imp,
        [MarshalAs(UnmanagedType.LPStr)] string types);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern IntPtr SendId_Native(IntPtr receiver, IntPtr selector);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern IntPtr SendId1_Native(IntPtr receiver, IntPtr selector, IntPtr arg1);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern IntPtr SendIdL_Native(IntPtr receiver, IntPtr selector, long arg1);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern IntPtr SendId3_Native(IntPtr receiver, IntPtr selector,
        IntPtr a1, IntPtr a2, IntPtr a3);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern IntPtr SendIdD_Native(IntPtr receiver, IntPtr selector, double arg1);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern void SendVoid1_Native(IntPtr receiver, IntPtr selector, IntPtr arg1);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern void SendVoidL_Native(IntPtr receiver, IntPtr selector, long arg1);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern void SendVoidUL_Native(IntPtr receiver, IntPtr selector, ulong arg1);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern ulong SendUL_Native(IntPtr receiver, IntPtr selector);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern long SendL_Native(IntPtr receiver, IntPtr selector);

    [DllImport(ObjC, EntryPoint = "objc_msgSend", CharSet = CharSet.Ansi)]
    private static extern IntPtr SendIdStr_Native(IntPtr receiver, IntPtr selector,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string arg);

    [StructLayout(LayoutKind.Sequential)]
    private struct CGSize { public double Width; public double Height; }

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern void SendVoid_CGSize_Native(IntPtr receiver, IntPtr selector, CGSize arg);

    // --- helpers ---

    private static IntPtr GetClass(string name) => objc_getClass(name);
    private static IntPtr Sel(string name) => sel_registerName(name);

    private static IntPtr SendId(IntPtr r, IntPtr s) => SendId_Native(r, s);
    private static IntPtr SendId1(IntPtr r, IntPtr s, IntPtr a) => SendId1_Native(r, s, a);
    private static IntPtr SendIdL(IntPtr r, IntPtr s, long a) => SendIdL_Native(r, s, a);
    private static IntPtr SendId3(IntPtr r, IntPtr s, IntPtr a, IntPtr b, IntPtr c) => SendId3_Native(r, s, a, b, c);
    private static IntPtr SendIdD(IntPtr r, IntPtr s, double a) => SendIdD_Native(r, s, a);
    private static void SendVoid1(IntPtr r, IntPtr s, IntPtr a) => SendVoid1_Native(r, s, a);
    private static void SendVoidL(IntPtr r, IntPtr s, long a) => SendVoidL_Native(r, s, a);
    private static void SendVoidUL(IntPtr r, IntPtr s, ulong a) => SendVoidUL_Native(r, s, a);
    private static void SendVoid_CGSize(IntPtr r, IntPtr s, CGSize a) => SendVoid_CGSize_Native(r, s, a);
    private static ulong SendUL(IntPtr r, IntPtr s) => SendUL_Native(r, s);
    private static long SendL(IntPtr r, IntPtr s) => SendL_Native(r, s);

    private static IntPtr MakeNSString(string text)
    {
        // Use alloc/initWithUTF8String: so we own the +1 retain count and can release.
        var s = SendId(GetClass("NSString"), Sel("alloc"));
        return SendIdStr_Native(s, Sel("initWithUTF8String:"), text);
    }

    private static void Release(IntPtr obj)
    {
        if (obj == IntPtr.Zero) return;
        SendId(obj, Sel("release")); // release returns void; signature mismatch is harmless on AArch64
    }
}
