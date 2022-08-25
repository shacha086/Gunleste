using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;

namespace Celeste.Mod.Gunleste {
    public class GunlesteModule : EverestModule {
        public static GunlesteModule Instance { get; private set; }

        public override Type SettingsType => typeof(GunlesteModuleSettings);
        public static GunlesteModuleSettings Settings => (GunlesteModuleSettings)Instance._Settings;

        public override Type SessionType => typeof(GunlesteModuleSession);
        public static GunlesteModuleSession Session => (GunlesteModuleSession)Instance._Session;

        public GunlesteModule() {
            Instance = this;
        }

        public override void Load() {
            // TODO: apply any hooks that should always be active
            On.Celeste.Player.Update += HookPlayerUpdate;
            On.Celeste.ZipMover.Sequence += HookZipMoverSequence;
            On.Celeste.FlutterBird.IdleRoutine += HookFlutterBirdIdleRoutine;
            On.Celeste.BirdNPC.WaitRoutine += HookBirdNpcWaitRoutine;
            On.Celeste.CrumblePlatform.Sequence += HookCrumblePlatformSequence;
            On.Celeste.BadelineBoost.Update += HookBadelineBoostUpdate;
        }

        public override void Unload() {
            // TODO: unapply any hooks applied in Load()
            On.Celeste.Player.Update -= HookPlayerUpdate;
            On.Celeste.ZipMover.Sequence -= HookZipMoverSequence;
            On.Celeste.FlutterBird.IdleRoutine -= HookFlutterBirdIdleRoutine;
            On.Celeste.BirdNPC.WaitRoutine -= HookBirdNpcWaitRoutine;
            On.Celeste.CrumblePlatform.Sequence -= HookCrumblePlatformSequence;
            On.Celeste.BadelineBoost.Update -= HookBadelineBoostUpdate;
        }

        private static void HookPlayerUpdate(On.Celeste.Player.orig_Update orig, Player self) {
            orig(self);

            if (!Settings.ButtonShot.Pressed) {
                return;
            }

            if (self.Scene.Entities.FindAll<Bullet>().Count >= 4) {
                return;
            }

            _ = new Bullet(self);
        }

        private static IEnumerator HookZipMoverSequence(On.Celeste.ZipMover.orig_Sequence orig, ZipMover self) {
            DynamicData selfData = new(self);

            var start = self.Position;

            for (;;) {
                var _ = self.SceneAs<Level>().Tracker.Entities[typeof(Bullet)];
                var collidedBullet = _.Find(x => self.CollideRect(((Bullet)x).Hitbox));
                if (self.HasPlayerRider() || collidedBullet != null) {
                    bool flag = self.HasPlayerRider();

                    selfData.Get<SoundSource>("sfx").Play(
                        selfData.Get<ZipMover.Themes>("theme") == ZipMover.Themes.Normal
                            ? "event:/game/01_forsaken_city/zip_mover"
                            : "event:/new_content/game/10_farewell/zip_mover"
                    );
                    Input.Rumble(RumbleStrength.Medium, RumbleLength.Short);
                    self.StartShaking(0.1f);
                    yield return 0.1f;
                    selfData.Get<Sprite>("streetlight").SetAnimationFrame(3);

                    if (flag) {
                        self.StopPlayerRunIntoAnimation = false;
                    }

                    collidedBullet?.RemoveSelf();

                    var at = 0f;

                    while (at < 1f) {
                        yield return null;
                        at = Calc.Approach(at, 1f, 2f * Engine.DeltaTime);
                        selfData.Set("percent", Ease.SineIn(at));

                        var vector = Vector2.Lerp(
                            start, selfData.Get<Vector2>("target"), selfData.Get<float>("percent")
                        );
                        selfData.Invoke("ScrapeParticlesCheck", vector);

                        if (self.Scene.OnInterval(0.1f)) {
                            DynamicData rendererData = new(selfData.Get("pathRenderer"));
                            rendererData.Invoke("CreateSparks");
                        }

                        self.MoveTo(vector);
                    }

                    self.StartShaking(0.2f);
                    Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
                    self.SceneAs<Level>().Shake();

                    if (flag) {
                        self.StopPlayerRunIntoAnimation = true;
                    }

                    yield return 0.5f;

                    if (flag) {
                        self.StopPlayerRunIntoAnimation = false;
                    }

                    selfData.Get<Sprite>("streetlight").SetAnimationFrame(2);
                    at = 0f;

                    while (at < 1f) {
                        yield return null;
                        at = Calc.Approach(at, 1f, 0.5f * Engine.DeltaTime);
                        selfData.Set("percent", 1f - Ease.SineIn(at));
                        Vector2 position = Vector2.Lerp(selfData.Get<Vector2>("target"), start, Ease.SineIn(at));
                        self.MoveTo(position);
                    }

                    if (flag) {
                        self.StopPlayerRunIntoAnimation = true;
                    }

                    self.StartShaking(0.2f);
                    selfData.Get<Sprite>("streetlight").SetAnimationFrame(1);
                    yield return 0.5f;
                } else {
                    yield return null;
                }
            }
        }

