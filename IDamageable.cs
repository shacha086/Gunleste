using System;
using System.Collections.Generic;
using Monocle;

namespace Celeste.Mod.Gunleste {
    public interface IDamageable {
        int Health { get; }
        Action<Player> OnDamage { get; }
        void Die();
    }
}