using Microsoft.Xna.Framework.Input;

namespace Celeste.Mod.Gunleste {
    public class GunlesteModuleSettings : EverestModuleSettings {
        #region KeyBindings

        [DefaultButtonBinding(Buttons.RightTrigger, Keys.A)]
        public ButtonBinding ButtonShot { get; set; }

        #endregion
    }
}