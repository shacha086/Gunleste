using System;
using Monocle;

namespace Celeste.Mod.Gunleste {

    [Tracked]
    public class BulletCollider : Component {
        private readonly Action<Entity, Bullet> _onCollide;
        private readonly Collider _collider;
        private readonly Entity _entity;

        public BulletCollider(
            Action<Entity, Bullet> onCollide,
            Entity entity,
            Collider collider = null
        ) : base(false, false) {
            _onCollide = onCollide;
            _entity = entity;
            _collider = collider;
        }

        public bool Check(Bullet bullet) {
            if (_collider == null) {
                if (!bullet.CollideCheck(Entity)) {
                    return false;
                }

                _onCollide(_entity, bullet);
                return true;
            }

            var collider2 = Entity.Collider;
            Entity.Collider = _collider;
            var flag = bullet.CollideCheck(Entity);
            Entity.Collider = collider2;

            if (!flag) {
                return false;
            }

            _onCollide(_entity, bullet);
            return true;
        }
    }
}