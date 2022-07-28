using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FMOD.Studio;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;

namespace Celeste.Mod.Gunleste;

public static class Util {
    public static readonly Dictionary<object, object> TempDict = new();

    public static bool IsOutOfScene(this Entity entity) {
        return !entity.Position.In(entity.SceneAs<Level>().Bounds);
    }

    public static bool In(this Vector2 it, Rectangle rectangle) {
        // Console.WriteLine($"entity position: {it}, rectangle.Right: {rectangle.Right}, rectangle.Left: {rectangle.Left}, rectangle.Bottom: {rectangle.Bottom}, rectangle.Top: {rectangle.Top}");
        return it.X < rectangle.Right && it.X > rectangle.Left && it.Y < rectangle.Bottom && it.Y > rectangle.Top;
    }

    public static Dictionary<Type, Action<Entity, Player, Bullet>> DamageableSolids { get; } = new() {
        {
            typeof(DreamBlock), (entity, _, bullet) => {
                if (entity is not DreamBlock dreamBlock) {
                    return;
                }

                DynamicData dreamData = new(dreamBlock);

                if (!dreamData.Get<bool>("playerHasDreamDash")) {
                    bullet.RemoveSelf();
                    return;
                }

                bullet.Depth = entity.Depth - 1;
                bullet.Speed = 5;
                var burst = entity.SceneAs<Level>().Displacement.AddBurst(bullet.Center, 0.3f, 0f, 15f);
                burst.WorldClipCollider = entity.Collider;
                burst.WorldClipPadding = 2;
            }
        }
    };

