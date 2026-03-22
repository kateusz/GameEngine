using Engine.Audio;
using Serilog;
using Silk.NET.OpenAL;

namespace Engine.Platform.OpenAL.Effects;

internal sealed class OpenALAudioEffectFactory(AL al) : IAudioEffectFactory
{
    private static readonly ILogger Logger = Log.ForContext<OpenALAudioEffectFactory>();

    // Detect EFX by probing for a known EFX function
    private readonly bool _efxAvailable = InitEfx(al);

    private static bool InitEfx(AL al)
    {
        var available = al.GetProcAddress("alGenEffects") != IntPtr.Zero;
        if (available)
            Logger.Information("OpenAL EFX extension available");
        else
            Logger.Warning("OpenAL EFX extension not available - audio effects disabled");
        return available;
    }

    public IAudioEffect CreateEffect(AudioEffectType type)
    {
        if (!_efxAvailable)
        {
            Logger.Debug("Creating no-op effect for {Type} (EFX unavailable)", type);
            return new NoOpAudioEffect(type);
        }

        try
        {
            return type switch
            {
                AudioEffectType.Reverb => new OpenALReverbEffect(al),
                AudioEffectType.LowPass => new OpenALLowPassEffect(al),
                AudioEffectType.Echo => new OpenALEchoEffect(al),
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown effect type")
            };
        }
        catch (InvalidOperationException ex)
        {
            Logger.Warning(ex, "Failed to create {Type} effect, falling back to no-op", type);
            return new NoOpAudioEffect(type);
        }
    }
}
