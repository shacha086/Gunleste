using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.Gunleste {

    [Tracked]
    public class Bullet : Entity {
        public Facings Facing { get; set; }

        public double Speed { get; set; } = 3;

        public Vector2 Direction {
            get => Facing == Facings.Left ? -Vector2.UnitX : Vector2.UnitX;
        }

        public Player Owner { get; }

        public Bullet(Player owner) {
            Sprite sprite;
            Owner = owner;
            Vector2 position;

            if (owner.StateMachine.State == 1 && Math.Abs((float)Input.MoveX + (float)owner.Facing) < 1e-4) {
                Facing = owner.Facing == Facings.Left ? Facings.Right : Facings.Left;
            } else {
                Facing = owner.Facing;
            }

            position = Facing switch {
                Facings.Left => new Vector2 { Y = owner.BottomLeft.Y - 8, X = owner.BottomLeft.X - 1 },
                Facings.Right => new Vector2 { Y = owner.BottomRight.Y - 8, X = owner.BottomRight.X - 1 },
                _ => throw new NotImplementedException($"{owner.Facing} is not supported.")
            };
            Position = position;
            Add(sprite = GFX.SpriteBank.Create("bullet"));
            sprite.Play("frames");
            owner.Scene.Add(this);
        }

        public override void Update() {
            Speed = 3;

            if (Owner.Scene == null) {
                return;
            }

            if (this.IsOutOfScene()) {
                RemoveSelf();
                return;
            }

            var collidedSolid = Owner.Scene.CollideFirst<Solid>(Hitbox);

            if (collidedSolid != null) {
                if (!Util.DamageableSolids.ContainsKey(collidedSolid.GetType())) {
                    RemoveSelf();
                    return;
                }

                Util.DamageableSolids[collidedSolid.GetType()](collidedSolid, Owner, this);
            }

            foreach (var collidedBarrier in Scene.Tracker.GetEntities<SeekerBarrier>()) {
                var collidable = collidedBarrier.Collidable;
                collidedBarrier.Collidable = true;

                if (collidedBarrier.CollideRect(Hitbox)) {
                    (collidedBarrier as SeekerBarrier)?.OnReflectSeeker();
                    RemoveSelf();
                }

                collidedBarrier.Collidable = collidable;
            }

            var bigEyeball = Scene.Entities.FirstOrDefault(it => it.GetType() == typeof(TempleBigEyeball));

            if (bigEyeball != null) {
                var collidable = bigEyeball.Collidable;
                bigEyeball.Collidable = true;

                if (bigEyeball.CollideRect(Hitbox)) {
                    Util.DamageableEntities[typeof(TempleBigEyeball)](bigEyeball, Owner, this);
                }

                bigEyeball.Collidable = collidable;
            }

            foreach (var fakeWall in Scene.Tracker.GetEntities<FakeWall>()) {
                var collidable = fakeWall.Collidable;
                fakeWall.Collidable = true;

                if (fakeWall.CollideRect(Hitbox)) {
                    Util.DamageableEntities[typeof(FakeWall)](fakeWall, Owner, this);
                }

                fakeWall.Collidable = collidable;
            }

            base.Update();

            var collided = Owner.Scene.CollideBy(
                Hitbox, it =>
                    it is IDamageable ||
                    Util.DamageableEntities.ContainsKey(it)
            ).FirstOrDefault(it => it != null);

            switch (collided) {
                case null:
                    Move();
                    return;

                case IDamageable damageable:
                    damageable.OnDamage(Owner);
                    break;
            }

            if (Util.DamageableEntities.ContainsKey(collided.GetType())) {
                Util.DamageableEntities[collided.GetType()](collided, Owner, this);
            }
            
            Move();
        }

        private void Move() {
            switch (Facing) {
                case Facings.Left:
                    Position.X -= (int)Speed;
                    break;

                case Facings.Right:
                    Position.X += (int)Speed;
                    break;

                default:
                    throw new NotSupportedException($"{Facing} is not supported.");
            }
        }
        
        public Rectangle Hitbox => new((int)Position.X - 2, (int)Position.Y - 2, 4, 4);
    }
}