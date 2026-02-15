using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Reflection;
using MagicaHookingLibrary.Helpers;
using SlugBase;
using SlugBase.Features;
using static ExtendedSlugbase.Helpers.FeatureHelpers;

namespace ExtendedSlugbase.Helpers
{
    public static class SlugbaseHelpers
    {
        public static readonly Type FeatureManager;
        public static readonly MethodInfo Register;
        public static readonly MethodInfo TryGetFeature;

        /// <summary>
        /// Shorthand for invoking <see cref="TryGetFeature"/>.
        /// </summary>
        public static bool InvokeTryGetFeature(string id, out Feature result)
        {
            object[] args = [id, null];
            result = null;
            if (TryGetFeature?.Invoke(null, args) is bool gotFeature && gotFeature && args[1] is Feature feature)
            {
                result = feature;
                return true;
            }
            return false;
        }

        public static readonly MethodInfo AddMany;

        public static readonly Type FeatureHooks;

        static SlugbaseHelpers()
        {
            FeatureManager = (from a in ReflectionHelpers.GetScanAssemblies() from type in a.GetTypes() where type.Name == "FeatureManager" select type).FirstOrDefault();
            Register = FeatureManager?.GetMethod(nameof(Register), BindingFlags.Public | BindingFlags.Static);
            TryGetFeature = FeatureManager?.GetMethod(nameof(TryGetFeature), BindingFlags.Public | BindingFlags.Static);

            // From SlugBaseCharacter.FeatureList
            AddMany = typeof(SlugBaseCharacter.FeatureList).GetMethod(nameof(AddMany), BindingFlags.NonPublic | BindingFlags.Instance);

            // From SlugBase.Features
            FeatureHooks = typeof(Feature).Assembly.GetTypes().FirstOrDefault(x => x.Name == nameof(FeatureHooks));
        }

        /// <summary>
        /// A dictionary containing all registered <see cref="Feature"/>s, and their <see cref="FeatureInfo"/>.
        /// </summary>
        public static Dictionary<string, FeatureInfo> RegisteredFeatures { get; } = [];
        public struct FeatureInfo
        {
            public RequiresDLC dlc;
            public Assembly originAssembly;
        }

        public static void CheckForInvalidDLC(string id, JsonAny json, bool throwDLCError = true)
        {
            if (RegisteredFeatures.TryGetValue(id, out var info) && !RequiresDLC.DLCsEnabled(id) && throwDLCError)
            {
                throw new JsonException($"{id} needs {info.dlc.needsMSC.BlankConditional("MSC")}{(info.dlc.needsMSC && info.dlc.needsWatcher).BlankConditional(info.dlc.mutualExclusion ? " or" : " and")}{info.dlc.needsWatcher.BlankConditional(" Watcher")} enabled to use!", json);
            }
        }
    }

}
