using Engine.Audio;
using Serilog;
using Silk.NET.OpenAL;

namespace Engine.Platform.OpenAL.Effects;

internal sealed class OpenALAudioEffectFactory : IAudioEffectFactory
{
    private static readonly ILogger Logger = Log.ForContext<OpenALAudioEffectFactory>();

    private readonly AL _al;
    private readonly bool _efxAvailable;

    public OpenALAudioEffectFactory(AL al)
    {
        _al = al;

        // Detect EFX by probing for a known EFX function
        _efxAvailable = al.GetProcAddress("alGenEffects") != IntPtr.Zero;

        if (_efxAvailable)
            Logger.Information("OpenAL EFX extension available");
        else
            Logger.Warning("OpenAL EFX extension not available - audio effects disabled");
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
                AudioEffectType.Reverb => new OpenALReverbEffect(_al),
                AudioEffectType.LowPass => new OpenALLowPassEffect(_al),
                AudioEffectType.Echo => new OpenALEchoEffect(_al),
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
