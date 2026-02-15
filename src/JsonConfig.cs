using Menu.Remix.MixedUI;

namespace ExtendedSlugbase
{
    /// <summary>
    /// A built-in JSON configuration for editing features easily in the remix menu.
    /// </summary>
    internal class JsonConfig
    {
        internal static void SetUpSlugbaseConfig(OpTab slugbaseConfigTab)
        {
            //LATER: Transfer old code here

            // Temp
            float yOffset = 0f;
            UIQueue.InitializeQueues(slugbaseConfigTab, 0f, ref yOffset,
            new OpLabel.Queue("To be added in a future update.", bigText: true));
        }
    }
}
