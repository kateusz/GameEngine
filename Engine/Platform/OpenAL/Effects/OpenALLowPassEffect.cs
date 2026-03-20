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

    private delegate void AlGenFiltersDelegate(int n, uint* filters);
    private delegate void AlDeleteFiltersDelegate(int n, uint* filters);
    private delegate void AlFilteriDelegate(uint filter, int param, int value);
    private delegate void AlFilterfDelegate(uint filter, int param, float value);

    private readonly AlGenFiltersDelegate _genFilters;
    private readonly AlDeleteFiltersDelegate _deleteFilters;
    private readonly AlFilteriDelegate _filteri;
    private readonly AlFilterfDelegate _filterf;

    private uint _filterId;
    private bool _disposed;

    public AudioEffectType Type => AudioEffectType.LowPass;
    // Low-pass uses direct source filter (AL_DIRECT_FILTER); no aux effect slot needed
    public uint SlotId => 0;
    public uint FilterId => _filterId;

    public OpenALLowPassEffect(AL al)
    {
        _genFilters = GetProc<AlGenFiltersDelegate>(al, "alGenFilters");
        _deleteFilters = GetProc<AlDeleteFiltersDelegate>(al, "alDeleteFilters");
        _filteri = GetProc<AlFilteriDelegate>(al, "alFilteri");
        _filterf = GetProc<AlFilterfDelegate>(al, "alFilterf");

        try
        {
            fixed (uint* filterPtr = &_filterId)
                _genFilters(1, filterPtr);

            _filteri(_filterId, AlFilterParamType, AlFilterTypeLowpass);

            Logger.Debug("Created low-pass filter {FilterId}", _filterId);
        }
        catch
        {
            Dispose();
            throw;
        }
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

        if (_filterId != 0)
        {
            fixed (uint* filterPtr = &_filterId)
                _deleteFilters(1, filterPtr);
            _filterId = 0;
        }

        _disposed = true;
        Logger.Debug("Disposed low-pass filter");
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
