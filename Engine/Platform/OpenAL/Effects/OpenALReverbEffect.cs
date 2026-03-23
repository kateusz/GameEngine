using System.Runtime.InteropServices;
using Engine.Audio;
using Serilog;
using Silk.NET.OpenAL;

namespace Engine.Platform.OpenAL.Effects;

internal sealed unsafe class OpenALReverbEffect : IAudioEffect
{
    private static readonly ILogger Logger = Log.ForContext<OpenALReverbEffect>();

    // EFX constants
    private const int AlEffectTypeReverb = 0x0001;
    private const int AlEffectTypeEaxReverb = 0x8000;
    private const int AlEffectParamReverbDensity = 0x0001;
    private const int AlEffectParamReverbDiffusion = 0x0002;
    private const int AlEffectParamReverbGain = 0x0003;
    private const int AlEffectParamReverbDecayTime = 0x0005;      // standard reverb
    private const int AlEffectParamEaxReverbDecayTime = 0x0006;   // EAX reverb
    private const int AlEffectParamType = 0x8001;
    private const int AlEffectSlotParamEffect = 0x0001;

    private delegate void AlGenEffectsDelegate(int n, uint* effects);

    private delegate void AlDeleteEffectsDelegate(int n, uint* effects);

    private delegate void AlEffectiDelegate(uint effect, int param, int value);

    private delegate void AlEffectfDelegate(uint effect, int param, float value);

    private delegate void AlGenAuxiliaryEffectSlotsDelegate(int n, uint* slots);

    private delegate void AlDeleteAuxiliaryEffectSlotsDelegate(int n, uint* slots);

    private delegate void AlAuxiliaryEffectSlotiDelegate(uint slot, int param, int value);

    private readonly AL _al;
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
    private int _decayTimeParam;

    public AudioEffectType Type => AudioEffectType.Reverb;
    public uint SlotId => _slotId;

    public OpenALReverbEffect(AL al)
    {
        _al = al;
        _genEffects = GetProc<AlGenEffectsDelegate>(al, "alGenEffects");
        _deleteEffects = GetProc<AlDeleteEffectsDelegate>(al, "alDeleteEffects");
        _effecti = GetProc<AlEffectiDelegate>(al, "alEffecti");
        _effectf = GetProc<AlEffectfDelegate>(al, "alEffectf");
        _genAuxSlots = GetProc<AlGenAuxiliaryEffectSlotsDelegate>(al, "alGenAuxiliaryEffectSlots");
        _deleteAuxSlots = GetProc<AlDeleteAuxiliaryEffectSlotsDelegate>(al, "alDeleteAuxiliaryEffectSlots");
        _auxSloti = GetProc<AlAuxiliaryEffectSlotiDelegate>(al, "alAuxiliaryEffectSloti");

        try
        {
            // Clear any stale OpenAL errors before EFX operations
            _al.GetError();

            fixed (uint* idPtr = &_effectId)
                _genEffects(1, idPtr);

            var genErr = _al.GetError();
            if (genErr != AudioError.NoError)
                throw new InvalidOperationException($"alGenEffects failed: {genErr}");

            // Try standard reverb first; fall back to EAX reverb (required on macOS Apple OpenAL)
            _effecti(_effectId, AlEffectParamType, AlEffectTypeReverb);
            if (_al.GetError() != AudioError.NoError)
            {
                _effecti(_effectId, AlEffectParamType, AlEffectTypeEaxReverb);
                var fallbackErr = _al.GetError();
                if (fallbackErr != AudioError.NoError)
                    throw new InvalidOperationException($"Neither AL_EFFECT_REVERB nor AL_EFFECT_EAXREVERB supported: {fallbackErr}");
                _decayTimeParam = AlEffectParamEaxReverbDecayTime;
                Logger.Debug("Using EAX reverb (standard reverb unsupported)");
            }
            else
            {
                _decayTimeParam = AlEffectParamReverbDecayTime;
            }

            fixed (uint* slotPtr = &_slotId)
                _genAuxSlots(1, slotPtr);

            var slotErr = _al.GetError();
            if (slotErr != AudioError.NoError)
                throw new InvalidOperationException($"alGenAuxiliaryEffectSlots failed: {slotErr}");

            _auxSloti(_slotId, AlEffectSlotParamEffect, (int)_effectId);

            Logger.Debug("Created reverb effect {EffectId} with slot {SlotId}", _effectId, _slotId);
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
            throw new ObjectDisposedException(nameof(OpenALReverbEffect));

        var decayTime = MathF.Max(AudioEffectConstants.ReverbMinDecayTime, amount * AudioEffectConstants.ReverbMaxDecayTime);
        var density = amount;                              // 0 to 1
        var diffusion = AudioEffectConstants.ReverbBaseDiffusion + (amount * AudioEffectConstants.ReverbDiffusionRange);
        var gain = AudioEffectConstants.ReverbBaseGain + (amount * AudioEffectConstants.ReverbGainRange);

        _effectf(_effectId, _decayTimeParam, decayTime);
        _effectf(_effectId, AlEffectParamReverbDensity, density);
        _effectf(_effectId, AlEffectParamReverbDiffusion, diffusion);
        _effectf(_effectId, AlEffectParamReverbGain, gain);
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
        Logger.Debug("Disposed reverb effect");
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