        private static IEnumerator HookFlutterBirdIdleRoutine(On.Celeste.FlutterBird.orig_IdleRoutine orig,
            FlutterBird self) {
            DynamicData selfData = new(self);
            for (;;) {
                Player player = self.Scene.Tracker.GetEntity<Player>();
                Bullet bullet = self.Scene.Tracker.GetEntity<Bullet>();
                float delay = 0.25f + Calc.Random.NextFloat(1f);
                for (float p = 0f; p < delay; p += Engine.DeltaTime) {
                    if (player != null && Math.Abs(player.X - self.X) < 48f && player.Y > self.Y - 40f &&
                        player.Y < self.Y + 8f) {
                        self.FlyAway(Math.Sign(self.X - player.X), Calc.Random.NextFloat(0.2f));
                    }

                    if (bullet != null && (bullet.Center - self.Position).Length() < 32f) {
                        self.FlyAway(Math.Sign(self.X - bullet.X), Calc.Random.NextFloat(0.2f));
                    }

                    yield return null;
                }

                Audio.Play("event:/game/general/birdbaby_hop", self.Position);
                Vector2 target = selfData.Get<Vector2>("start") + new Vector2(-4f + Calc.Random.NextFloat(8f), 0f);
                selfData.Get<Sprite>("sprite").Scale.X = Math.Sign(target.X - self.Position.X);
                SimpleCurve bezier = new SimpleCurve(self.Position, target,
                    (self.Position + target) / 2f - Vector2.UnitY * 14f);
                for (float p = 0f; p < 1f; p += Engine.DeltaTime * 4f) {
                    self.Position = bezier.GetPoint(p);
                    yield return null;
                }

                selfData.Get<Sprite>("sprite").Scale.X = Math.Sign(selfData.Get<Sprite>("sprite").Scale.X) * 1.4f;
                selfData.Get<Sprite>("sprite").Scale.Y = 0.6f;
                self.Position = target;
            }
        }

        private static IEnumerator HookBirdNpcWaitRoutine(On.Celeste.BirdNPC.orig_WaitRoutine orig, BirdNPC self) {
            while (!self.AutoFly) {
                var player = self.Scene.Tracker.GetEntity<Player>();
                var bullet = self.Scene.Tracker.GetEntity<Bullet>();
                if (player != null && Math.Abs(player.X - self.X) < 120f) {
                    break;
                }

                if (bullet != null) {
                    break;
                }

                yield return null;
            }

            yield return self.Caw();
            while (!self.AutoFly) {
                var player2 = self.Scene.Tracker.GetEntity<Player>();
                var bullet2 = self.Scene.Tracker.GetEntity<Bullet>();
                if (player2 != null && (player2.Center - self.Position).Length() < 32f) {
                    break;
                }

                if (player2 != null && bullet2 != null && (bullet2.Center - self.Position).Length() < 16f) {
                    self.Sprite.Visible = false;
                    self.Depth = -1000000;
                    self.SceneAs<Level>().Displacement.AddBurst(self.Position, 0.3f, 0f, 80f);
                    DeathEffect deathEffect = new(Calc.HexToColor("9B3FB5"), self.Center - self.Position);
                    Audio.Play("event:/char/madeline/death", self.Position);
                    deathEffect.OnUpdate = f => self.Light.Alpha = 1f - f;
                    self.Add(deathEffect);
                    yield return deathEffect.Duration * 0.65f;
                    self.RemoveSelf();
                    self.SceneAs<Level>().Session.SetFlag("bird_fly_away_" + self.SceneAs<Level>().Session.Level);
                    yield break;
                }

                yield return null;
            }

            yield return self.StartleAndFlyAway();
        }