    public static Dictionary<Type, Action<Entity, Player, Bullet>> DamageableEntities { get; } = new() {
        {
            typeof(AngryOshiro), (entity, _, bullet) => {
                if (entity is not AngryOshiro oshiro) {
                    return;
                }

                DynamicData oshiroData = new(oshiro);
                var stateMachine = oshiroData.Get<StateMachine>("state");

                if (stateMachine.State == 5) {
                    return; // if (oshiro.state.State == 2)
                }

                Audio.Play("event:/game/general/thing_booped", oshiro!.Position);
                stateMachine.State = 5; // oshiro.state.State = 5;
                oshiroData.Get<SoundSource>("prechargeSfx").Stop(); // oshiro.prechargeSfx.Stop(true);
                oshiroData.Get<SoundSource>("chargeSfx").Stop(); // oshiro.chargeSfx.Stop(true);
                bullet.RemoveSelf();
            }
        }, {
            typeof(TheoCrystal), (entity, _, bullet) => {
                if (entity is not TheoCrystal theo) {
                    return;
                }

                if (!SaveData.Instance.Assists.Invincible) {
                    theo.Die();
                }

                bullet.RemoveSelf();
            }
        }, {
            typeof(FinalBoss), (entity, player, bullet) => {
                if (entity is not FinalBoss boss) {
                    return;
                }

                boss.OnPlayer(player);
                bullet.RemoveSelf();
            }
        }, {
            typeof(Strawberry), (entity, player, _) => {
                if (entity is not Strawberry strawberry) {
                    return;
                }

                strawberry.OnPlayer(player);
            }
        }, {
            typeof(Puffer), (entity, _, bullet) => {
                if (entity is not Puffer puffer) {
                    return;
                }

                DynamicData pufferData = new(puffer);

                if (pufferData.Get("state").ToString() != "Idle") {
                    return;
                }

                pufferData.Invoke("Explode");
                pufferData.Invoke("GotoGone");
                bullet.RemoveSelf();
            }
        }, {
            typeof(StrawberrySeed), (entity, player, _) => {
                if (entity is not StrawberrySeed seed) {
                    return;
                }

                DynamicData seedData = new(seed);
                seedData.Invoke("OnPlayer", player);
            }
        }, {
            typeof(Key), (entity, player, _) => {
                if (entity is not Key key) {
                    return;
                }

                DynamicData keyData = new(key);
                keyData.Invoke("OnPlayer", player);
            }
        }, {
            typeof(Refill), (entity, player, _) => {
                if (entity is not Refill refill) {
                    return;
                }

                DynamicData refillData = new(refill);
                refillData.Invoke("OnPlayer", player);
            }
        }, {
            typeof(DustRotateSpinner), (entity, _, bullet) => {
                if (entity is not DustRotateSpinner) {
                    return;
                }

                bullet.RemoveSelf();
            }
        }, {
            typeof(DustStaticSpinner), (entity, _, bullet) => {
                if (entity is not DustStaticSpinner) {
                    return;
                }

                bullet.RemoveSelf();
            }
        }, {
            typeof(DustTrackSpinner), (entity, _, bullet) => {
                if (entity is not DustTrackSpinner) {
                    return;
                }

                bullet.RemoveSelf();
            }
        }, {
            typeof(BladeRotateSpinner), (entity, _, bullet) => {
                if (entity is not BladeRotateSpinner) {
                    return;
                }

                bullet.RemoveSelf();
            }
        }, {
            typeof(BladeTrackSpinner), (entity, _, bullet) => {
                if (entity is not BladeTrackSpinner) {
                    return;
                }

                bullet.RemoveSelf();
            }
        }, {
            typeof(Water), (entity, _, bullet) => {
                if (entity is not Water) {
                    return;
                }

                bullet.Speed = 2;
            }
        }, {
            typeof(DashBlock), (entity, _, bullet) => {
                if (entity is not DashBlock dashBlock) {
                    return;
                }

                DynamicData dashData = new(dashBlock);

                if (!dashData.Get<bool>("canDash")) {
                    bullet.RemoveSelf();
                }

                var count = 1;

                if (!TempDict.ContainsKey(entity)) {
                    TempDict.Add(entity, count);
                } else {
                    count = (int) TempDict[entity];
                }

                if (count > 3) {
                    TempDict.Remove(entity);
                    dashBlock.Break(bullet.Position, Vector2.Zero, true, true);
                } else {
                    var tileType = dashData.Get<char>("tileType");
                    TempDict[entity] = ++count;
                    Audio.Play("event:/game/04_cliffside/snowball_impact", entity.Position);

                    entity.Scene.Add(
                        Engine.Pooler.Create<Debris>()
                            .Init(
                                bullet.Center with {
                                    X = bullet.Facing == Facings.Left ? bullet.Center.X + 2 : bullet.Center.X - 2
                                }, tileType
                            )
                            .BlastFrom(bullet.Center)
                    );
                }
            }
        }, {
            typeof(TouchSwitch), (entity, _, _) => {
                if (entity is not TouchSwitch touchSwitch) {
                    return;
                }

                touchSwitch.TurnOn();
            }
        }, {
            typeof(Snowball), (entity, _, ballet) => {
                if (entity is not Snowball snowball) {
                    return;
                }

                DynamicData snowballData = new(snowball);
                snowballData.Invoke("Destroy");
                Audio.Play("event:/game/general/thing_booped", snowball.Position);
                ballet.RemoveSelf();
            }
        }, {
            typeof(Torch), (entity, player, _) => {
                if (entity is not Torch torch) {
                    return;
                }

                DynamicData torchData = new(torch);
                torchData.Invoke("OnPlayer", player);
            }
        }, {
            typeof(DashSwitch), (entity, _, ballet) => {
                if (entity is not DashSwitch dashSwitch) {
                    return;
                }

                DynamicData switchData = new(dashSwitch);

                void HandleDashSwitch(Vector2 direction) {
                    Audio.Play("event:/game/05_mirror_temple/button_activate", entity.Position);
                    var sprite = switchData.Get<Sprite>("sprite");
                    sprite.Play("push");
                    switchData.Set("pressed", true);
                    dashSwitch.MoveTo(switchData.Get<Vector2>("pressedTarget"));
                    dashSwitch.Collidable = false;
                    dashSwitch.Position -= switchData.Get<Vector2>("pressDirection") * 2f;

                    entity.SceneAs<Level>()
                        .ParticlesFG.Emit(
                            switchData.Get<bool>("mirrorMode") ? DashSwitch.P_PressAMirror : DashSwitch.P_PressA, 10,
                            entity.Position + sprite.Position, direction.Perpendicular() * 6f,
                            sprite.Rotation - 3.1415927f
                        );

                    entity.SceneAs<Level>()
                        .ParticlesFG.Emit(
                            switchData.Get<bool>("mirrorMode") ? DashSwitch.P_PressBMirror : DashSwitch.P_PressB, 4,
                            entity.Position + sprite.Position, direction.Perpendicular() * 6f,
                            sprite.Rotation - 3.1415927f
                        );

                    if (switchData.Get<bool>("allGates")) {
                        using var enumerator = entity.Scene.Tracker.GetEntities<TempleGate>().GetEnumerator();

                        while (enumerator.MoveNext()) {
                            var enumeratorCurrent = enumerator.Current;
                            var templeGate = (TempleGate) enumeratorCurrent;

                            if (templeGate is {Type: TempleGate.Types.NearestSwitch} &&
                                templeGate.LevelID == switchData.Get<EntityID>("id").Level) {
                                templeGate.SwitchOpen();
                            }
                        }

                        goto x;
                    }

                    var gate = switchData.Invoke<TempleGate>("GetGate");
                    gate?.SwitchOpen();

                    x:
                    var templeMirrorPortal = entity.Scene.Entities.FindFirst<TempleMirrorPortal>();
                    templeMirrorPortal?.OnSwitchHit(Math.Sign(entity.X - entity.SceneAs<Level>().Bounds.Center.X));

                    if (switchData.Get<bool>("persistent")) {
                        entity.SceneAs<Level>().Session.SetFlag(switchData.Get<string>("FlagName"));
                    }
                }

                switch (switchData.Get<DashSwitch.Sides>("side")) {
                    case DashSwitch.Sides.Left:
                        if (ballet.Facing != Facings.Left) {
                            ballet.RemoveSelf();
                            return;
                        }

                        HandleDashSwitch(DashSwitch.Sides.Left.ToVector2());
                        break;
                    case DashSwitch.Sides.Right:
                        if (ballet.Facing != Facings.Right) {
                            ballet.RemoveSelf();
                            return;
                        }

                        HandleDashSwitch(DashSwitch.Sides.Right.ToVector2());
                        break;

                    case DashSwitch.Sides.Up:
                    case DashSwitch.Sides.Down:
                    default:
                        Audio.Play("event:/game/05_mirror_temple/button_return", entity.Position);
                        ballet.RemoveSelf();
                        Audio.Play("event:/game/05_mirror_temple/button_depress", entity.Position);
                        return;
                }
            }
        }, {
            typeof(Seeker), (entity, _, bullet) => {
                if (entity is not Seeker seeker) {
                    return;
                }

                DynamicData seekerData = new(seeker);
                seekerData.Invoke("GotBouncedOn", bullet);
                bullet.RemoveSelf();
            }
        }, {
            typeof(TempleCrackedBlock), (entity, _, bullet) => {
                if (entity is not TempleCrackedBlock templeCrackedBlock) {
                    return;
                }

                var count = 1;

                if (!TempDict.ContainsKey(entity)) {
                    TempDict.Add(entity, count);
                } else {
                    count = (int) TempDict[entity];
                }

                if (count > 3) {
                    TempDict.Remove(entity);
                    templeCrackedBlock.Break(bullet.Position);
                } else {
                    TempDict[entity] = ++count;
                    Audio.Play("event:/game/04_cliffside/snowball_impact", entity.Position);

                    entity.Scene.Add(
                        Engine.Pooler.Create<Debris>()
                            .Init(
                                bullet.Center with {
                                    X = bullet.Facing == Facings.Left ? bullet.Center.X + 2 : bullet.Center.X - 2
                                }, '1'
                            )
                            .BlastFrom(bullet.Center)
                    );
                }
            }
        }, {
            typeof(Bumper), (entity, _, bullet) => {
                if (entity is not Bumper bumper) {
                    return;
                }

                DynamicData bumperData = new(bumper);

                if (bumperData.Get<bool>("fireMode")) {
                    var vector = (bullet.Center - entity.Center).SafeNormalize();
                    bumperData.Set("hitDir", -vector);
                    bumperData.Get<Wiggler>("hitWiggler").Start();
                    Audio.Play("event:/game/09_core/hotpinball_activate", entity.Position);
                    bumperData.Set("respawnTimer", 0.6f);
                    bullet.RemoveSelf();

                    entity.SceneAs<Level>()
                        .Particles.Emit(
                            Bumper.P_FireHit, 12, entity.Center + vector * 12f, Vector2.One * 3f, vector.Angle()
                        );
                } else if (bumperData.Get<float>("respawnTimer") <= 0f) {
                    Audio.Play(
                        entity.SceneAs<Level>().Session.Area.ID == 9
                            ? "event:/game/09_core/pinballbumper_hit"
                            : "event:/game/06_reflection/pinballbumper_hit", entity.Position
                    );
                    bumperData.Set("respawnTimer", 0.6f);
                    // TODO
                    bumperData.Get<Sprite>("sprite").Play("hit", true);
                    bumperData.Get<Sprite>("spriteEvil").Play("hit", true);
                    bumperData.Get<VertexLight>("light").Visible = false;
                    bumperData.Get<BloomPoint>("bloom").Visible = false;
                    entity.SceneAs<Level>().Displacement.AddBurst(entity.Center, 0.3f, 8f, 32f, 0.8f);
                    bullet.RemoveSelf();
                }
            }
        }, {
            typeof(FireBall), (entity, _, bullet) => {
                if (entity is not FireBall fireBall) {
                    return;
                }

                DynamicData ballData = new(fireBall);

                if (!ballData.Get<bool>("iceMode")) {
                    var direction = (bullet.Center - entity.Center).SafeNormalize();
                    bullet.RemoveSelf();
                    ballData.Set("hitDir", direction);
                    ballData.Get<Wiggler>("hitWiggler").Start();
                    return;
                }

                Audio.Play("event:/game/09_core/iceball_break", entity.Position);
                ballData.Get<Sprite>("sprite").Play("shatter");
                ballData.Set("broken", true);
                ballData.Set("Collidable", false);
                entity.SceneAs<Level>().Particles.Emit(FireBall.P_IceBreak, 18, entity.Center, Vector2.One * 6f);
                bullet.RemoveSelf();
            }
        }, {
            typeof(CoreModeToggle), (entity, player, bullet) => {
                if (entity is not CoreModeToggle toggle) {
                    return;
                }

                DynamicData toggleData = new(toggle);

                toggleData.Invoke("OnPlayer", player);
                bullet.RemoveSelf();
            }
        }, {
            typeof(LightningBreakerBox), (entity, _, bullet) => {
                if (entity is not LightningBreakerBox box) {
                    return;
                }

                DynamicData boxData = new(box);

                boxData.Get<SoundSource>("firstHitSfx")?.Stop();
                Audio.Play("event:/new_content/game/10_farewell/fusebox_hit_2", entity.Position);
                Celeste.Freeze(0.2f);
                boxData.Invoke("Break");
                Input.Rumble(RumbleStrength.Strong, RumbleLength.Long);

                var dir = bullet.Facing.ToVector2();
                boxData.Invoke("SmashParticles", dir.Perpendicular());
                boxData.Invoke("SmashParticles", -dir.Perpendicular());
            }
        }, {
            typeof(FakeWall), (entity, _, _) => {
                if (entity is not FakeWall fakeWall) {
                    return;
                }

                DynamicData fakeWallData = new(fakeWall);

                fakeWallData.Set("fade", true);

                if (!TempDict.ContainsKey(entity) || !Audio.IsPlaying(TempDict[entity] as EventInstance)) {
                    TempDict.Add(entity, Audio.Play("event:/game/general/secret_revealed", entity.Center));
                }

                entity.SceneAs<Level>().Session.DoNotLoad.Add(fakeWallData.Get<EntityID>("eid"));
            }
        }, {
            typeof(TempleBigEyeball), (entity, player, bullet) => {
                if (entity is not TempleBigEyeball eyeball) {
                    return;
                }

                DynamicData eyeballData = new(eyeball);

                if (eyeballData.Get<bool>("triggered")) {
                    return;
                }

                int count;

                if (!TempDict.ContainsKey(entity)) {
                    count = 1;
                    TempDict.Add(entity, 1);
                } else {
                    count = (int) TempDict[entity];
                }

                if (count < 4) {
                    TempDict[entity] = count + 1;
                    entity.SceneAs<Level>().Shake(0.4f);
                    eyeballData.Get<Wiggler>("bounceWiggler").Start();
                    Audio.Play("event:/game/05_mirror_temple/eyewall_bounce", bullet.Position);
                    bullet.RemoveSelf();
                } else {
                    TempDict.Remove(entity);
                    eyeballData.Set("triggered", true);
                    eyeballData.Get<Wiggler>("bounceWiggler").Start();
                    eyeball.Collidable = false;
                    Audio.SetAmbience(null);
                    Audio.Play("event:/game/05_mirror_temple/eyewall_destroy", entity.Position);
                    Alarm.Set(entity, 1.3f, delegate { Audio.SetMusic(null); });
                    entity.Add(new Coroutine(Burst()));
                    bullet.RemoveSelf();

                    IEnumerator Burst() {
                        eyeballData.Set("bursting", true);
                        var level = entity.SceneAs<Level>();
                        level.StartCutscene(l => l.CompleteArea(false, false, false), false, true);
                        level.RegisterAreaComplete();
                        Celeste.Freeze(0.1f);
                        yield return null;
                        var start = Glitch.Value;
                        var tween = Tween.Create(Tween.TweenMode.Oneshot, null, 0.5f, true);
                        tween.OnUpdate = delegate(Tween t) { Glitch.Value = MathHelper.Lerp(start, 0f, t.Eased); };
                        entity.Add(tween);

                        player.StateMachine.State = 11;
                        player.StateMachine.Locked = true;

                        if (player.OnGround()) {
                            player.DummyAutoAnimate = false;
                            player.Sprite.Play("shaking");
                        }

                        entity.Add(new Coroutine(level.ZoomTo(player.TopCenter - level.Camera.Position, 2f, 0.5f)));

                        foreach (var templeEye in entity.Scene.Entities.FindAll<TempleEye>()) {
                            templeEye.Burst();
                        }

                        eyeballData.Get<Sprite>("sprite").Play("burst");
                        eyeballData.Get<Image>("pupil").Visible = false;
                        level.Shake(0.4f);
                        yield return 2f;

                        if (player.OnGround()) {
                            player.DummyAutoAnimate = false;
                            player.Sprite.Play("shaking");
                        }

                        entity.Visible = false;

                        var fade = typeof(TempleBigEyeball).GetNestedType("Fader", BindingFlags.NonPublic)
                            .GetConstructor(Type.EmptyTypes)
                            ?.Invoke(new object[0]);
                        DynamicData fadeData = new(fade);
                        new DynamicData(level).Invoke("Add", fade);
                        var time = fadeData.Get<float>("Fade");

                        while ((time += Engine.DeltaTime) < 1f) {
                            fadeData.Set("Fade", time);
                            yield return null;
                        }

                        yield return 1f;
                        level.EndCutscene();
                        level.CompleteArea(false, false, false);
                    }
                }
            }
        }, {
            typeof(Booster), (entity, _, bullet) => {
                if (entity is not Booster booster) {
                    return;
                }

                DynamicData boosterData = new(booster);

                if (boosterData.Get<float>("respawnTimer") > 0f ||
                    boosterData.Get<float>("cannotUseTimer") > 0f ||
                    boosterData.Get<bool>("BoostingPlayer")) {
                    return;
                }

                Audio.Play(
                    boosterData.Get<bool>("red")
                        ? "event:/game/05_mirror_temple/redbooster_end"
                        : "event:/game/04_cliffside/greenbooster_end", boosterData.Get<Sprite>("sprite").RenderPosition
                );
                boosterData.Get<Sprite>("sprite").Play("pop");
                boosterData.Get<Entity>("outline").Visible = true;
                boosterData.Set("cannotUseTimer", 0f);
                boosterData.Set("respawnTimer", 1f);
                boosterData.Set("BoostingPlayer", false);
                boosterData.Get<Wiggler>("wiggler").Stop();
                boosterData.Get<SoundSource>("loopingSfx").Stop();
                bullet.RemoveSelf();
            }
        }, {
            typeof(ZipMover), (_, _, _) => {  }  // This Entity was hooked somewhere else
        }, {
            typeof(HeartGem), (entity, player, _) => {
                if (entity is not HeartGem heart) {
                    return;
                }

                DynamicData heartData = new(heart);

                if (!heartData.Get<bool>("collected") && !heart.SceneAs<Level>().Frozen) {
                   heartData.Invoke("Collect", player); 
                }
            }
        }, {
            typeof(SummitGem), (entity, player, _) => {
                if (entity is not SummitGem gem) {
                    return;
                }
                
                DynamicData gemData = new(gem);
                var level = entity.SceneAs<Level>();
                entity.Add(new Coroutine(gemData.Invoke<IEnumerator>("SmashRoutine", player, level)));
            }
        }, {
            typeof(Cassette), (entity, player, bullet) => {
                if (entity is not Cassette cassette) {
                    return;
                }

                DynamicData cassetteData = new(cassette);
                cassetteData.Invoke("OnPlayer", player);
                bullet.RemoveSelf();
            }
        }
    };

    public static IEnumerable<Entity> CollideBy(this Scene scene, Rectangle rectangle, Func<Type, bool> action) {
        var entities = scene.Entities.Where(entity => action(entity.GetType()));

        return entities.Where(t => t.Collidable && t.CollideRect(rectangle));
    }

    public static Vector2 ToVector2(this Facings facing) {
        return facing switch {
            Facings.Left => -Vector2.UnitX,
            Facings.Right => Vector2.UnitX,
            _ => throw new NotImplementedException($"{facing} is not supported.")
        };
    }

    public static Vector2 ToVector2(this DashSwitch.Sides side) {
        return side switch {
            DashSwitch.Sides.Left => -Vector2.UnitX,
            DashSwitch.Sides.Right => Vector2.UnitX,
            DashSwitch.Sides.Up => Vector2.UnitY,
            DashSwitch.Sides.Down => -Vector2.UnitY,
            _ => throw new NotImplementedException($"{side} is not supported.")
        };
    }
}
