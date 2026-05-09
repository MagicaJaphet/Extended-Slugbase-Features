using System;
using Menu.Remix.MixedUI;

namespace ExtendedSlugbase
{
    public class RemixOptions : OptionInterface
    {
	    public static RemixOptions Instance { get; } = new();
        public static void RegisterOI()
        {
            if (MachineConnector.GetRegisteredOI(Plugin._MOD_ID) != Instance)
            {
                MachineConnector.SetRegisteredOI(Plugin._MOD_ID, Instance);
            }
        }

        public static Configurable<bool> ShowPrototype { get; } = Instance.config.Bind(nameof(ShowPrototype), false, new ConfigurableInfo(
            "Enable the Prototype.",
            null, "", "Show The Prototype in the select and arena menu."));
        
        public static Configurable<string> SelectedSlugcat { get; } = Instance.config.Bind(nameof(SelectedSlugcat), "", new ConfigurableInfo(
            "Enable a dropdown of all enabled slugbase characters.",
            null, "", "Changes selected slugbase character."));

        public OpTab ModOptionsTab { get; private set; }
        public OpTab SlugbaseConfigTab { get; private set; }


        public RemixOptions() {}

        public override void Initialize()
        {
            base.Initialize();

            ModOptionsTab = new OpTab(this, "Mod Options");
            SlugbaseConfigTab = new OpTab(this, "Json Config");

            Tabs = [SlugbaseConfigTab, ModOptionsTab];

            SetUpModConfig();

            JsonConfig.SetUpSlugbaseConfig(SlugbaseConfigTab);
        }

        private void SetUpModConfig()
        {
            float yOffset = 0f;
            UIQueue.InitializeQueues(ModOptionsTab, 0f, ref yOffset,
            new OpCheckBox.Queue(ShowPrototype));
        }
    }
}
