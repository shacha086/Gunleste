using System;
using System.Collections;
using System.Collections.Generic;
using Monocle;

namespace Celeste.Mod.Gunleste {

    public abstract class AbstractDamageableEntity : Entity, IDamageable {
        public abstract int Health { get; }
        public abstract Action<Player> OnDamage { get; }
        public abstract void Die();
    }
}