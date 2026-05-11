namespace LethalMenu.Mixins
{
    public interface IJetpack { }

    public static class JetpackMixin
    {
        public static void ExplodeAllJetpacks(this IJetpack _)
        {
            foreach (var item in LethalMenuMod.Items)
            {
                if (item is JetpackItem jp && jp != null)
                    jp.ExplodeJetpackServerRpc();
            }
        }
    }
}
