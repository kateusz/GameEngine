using System.Runtime.InteropServices;
using Engine.Audio;
using Serilog;
using Silk.NET.OpenAL;

namespace Engine.Platform.OpenAL.Effects;

internal sealed unsafe class OpenALLowPassEffect : IAudioEffect
{
    private static readonly ILogger Logger = Log.ForContext<OpenALLowPassEffect>();

    // EFX filter constants
    private const int AlFilterTypeLowpass = 0x0001;
    private const int AlFilterParamType = 0x8001;
    private const int AlFilterParamLowpassGain = 0x0001;
    private const int AlFilterParamLowpassGainHF = 0x0002;

    // Auxiliary effect slot (needed for IAudioEffect.SlotId, even though low-pass is a direct filter)
    private const int AlFilterNone = 0x0000;

    private delegate void AlGenFiltersDelegate(int n, uint* filters);
    private delegate void AlDeleteFiltersDelegate(int n, uint* filters);
    private delegate void AlFilteriDelegate(uint filter, int param, int value);
    private delegate void AlFilterfDelegate(uint filter, int param, float value);
    private delegate void AlGenAuxiliaryEffectSlotsDelegate(int n, uint* slots);
    private delegate void AlDeleteAuxiliaryEffectSlotsDelegate(int n, uint* slots);

    private readonly AlGenFiltersDelegate _genFilters;
    private readonly AlDeleteFiltersDelegate _deleteFilters;
    private readonly AlFilteriDelegate _filteri;
    private readonly AlFilterfDelegate _filterf;
    private readonly AlGenAuxiliaryEffectSlotsDelegate _genAuxSlots;
    private readonly AlDeleteAuxiliaryEffectSlotsDelegate _deleteAuxSlots;

    private uint _filterId;
    private uint _slotId;
    private bool _disposed;

    public AudioEffectType Type => AudioEffectType.LowPass;
    public uint SlotId => _slotId;
    public uint FilterId => _filterId;

    public OpenALLowPassEffect(AL al)
    {
        _genFilters = GetProc<AlGenFiltersDelegate>(al, "alGenFilters");
        _deleteFilters = GetProc<AlDeleteFiltersDelegate>(al, "alDeleteFilters");
        _filteri = GetProc<AlFilteriDelegate>(al, "alFilteri");
        _filterf = GetProc<AlFilterfDelegate>(al, "alFilterf");
        _genAuxSlots = GetProc<AlGenAuxiliaryEffectSlotsDelegate>(al, "alGenAuxiliaryEffectSlots");
        _deleteAuxSlots = GetProc<AlDeleteAuxiliaryEffectSlotsDelegate>(al, "alDeleteAuxiliaryEffectSlots");

        fixed (uint* filterPtr = &_filterId)
            _genFilters(1, filterPtr);

        _filteri(_filterId, AlFilterParamType, AlFilterTypeLowpass);

        // Low-pass uses direct filter on source, but create a slot for interface consistency
        fixed (uint* slotPtr = &_slotId)
            _genAuxSlots(1, slotPtr);

        Logger.Debug("Created low-pass filter {FilterId}", _filterId);
    }

    public void Apply(float amount)
    {
        // Map 0-1: 1.0 = no filtering, 0.0 = heavy filtering
        var gain = 1.0f - (amount * 0.9f);   // 1.0 to 0.1
        var gainHF = 1.0f - amount;           // 1.0 to 0.0

        _filterf(_filterId, AlFilterParamLowpassGain, gain);
        _filterf(_filterId, AlFilterParamLowpassGainHF, gainHF);
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

        if (_filterId != 0)
        {
            fixed (uint* filterPtr = &_filterId)
                _deleteFilters(1, filterPtr);
            _filterId = 0;
        }

        _disposed = true;
        Logger.Debug("Disposed low-pass filter");
    }

    private static T GetProc<T>(AL al, string name) where T : Delegate
    {
        var ptr = al.GetProcAddress(name);
        return Marshal.GetDelegateForFunctionPointer<T>(ptr);
    }
}
