using System;
using System.Runtime.InteropServices;

namespace MyMic.Audio;

public sealed class MicMuteService
{
    public event EventHandler<bool>? MuteChanged;

    public bool IsMuted { get; private set; }

    public void Toggle() => SetMuted(!IsMuted);

    public void SetMuted(bool muted)
    {
        var deviceId = CoreAudio.GetDefaultInputDevice();
        if (deviceId == 0) return;

        var ok = CoreAudio.TrySetMute(deviceId, muted)
                 || CoreAudio.TrySetVolumeMute(deviceId, muted);

        if (!ok) return;

        if (IsMuted == muted) return;
        IsMuted = muted;
        MuteChanged?.Invoke(this, muted);
    }
}

internal static class CoreAudio
{
    private const string Lib = "/System/Library/Frameworks/CoreAudio.framework/CoreAudio";

    private const uint kAudioObjectSystemObject = 1;
    private const uint kAudioHardwarePropertyDefaultInputDevice = 0x64496e20; // 'dIn '
    private const uint kAudioObjectPropertyScopeGlobal = 0x676c6f62;          // 'glob'
    private const uint kAudioObjectPropertyScopeInput = 0x696e7074;           // 'inpt'
    private const uint kAudioObjectPropertyElementMain = 0;
    private const uint kAudioDevicePropertyMute = 0x6d757465;                 // 'mute'
    private const uint kAudioDevicePropertyVolumeScalar = 0x766f6c6d;         // 'volm'

    [StructLayout(LayoutKind.Sequential)]
    private struct AudioObjectPropertyAddress
    {
        public uint Selector;
        public uint Scope;
        public uint Element;
    }

    [DllImport(Lib)]
    private static extern int AudioObjectGetPropertyData(
        uint objectId,
        ref AudioObjectPropertyAddress address,
        uint qualifierDataSize,
        IntPtr qualifierData,
        ref uint dataSize,
        IntPtr data);

    [DllImport(Lib)]
    private static extern int AudioObjectSetPropertyData(
        uint objectId,
        ref AudioObjectPropertyAddress address,
        uint qualifierDataSize,
        IntPtr qualifierData,
        uint dataSize,
        IntPtr data);

    [DllImport(Lib)]
    [return: MarshalAs(UnmanagedType.U1)]
    private static extern bool AudioObjectHasProperty(
        uint objectId,
        ref AudioObjectPropertyAddress address);

    [DllImport(Lib)]
    private static extern int AudioObjectIsPropertySettable(
        uint objectId,
        ref AudioObjectPropertyAddress address,
        [MarshalAs(UnmanagedType.U1)] out bool settable);

    public static uint GetDefaultInputDevice()
    {
        var address = new AudioObjectPropertyAddress
        {
            Selector = kAudioHardwarePropertyDefaultInputDevice,
            Scope = kAudioObjectPropertyScopeGlobal,
            Element = kAudioObjectPropertyElementMain,
        };
        uint deviceId = 0;
        uint size = sizeof(uint);
        unsafe
        {
            var status = AudioObjectGetPropertyData(
                kAudioObjectSystemObject, ref address, 0, IntPtr.Zero, ref size, (IntPtr)(&deviceId));
            return status == 0 ? deviceId : 0u;
        }
    }

    public static bool TrySetMute(uint deviceId, bool muted)
    {
        var address = new AudioObjectPropertyAddress
        {
            Selector = kAudioDevicePropertyMute,
            Scope = kAudioObjectPropertyScopeInput,
            Element = kAudioObjectPropertyElementMain,
        };

        if (!AudioObjectHasProperty(deviceId, ref address)) return false;
        if (AudioObjectIsPropertySettable(deviceId, ref address, out var settable) != 0 || !settable)
            return false;

        uint value = muted ? 1u : 0u;
        uint size = sizeof(uint);
        unsafe
        {
            var status = AudioObjectSetPropertyData(
                deviceId, ref address, 0, IntPtr.Zero, size, (IntPtr)(&value));
            return status == 0;
        }
    }

    // Fallback for devices that don't expose kAudioDevicePropertyMute (e.g. some USB mics).
    // Saves the pre-mute volume in a static so unmute can restore it within this process lifetime.
    private static float _savedVolume = 1.0f;

    public static bool TrySetVolumeMute(uint deviceId, bool muted)
    {
        var anySet = false;
        for (uint element = 0; element <= 8; element++)
        {
            var address = new AudioObjectPropertyAddress
            {
                Selector = kAudioDevicePropertyVolumeScalar,
                Scope = kAudioObjectPropertyScopeInput,
                Element = element,
            };
            if (!AudioObjectHasProperty(deviceId, ref address)) continue;
            if (AudioObjectIsPropertySettable(deviceId, ref address, out var settable) != 0 || !settable) continue;

            uint size = sizeof(float);
            float current = 1.0f;
            unsafe
            {
                AudioObjectGetPropertyData(deviceId, ref address, 0, IntPtr.Zero, ref size, (IntPtr)(&current));
            }

            if (muted)
            {
                if (current > 0f) _savedVolume = current;
                float zero = 0f;
                unsafe
                {
                    var status = AudioObjectSetPropertyData(deviceId, ref address, 0, IntPtr.Zero, sizeof(float), (IntPtr)(&zero));
                    if (status == 0) anySet = true;
                }
            }
            else
            {
                float restore = _savedVolume > 0f ? _savedVolume : 1.0f;
                unsafe
                {
                    var status = AudioObjectSetPropertyData(deviceId, ref address, 0, IntPtr.Zero, sizeof(float), (IntPtr)(&restore));
                    if (status == 0) anySet = true;
                }
            }
        }
        return anySet;
    }
}
