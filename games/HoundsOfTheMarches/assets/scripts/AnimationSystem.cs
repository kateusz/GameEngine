using System.Numerics;
using ECS;
using ECS.Systems;
using SceneComponents;
using SceneComponents.Rendering;
using Scripting;

namespace Hounds.Marches;

[Register(typeof(IGameSystem))]
public sealed class AnimationSystem(Context context, IEntityHierarchy hierarchy) : IGameSystem
{
    public int Priority => 110;

    public void OnInit() { }

    public void OnShutdown() { }

    public void OnUpdate(TimeSpan deltaTime)
    {
        var game = March.Game(context);
        if (game?.Pack == null)
            return;

        var dt = March.Dt(deltaTime);
        var frozen = game.Hitstop > 0f;
        foreach (var (entity, animator) in context.View<Animator2DComponent>())
        {
            if (!entity.TryGetComponent<SubTextureRendererComponent>(out var sprite))
                continue;
            var parent = hierarchy.GetParent(entity);
            if (parent == null ||
                !parent.TryGetComponent<ActorComponent>(out var actor) ||
                !parent.TryGetComponent<CombatComponent>(out var combat) ||
                !parent.TryGetComponent<TransformComponent>(out var body))
                continue;

            var moving = MathF.Abs(actor.KnockVx) > 0.4f;
            if (parent.TryGetComponent<SceneComponents.Physics.RigidBody2DComponent>(out var rigid))
                moving = MathF.Abs(rigid.Velocity.X) > 0.4f;
            var clip = SliceRules.Clip(actor.Kind, combat.Action, combat.Frame, combat.Phase2, moving, actor.Grounded);
            if (!game.Pack.Sheets.TryGetValue(animator.Sheet, out var sheet))
                continue;
            var frames = sheet.Get(clip);
            if (frames.Length == 0)
                frames = sheet.Get("idle");
            if (frames.Length == 0)
                continue;

            if (animator.Clip != clip)
            {
                animator.Clip = clip;
                animator.Frame = 0;
                animator.Timer = 0f;
            }

            animator.Loop = clip is "idle" or "run" or "air" or "phase2";
            if (!frozen)
            {
                animator.Timer += dt;
                if (animator.Timer >= animator.FrameTime)
                {
                    animator.Timer = 0f;
                    animator.Frame = animator.Loop
                        ? (animator.Frame + 1) % frames.Length
                        : System.Math.Min(animator.Frame + 1, frames.Length - 1);
                }
            }

            var frame = frames[animator.Frame % frames.Length];
            var flip = actor.Facing < 0;
            var u0 = flip ? frame.U1 : frame.U0;
            var u1 = flip ? frame.U0 : frame.U1;
            if (sprite.TexCoords is not { Length: 4 })
                sprite.TexCoords = new Vector2[4];
            sprite.TexCoords[0] = new Vector2(u0, frame.VBottom);
            sprite.TexCoords[1] = new Vector2(u1, frame.VBottom);
            sprite.TexCoords[2] = new Vector2(u1, frame.VTop);
            sprite.TexCoords[3] = new Vector2(u0, frame.VTop);
            if (sprite.TexturePath != frame.Texture)
                sprite.TexturePath = frame.Texture;

            if (actor.Flash > 0f)
            {
                actor.Flash -= dt;
                sprite.Color = new Vector4(1f, 0.82f, 0.75f, 1f);
            }
            else if (combat.Phase2)
            {
                sprite.Color = new Vector4(1f, 0.9f, 0.72f, 1f);
            }
            else
            {
                sprite.Color = Vector4.One;
            }

            sprite.SortOrder = 140f - body.Translation.Y;
        }
    }
}
