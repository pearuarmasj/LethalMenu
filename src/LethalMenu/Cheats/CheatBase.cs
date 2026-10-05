namespace LethalMenu.Cheats
{
    /// Lifecycle contract (driven by LethalMenuMod):
    /// - OnEnable / OnDisable fire on every toggle transition, whatever flipped the flag (menu, keybind,
    ///   config load, another cheat). OnDisable also fires on unload for cheats that are still enabled.
    /// - OnUpdate / OnLateUpdate run every frame regardless of state (some cheats poll hotkeys while off).
    /// - OnFixedUpdate / OnGUI run only while enabled.
    /// Cheats that mutate game state apply it while enabled and restore it in OnDisable; they never write
    /// "vanilla" values every frame while disabled, since that fights the game's own logic.
    public abstract class CheatBase
    {
        public abstract string Name { get; }
        public abstract Hack HackType { get; }

        public bool IsEnabled
        {
            get => HackType.IsEnabled();
            set => HackType.SetEnabled(value);
        }

        public virtual void OnUpdate() { }
        public virtual void OnLateUpdate() { }
        public virtual void OnFixedUpdate() { }
        public virtual void OnGUI() { }
        public virtual void OnEnable() { }
        public virtual void OnDisable() { }
    }
}
