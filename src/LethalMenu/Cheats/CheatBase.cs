namespace LethalMenu.Cheats
{
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
        public virtual void OnFixedUpdate() { }
        public virtual void OnGUI() { }
        public virtual void OnEnable() { }
        public virtual void OnDisable() { }
    }
}
