using System.Text.Json.Serialization;
using ECS;

namespace Hounds.Marches;

[SerializableComponent]
public sealed class MarchGameComponent : IGameComponent
{
    public bool Started { get; set; }
    public bool Built { get; set; }
    public string Art { get; set; } = "original";
    public int Stingers { get; set; }
    public int BloodAmount { get; set; }
    public float BloodX { get; set; }
    public float BloodY { get; set; }
    public bool BloodActive { get; set; }
    public float Hitstop { get; set; }
    public float Fade { get; set; } = 1f;
    public float DeathTimer { get; set; } = -1f;
    public bool Won { get; set; }
    public string BannerKey { get; set; } = "";
    public float BannerTimer { get; set; }
    public string ProximityKey { get; set; } = "";
    public float HelpTimer { get; set; } = 6f;
    public int ActiveCheck { get; set; }
    public float RespawnX { get; set; }
    public float RespawnY { get; set; }
    public string LastRoom { get; set; } = "";
    public float KillY { get; set; } = -3f;

    [JsonIgnore] public List<RoomInfo> Rooms { get; set; } = [];
    [JsonIgnore] public AssetPack? Pack { get; set; }

    public IComponent Clone() => new MarchGameComponent
    {
        Started = Started,
        Built = Built,
        Art = Art,
        Stingers = Stingers,
        BloodAmount = BloodAmount,
        BloodX = BloodX,
        BloodY = BloodY,
        BloodActive = BloodActive,
        Hitstop = Hitstop,
        Fade = Fade,
        DeathTimer = DeathTimer,
        Won = Won,
        BannerKey = BannerKey,
        BannerTimer = BannerTimer,
        ProximityKey = ProximityKey,
        HelpTimer = HelpTimer,
        ActiveCheck = ActiveCheck,
        RespawnX = RespawnX,
        RespawnY = RespawnY,
        LastRoom = LastRoom,
        KillY = KillY
    };
}

public sealed class RoomInfo
{
    public string Id { get; set; } = "";
    public float MinX { get; set; }
    public float MaxX { get; set; }
    public float CamMin { get; set; }
    public float CamMax { get; set; }
    public float CamY { get; set; }
    public string Title { get; set; } = "";
}

[SerializableComponent]
public sealed class ActorComponent : IGameComponent
{
    public string Kind { get; set; } = "player";
    public int Team { get; set; }
    public int Facing { get; set; } = 1;
    public float HalfW { get; set; } = 0.32f;
    public float HalfH { get; set; } = 0.78f;
    public float SpawnX { get; set; }
    public float SpawnY { get; set; }
    public int VisualId { get; set; }
    public float Coyote { get; set; }
    public float JumpBuffer { get; set; }
    public bool Grounded { get; set; }
    public bool OnLadder { get; set; }
    public bool InPuddle { get; set; }
    public float KnockVx { get; set; }
    public float Flash { get; set; }
    public int StingerValue { get; set; }
    public bool Rewarded { get; set; }
    public string Sheet { get; set; } = "player";

    public IComponent Clone() => new ActorComponent
    {
        Kind = Kind, Team = Team, Facing = Facing, HalfW = HalfW, HalfH = HalfH,
        SpawnX = SpawnX, SpawnY = SpawnY, VisualId = VisualId, Coyote = Coyote,
        JumpBuffer = JumpBuffer, Grounded = Grounded, OnLadder = OnLadder, InPuddle = InPuddle,
        KnockVx = KnockVx, Flash = Flash, StingerValue = StingerValue, Rewarded = Rewarded, Sheet = Sheet
    };
}

[SerializableComponent]
public sealed class HealthComponent : IGameComponent
{
    public float Current { get; set; }
    public float Max { get; set; }
    public float Poise { get; set; }
    public float PoiseMax { get; set; }
    public float PoiseDelay { get; set; }

    public IComponent Clone() => new HealthComponent
    {
        Current = Current, Max = Max, Poise = Poise, PoiseMax = PoiseMax, PoiseDelay = PoiseDelay
    };
}

[SerializableComponent]
public sealed class StaminaComponent : IGameComponent
{
    public float Current { get; set; } = 100f;
    public float Max { get; set; } = 100f;
    public float RegenDelay { get; set; }

    public IComponent Clone() => new StaminaComponent
    {
        Current = Current, Max = Max, RegenDelay = RegenDelay
    };
}

[SerializableComponent]
public sealed class CombatComponent : IGameComponent
{
    public string Action { get; set; } = "none";
    public int Frame { get; set; }
    public bool HitDone { get; set; }
    public bool Phase2 { get; set; }
    public float Cooldown { get; set; }
    public float Invuln { get; set; }
    public int Pattern { get; set; }

    public IComponent Clone() => new CombatComponent
    {
        Action = Action, Frame = Frame, HitDone = HitDone, Phase2 = Phase2,
        Cooldown = Cooldown, Invuln = Invuln, Pattern = Pattern
    };
}

[SerializableComponent]
public sealed class AIBrainComponent : IGameComponent
{
    public string State { get; set; } = "patrol";
    public float Left { get; set; }
    public float Right { get; set; }
    public int Dir { get; set; } = 1;
    public float Aggro { get; set; } = 7f;
    public float Leash { get; set; } = 14f;

    public IComponent Clone() => new AIBrainComponent
    {
        State = State, Left = Left, Right = Right, Dir = Dir, Aggro = Aggro, Leash = Leash
    };
}

[SerializableComponent]
public sealed class CheckpointComponent : IGameComponent
{
    public string Id { get; set; } = "";
    public int Index { get; set; }

    public IComponent Clone() => new CheckpointComponent { Id = Id, Index = Index };
}

[SerializableComponent]
public sealed class Animator2DComponent : IGameComponent
{
    public string Clip { get; set; } = "idle";
    public int Frame { get; set; }
    public float Timer { get; set; }
    public float FrameTime { get; set; } = 0.14f;
    public bool Loop { get; set; } = true;
    public string Sheet { get; set; } = "player";

    public IComponent Clone() => new Animator2DComponent
    {
        Clip = Clip, Frame = Frame, Timer = Timer, FrameTime = FrameTime, Loop = Loop, Sheet = Sheet
    };
}

[SerializableComponent]
public sealed class PropComponent : IGameComponent
{
    public string Kind { get; set; } = "";
    public float Factor { get; set; }
    public float Anchor { get; set; }
    public float BaseY { get; set; }
    public int Amount { get; set; }
    public string ImageKey { get; set; } = "";
    public bool Taken { get; set; }

    public IComponent Clone() => new PropComponent
    {
        Kind = Kind, Factor = Factor, Anchor = Anchor, BaseY = BaseY,
        Amount = Amount, ImageKey = ImageKey, Taken = Taken
    };
}

[SerializableComponent]
public sealed class ZoneComponent : IGameComponent
{
    public string Kind { get; set; } = "ladder";
    public float W { get; set; } = 1f;
    public float H { get; set; } = 1f;

    public IComponent Clone() => new ZoneComponent { Kind = Kind, W = W, H = H };
}
