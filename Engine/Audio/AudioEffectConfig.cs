namespace Engine.Audio;

public class AudioEffectConfig
{
    public AudioEffectType Type { get; set; }
    public bool Enabled { get; set; } = true;
    public float Amount { get; set; } = 0.5f;
}
