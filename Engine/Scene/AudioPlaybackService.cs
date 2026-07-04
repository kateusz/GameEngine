using Audio;
using ECS;
using Serilog;

namespace Engine.Scene;

internal sealed class AudioPlaybackService : IAudioPlayback
{
    private static readonly ILogger Logger = Log.ForContext<AudioPlaybackService>();
    private IAudioPlayback? _bound;

    internal void Bind(IAudioPlayback impl) => _bound = impl;

    internal void Unbind(IAudioPlayback impl)
    {
        if (_bound == impl)
            _bound = null;
    }

    public void Play(Entity entity)
    {
        if (_bound == null)
        {
            Logger.Warning("Cannot play audio for entity '{EntityName}' - no active AudioSystem", entity.Name);
            return;
        }

        _bound.Play(entity);
    }

    public void Pause(Entity entity)
    {
        if (_bound == null)
        {
            Logger.Warning("Cannot pause audio for entity '{EntityName}' - no active AudioSystem", entity.Name);
            return;
        }

        _bound.Pause(entity);
    }

    public void Stop(Entity entity)
    {
        if (_bound == null)
        {
            Logger.Warning("Cannot stop audio for entity '{EntityName}' - no active AudioSystem", entity.Name);
            return;
        }

        _bound.Stop(entity);
    }
}
