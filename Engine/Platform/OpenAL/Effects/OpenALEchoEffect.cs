using System.Runtime.InteropServices;
using Engine.Audio;
using Serilog;
using Silk.NET.OpenAL;

namespace Engine.Platform.OpenAL.Effects;

internal sealed unsafe class OpenALEchoEffect : IAudioEffect
{
    private static readonly ILogger Logger = Log.ForContext<OpenALEchoEffect>();

    private const int AlEffectTypeEcho = 0x0004;
    private const int AlEffectParamType = 0x8001;
    private const int AlEffectParamEchoDelay = 0x0001;
    private const int AlEffectParamEchoDamping = 0x0003;
    private const int AlEffectParamEchoFeedback = 0x0004;
    private const int AlEffectSlotParamEffect = 0x0001;

    private delegate void AlGenEffectsDelegate(int n, uint* effects);

    private delegate void AlDeleteEffectsDelegate(int n, uint* effects);

    private delegate void AlEffectiDelegate(uint effect, int param, int value);

    private delegate void AlEffectfDelegate(uint effect, int param, float value);

    private delegate void AlGenAuxiliaryEffectSlotsDelegate(int n, uint* slots);

    private delegate void AlDeleteAuxiliaryEffectSlotsDelegate(int n, uint* slots);

    private delegate void AlAuxiliaryEffectSlotiDelegate(uint slot, int param, int value);

    private readonly AlGenEffectsDelegate _genEffects;
    private readonly AlDeleteEffectsDelegate _deleteEffects;
    private readonly AlEffectiDelegate _effecti;
    private readonly AlEffectfDelegate _effectf;
    private readonly AlGenAuxiliaryEffectSlotsDelegate _genAuxSlots;
    private readonly AlDeleteAuxiliaryEffectSlotsDelegate _deleteAuxSlots;
    private readonly AlAuxiliaryEffectSlotiDelegate _auxSloti;

    private uint _effectId;
    private uint _slotId;
    private bool _disposed;

    public AudioEffectType Type => AudioEffectType.Echo;
    public uint SlotId => _slotId;

    public OpenALEchoEffect(AL al)
    {
        _genEffects = GetProc<AlGenEffectsDelegate>(al, "alGenEffects");
        _deleteEffects = GetProc<AlDeleteEffectsDelegate>(al, "alDeleteEffects");
        _effecti = GetProc<AlEffectiDelegate>(al, "alEffecti");
        _effectf = GetProc<AlEffectfDelegate>(al, "alEffectf");
        _genAuxSlots = GetProc<AlGenAuxiliaryEffectSlotsDelegate>(al, "alGenAuxiliaryEffectSlots");
        _deleteAuxSlots = GetProc<AlDeleteAuxiliaryEffectSlotsDelegate>(al, "alDeleteAuxiliaryEffectSlots");
        _auxSloti = GetProc<AlAuxiliaryEffectSlotiDelegate>(al, "alAuxiliaryEffectSloti");

        try
        {
            fixed (uint* idPtr = &_effectId)
                _genEffects(1, idPtr);

            _effecti(_effectId, AlEffectParamType, AlEffectTypeEcho);

            fixed (uint* slotPtr = &_slotId)
                _genAuxSlots(1, slotPtr);

            _auxSloti(_slotId, AlEffectSlotParamEffect, (int)_effectId);

            Logger.Debug("Created echo effect {EffectId} with slot {SlotId}", _effectId, _slotId);
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public void Apply(float amount)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(OpenALEchoEffect));

        var delay = 0.05f + (amount * 0.2f); // 50ms to 250ms
        var feedback = amount * 0.5f; // 0 to 0.5
        const float damping = 0.5f; // Fixed middle value

        _effectf(_effectId, AlEffectParamEchoDelay, delay);
        _effectf(_effectId, AlEffectParamEchoFeedback, feedback);
        _effectf(_effectId, AlEffectParamEchoDamping, damping);
        _auxSloti(_slotId, AlEffectSlotParamEffect, (int)_effectId);
    }

    public void Dispose()
    {
        if (_disposed) return;

        if (_slotId != 0)
        {
            fixed (uint* slotPtr = &_slotId)
                _deleteAuxSlots(1, slotPtr);
            _slotId = 0;
        }

        if (_effectId != 0)
        {
            fixed (uint* idPtr = &_effectId)
                _deleteEffects(1, idPtr);
            _effectId = 0;
        }

        _disposed = true;
        Logger.Debug("Disposed echo effect");
        GC.SuppressFinalize(this);
    }

    private static T GetProc<T>(AL al, string name) where T : Delegate
    {
        var ptr = al.GetProcAddress(name);
        if (ptr == IntPtr.Zero)
            throw new InvalidOperationException($"OpenAL EFX function '{name}' not available.");
        return Marshal.GetDelegateForFunctionPointer<T>(ptr);
    }
}