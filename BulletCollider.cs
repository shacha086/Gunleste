using System;
using Monocle;

namespace Celeste.Mod.Gunleste; 

[Tracked]
public class BulletCollider : Component {
    private readonly Action<IDamageable, Bullet> _onCollide;
    private readonly Collider _collider;
    private readonly IDamageable _damageable;
    public BulletCollider(
        Action<IDamageable, Bullet> onCollide,
        IDamageable damageable,
        Collider collider = null
    ) : base(false, false) {
        _onCollide = onCollide;
        _damageable = damageable;
        _collider = collider;
    }

    public bool Check(Bullet bullet) {
        if (_collider == null) {
            if (!bullet.CollideCheck(Entity)) {
                return false;
            }
            _onCollide(_damageable, bullet);
            return true;
        }

        var collider2 = Entity.Collider;
        Entity.Collider = _collider;
        var flag = bullet.CollideCheck(Entity);
        Entity.Collider = collider2;

        if (!flag) {
            return false;
        }
        _onCollide(_damageable, bullet);
        return true;
    }
}
