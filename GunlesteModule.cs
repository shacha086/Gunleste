using System;
using System.Collections;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;

namespace Celeste.Mod.Gunleste; 

public class GunlesteModule : EverestModule {
    public static GunlesteModule Instance { get; private set; }

    public override Type SettingsType => typeof(GunlesteModuleSettings);
    public static GunlesteModuleSettings Settings => (GunlesteModuleSettings) Instance._Settings;

    public override Type SessionType => typeof(GunlesteModuleSession);
    public static GunlesteModuleSession Session => (GunlesteModuleSession) Instance._Session;

    public GunlesteModule() {
        Instance = this;
    }

    public override void Load() {
        // TODO: apply any hooks that should always be active
        On.Celeste.Player.Update += HookPlayerUpdate;
        On.Celeste.ZipMover.Sequence += HookZipMoverSequence;
        On.Celeste.FlutterBird.IdleRoutine += HookFlutterBirdIdleRoutine;
        On.Celeste.BirdNPC.WaitRoutine += HookBirdNpcWaitRoutine;
    }

    public override void Unload() {
        // TODO: unapply any hooks applied in Load()
        On.Celeste.Player.Update -= HookPlayerUpdate;
        On.Celeste.ZipMover.Sequence -= HookZipMoverSequence;
        On.Celeste.FlutterBird.IdleRoutine -= HookFlutterBirdIdleRoutine;
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
            var collidedBullet = _.Find(x => self.CollideRect(((Bullet) x).Hitbox));
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

    private static IEnumerator HookFlutterBirdIdleRoutine(On.Celeste.FlutterBird.orig_IdleRoutine orig, FlutterBird self) {
        DynamicData selfData = new(self);
        for (;;)
        {
            Player player = self.Scene.Tracker.GetEntity<Player>();
            Bullet bullet = self.Scene.Tracker.GetEntity<Bullet>();
            float delay = 0.25f + Calc.Random.NextFloat(1f);
            for (float p = 0f; p < delay; p += Engine.DeltaTime)
            {
                if (player != null && Math.Abs(player.X - self.X) < 48f && player.Y > self.Y - 40f && player.Y < self.Y + 8f)
                {
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
            SimpleCurve bezier = new SimpleCurve(self.Position, target, (self.Position + target) / 2f - Vector2.UnitY * 14f);
            for (float p = 0f; p < 1f; p += Engine.DeltaTime * 4f)
            {
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
        while (!self.AutoFly)
        {
            var player2 = self.Scene.Tracker.GetEntity<Player>();
            var bullet2 = self.Scene.Tracker.GetEntity<Bullet>();
            if (player2 != null && (player2.Center - self.Position).Length() < 32f)
            {
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
}