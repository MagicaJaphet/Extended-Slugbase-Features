using BepInEx;
using BepInEx.Logging;
using MagicaHookingLibrary;
using MagicaHookingLibrary.Helpers;
using MonoMod.RuntimeDetour;
using SlugBase;
using SlugBase.Features;
using System.Linq;
using System.Reflection;
using System.Security.Permissions;
using System;
using ExtendedSlugbase.Features;
using SlugBase.DataTypes;
using ExtendedSlugbase.Hooks.OnHooks;
using static ExtendedSlugbase.Features.ExtFeatureTypes;
using static ExtendedSlugbase.Extensions.SlugbaseHelpers;
using ExtendedSlugbase.Assets;

// Allows access to private members
#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace ExtendedSlugbase;

[BepInDependency("magica.extendedmenuscenes", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("magica.hookinglibrary", BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency("slime-cubed.slugbase", BepInDependency.DependencyFlags.HardDependency)]
[BepInPlugin(_MOD_ID, "Extended Slugbase Features", "2.0.0")]

public class Plugin : PluginTemplate
{
	//FEATURE: Food that stuns or kills if it gives negative pips
	//FEATURE: Saint ascension (toggleable flight)
	//FEATURE: Modify movement bonuses/penalties (land and water)
	//FEATURE: Watcher invisibility
	//FEATURE: expedition perk slow time
	//FEATURE: expedition burdens in campaign
	//FEATURE: Configure player grasps (and add sprites)
	//FEATURE: Embedded pearl
	//FEATURE: Lock abilities behind a check, with the ability to add tutorial text when first encountering the ability to use them

	//FEATURE: Mauling side effects
	//FEATURE: Weaver cosmetic toggles

	// TODO: Replace with more rich logger
	public static new ManualLogSource Logger;

	public const string _MOD_ID = "magica.extendedslugbasefeatures";

	public static SlugcatStats.Name Prototype = new("magica.Prototype");

	public static bool ExtendedMenuScenes { get; internal set; }

	//TODO: Add translation support for slugbase
	public Plugin() : base()
	{
		Logger = base.Logger;

		_ = new ExtPlayerFeatures();
		_ = new ExtGameFeatures();
		_ = new TimelineFeatures();
		_ = new ObsoleteFeatures();

		// Slugbase hook to trace where Features are initalized from
		try
		{
			// Load all features and check if they have a RequiredDLC attribute
			foreach((string feature, RequiresDLC dlc) in from ass in ReflectionHelpers.GetScanAssemblies()
			from type in ass.GetTypes() 
			from field in type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) where typeof(Feature).IsAssignableFrom(field.FieldType) && field.GetCustomAttribute(typeof(ObsoleteAttribute)) == null // Ignore obsolete features
			let attr = field.GetCustomAttribute(typeof(RequiresDLC)) let feature = field.GetValue(null) where feature != null && attr != null 
			select ((feature as Feature).ID, attr as RequiresDLC))
			{
				if (RegisteredFeatures.TryGetValue(feature, out var info))
				{
					info.dlc = dlc;
					RegisteredFeatures[feature] = info;
				}
				else
				{
					RegisteredFeatures.Add(feature, new() { dlc = dlc });        
				}
			}
			
			_ = new Hook(typeof(ColorSlot).GetConstructor([typeof(int), typeof(JsonAny)]), SlugbaseHooks.ColorSlot_ctor);
			_ = new Hook(FeatureManager.GetMethod(nameof(Register), BindingFlags.Public | BindingFlags.Static), SlugbaseHooks.FeatureManagerRegisterHook);
			_ = new Hook(typeof(SlugBaseCharacter.FeatureList).GetMethod(nameof(SlugBaseCharacter.FeatureList.Set), BindingFlags.Public | BindingFlags.Instance), SlugbaseHooks.FeatureListSet);
			_ = new Hook(AddMany, SlugbaseHooks.FeatureListAddMany);
		}
		catch (Exception ex)
		{
			Logger?.LogError(ex);
		}
	}

    public override void PreModsInit(RainWorld self)
    {
        HookHelpers.ApplyHooks(HookHelpers.HookType.Pre, Logger);
    }

    public override void OnModsInit(RainWorld self)
    {
		ExtendedMenuScenes = MiscHelpers.IsModActive("magica.extendedmenuscenes");

		RemixOptions.RegisterOI();
        HookHelpers.ApplyHooks(HookHelpers.HookType.On, Logger);
    }

    public override void PostModsInit(RainWorld self)
    {
        HookHelpers.ApplyHooks(HookHelpers.HookType.Post, Logger);
		AtlasManager.LoadSlugbaseImages();
    }
}