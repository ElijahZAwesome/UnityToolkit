using UnityEditor;

namespace Editor.DiscordPresence
{
    /*
        Probably get rid of this when the full toolkit is more in place
        because we'll be loading configuration from playerprefs so we wont wanna
        call this code if we know that Discord is disabled.
    */
    [InitializeOnLoad]
    public static class DiscordEntrypoint
    {
        static DiscordEntrypoint()
        {
            DiscordManager.Start();
        }
    }
}