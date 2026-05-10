using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MyMic.macOS;

/// <summary>
/// System-wide keyboard hotkey via Carbon's RegisterEventHotKey. Works without
/// Accessibility permission. Single-instance (only one hotkey registered at a
/// time, which is all MyMic needs).
/// </summary>
public sealed class GlobalHotkey : IDisposable
{
    private static GlobalHotkey? _instance;

    /// <summary>Fires (on the main thread) when the registered hotkey is pressed.</summary>
    public Action? Pressed;

    private IntPtr _hotKeyRef;
    private IntPtr _eventHandlerRef;
    private uint _nextId = 1;

    public GlobalHotkey()
    {
        if (_instance is not null)
            throw new InvalidOperationException("Only one GlobalHotkey supported");
        _instance = this;
        InstallHandler();
    }

    /// <summary>Register a hotkey. Returns true if the registration succeeded.</summary>
    public bool TrySet(uint macKeyCode, uint macModifiers)
    {
        Unset();

        var id = new EventHotKeyID { Signature = 0x4D794D69, Id = _nextId++ }; // 'MyMi'
        var target = GetApplicationEventTarget();
        var status = RegisterEventHotKey(macKeyCode, macModifiers, id, target, 0, out _hotKeyRef);
        if (status != 0) _hotKeyRef = IntPtr.Zero;
        return status == 0;
    }

    public void Unset()
    {
        if (_hotKeyRef != IntPtr.Zero)
        {
            UnregisterEventHotKey(_hotKeyRef);
            _hotKeyRef = IntPtr.Zero;
        }
    }

    private unsafe void InstallHandler()
    {
        var spec = new EventTypeSpec
        {
            EventClass = 0x6B657962, // 'keyb' kEventClassKeyboard
            EventKind = 5,           // kEventHotKeyPressed
        };

        delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, int> thunk = &HandlerThunk;
        InstallEventHandler(
            GetApplicationEventTarget(),
            (IntPtr)thunk,
            1,
            new[] { spec },
            IntPtr.Zero,
            out _eventHandlerRef);
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static int HandlerThunk(IntPtr nextHandler, IntPtr theEvent, IntPtr userData)
    {
        try { _instance?.Pressed?.Invoke(); } catch { /* must not throw across native */ }
        return 0; // noErr
    }

    public void Dispose()
    {
        Unset();
        if (_eventHandlerRef != IntPtr.Zero)
        {
            RemoveEventHandler(_eventHandlerRef);
            _eventHandlerRef = IntPtr.Zero;
        }
        if (ReferenceEquals(_instance, this)) _instance = null;
    }

    private const string Carbon = "/System/Library/Frameworks/Carbon.framework/Carbon";

    [DllImport(Carbon)]
    private static extern IntPtr GetApplicationEventTarget();

    [DllImport(Carbon)]
    private static extern int RegisterEventHotKey(
        uint inHotKeyCode,
        uint inHotKeyModifiers,
        EventHotKeyID inHotKeyID,
        IntPtr inTarget,
        uint inOptions,
        out IntPtr outRef);

    [DllImport(Carbon)]
    private static extern int UnregisterEventHotKey(IntPtr hotKeyRef);

    [DllImport(Carbon)]
    private static extern int InstallEventHandler(
        IntPtr inTarget,
        IntPtr inHandler,
        uint inNumTypes,
        [In] EventTypeSpec[] inList,
        IntPtr inUserData,
        out IntPtr outRef);

    [DllImport(Carbon)]
    private static extern int RemoveEventHandler(IntPtr eventHandlerRef);

    [StructLayout(LayoutKind.Sequential)]
    private struct EventHotKeyID
    {
        public uint Signature;
        public uint Id;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct EventTypeSpec
    {
        public uint EventClass;
        public uint EventKind;
    }
}
