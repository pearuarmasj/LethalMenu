namespace LethalMenu.Cheats
{
    /// 
    /// Base class for all cheats.
    /// 
    public abstract class CheatBase
    {
        /// 
        /// Display name of the cheat.
        /// 
        public abstract string Name { get; }

        /// 
        /// Whether the cheat is currently enabled.
        /// 
        public bool IsEnabled { get; set; }

        /// 
        /// Called every frame when enabled.
        /// 
        public virtual void OnUpdate() { }

        /// 
        /// Called every fixed update when enabled.
        /// 
        public virtual void OnFixedUpdate() { }

        /// 
        /// Called during OnGUI when enabled.
        /// 
        public virtual void OnGUI() { }

        /// 
        /// Called when the cheat is enabled.
        /// 
        public virtual void OnEnable() { }

        /// 
        /// Called when the cheat is disabled.
        /// 
        public virtual void OnDisable() { }
    }
}
