namespace LethalMenu.Cheats
{
    /// <summary>
    /// Base class for all cheats.
    /// </summary>
    public abstract class CheatBase
    {
        /// <summary>
        /// Display name of the cheat.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Whether the cheat is currently enabled.
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Called every frame when enabled.
        /// </summary>
        public virtual void OnUpdate() { }

        /// <summary>
        /// Called every fixed update when enabled.
        /// </summary>
        public virtual void OnFixedUpdate() { }

        /// <summary>
        /// Called during OnGUI when enabled.
        /// </summary>
        public virtual void OnGUI() { }

        /// <summary>
        /// Called when the cheat is enabled.
        /// </summary>
        public virtual void OnEnable() { }

        /// <summary>
        /// Called when the cheat is disabled.
        /// </summary>
        public virtual void OnDisable() { }
    }
}