        private IEnumerator HookCrumblePlatformSequence(On.Celeste.CrumblePlatform.orig_Sequence orig,
            CrumblePlatform self) {
            var selfData = DynamicData.For(self);
            label_1:
            bool onTop;
            while (self.GetPlayerOnTop() == null) {
                var collidedBullet = self.SceneAs<Level>().Tracker.Entities[typeof(Bullet)]
                    .Find(x => self.CollideRect(((Bullet)x).Hitbox));
                if (collidedBullet != null) {
                    onTop = true;
                    Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
                    goto label_7;
                }

                if (self.GetPlayerClimbing() != null) {
                    onTop = false;
                    Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
                    goto label_7;
                }

                yield return null;
            }

            onTop = true;
            Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
            label_7:
            Audio.Play("event:/game/general/platform_disintegrate", self.Center);
            selfData.Get<ShakerList>("shaker").ShakeFor(onTop ? 0.6f : 1f, false);
            foreach (Image image in selfData.Get<List<Image>>("images"))
                self.SceneAs<Level>().Particles.Emit(CrumblePlatform.P_Crumble, 2,
                    self.Position + image.Position + new Vector2(0.0f, 2f), Vector2.One * 3f);
            for (int i = 0; i < (onTop ? 1 : 3); ++i) {
                yield return 0.2f;
                foreach (Image image in selfData.Get<List<Image>>("images"))
                    self.SceneAs<Level>().Particles.Emit(CrumblePlatform.P_Crumble, 2,
                        self.Position + image.Position + new Vector2(0.0f, 2f), Vector2.One * 3f);
            }

            float timer = 0.4f;
            if (onTop) {
                for (; timer > 0.0 && self.GetPlayerOnTop() != null; timer -= Engine.DeltaTime)
                    yield return null;
            } else {
                for (; timer > 0.0; timer -= Engine.DeltaTime)
                    yield return null;
            }

            selfData.Get<Coroutine>("outlineFader").Replace(selfData.Invoke<IEnumerator>("OutlineFade", 1f));
            selfData.Get<LightOcclude>("occluder").Visible = false;
            self.Collidable = false;
            float num = 0.05f;
            for (int index1 = 0; index1 < 4; ++index1) {
                for (int index2 = 0; index2 < selfData.Get<List<Image>>("images").Count; ++index2) {
                    if (index2 % 4 - index1 == 0)
                        selfData.Get<List<Coroutine>>("falls")[index2].Replace(selfData.Invoke<IEnumerator>("TileOut",
                            selfData.Get<List<Image>>("images")[selfData.Get<List<int>>("fallOrder")[index2]],
                            num * index1));
                }
            }

            yield return 2f;
            while (self.CollideCheck<Actor>() || self.CollideCheck<Solid>())
                yield return null;
            selfData.Get<Coroutine>("outlineFader").Replace(selfData.Invoke<IEnumerator>("OutlineFade", 0.0f));
            selfData.Get<LightOcclude>("occluder").Visible = true;
            self.Collidable = true;
            for (int index3 = 0; index3 < 4; ++index3) {
                for (int index4 = 0; index4 < selfData.Get<List<Image>>("images").Count; ++index4) {
                    if (index4 % 4 - index3 == 0)
                        selfData.Get<List<Coroutine>>("falls")[index4].Replace(selfData.Invoke<IEnumerator>("TileIn",
                            index4,
                            selfData.Get<List<Image>>("images")[selfData.Get<List<int>>("fallOrder")[index4]],
                            0.05f * index3));
                }
            }

            goto label_1;
        }
        
        private void HookBadelineBoostUpdate(On.Celeste.BadelineBoost.orig_Update orig, BadelineBoost self) {
            if (self.Sprite().Visible && self.Scene.OnInterval(0.05f))
                self.SceneAs<Level>().ParticlesBG.Emit(BadelineBoost.P_Ambience, 1, self.Center, Vector2.One * 3f);
            if (self.Holding() != null)
                self.Holding().Speed = Vector2.Zero;
            if (!self.Travelling())
            {
                var player = self.Scene.Tracker.GetEntity<Player>();
                if (player != null)
                {
                    var bullet = self.Scene.Tracker.GetEntity<Bullet>();
                    var bulletNum = -1f;
                    if (bullet != null) {
                        bulletNum = Calc.ClampedMap((bullet.Center - self.Position).Length(), 16f, 64f, 12f, 0.0f);
                    }
                    var playerNum = Calc.ClampedMap((player.Center - self.Position).Length(), 16f, 64f, 12f, 0.0f);
                    var num = Calc.Max(bulletNum, playerNum);
                    self.Sprite().Position = Calc.Approach(self.Sprite().Position, (player.Center - self.Position).SafeNormalize() * num, 32f * Engine.DeltaTime);
                    if (self.CanSkip() && player.Position.X - (double) self.X >= 100.0 && self.NodeIndex() + 1 < self.Nodes().Length)
                        self.Skip();
                }
            }
            self.Light().Visible = self.Bloom().Visible = self.Sprite().Visible || self.Stretch().Visible;
            DynamicData.For(self.Components).Invoke("Update");
        }
    }
}