using System;
using BepInEx.Logging;
using ExtendedSlugbase.Features;
using ExtendedSlugbase.Helpers;
using MagicaHookingLibrary.Helpers;
using static MagicaHookingLibrary.Helpers.HookHelpers;
using MagicaHookingLibrary.Interfaces;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;

namespace ExtendedSlugbase.Hooks.ILHooks
{
    public class RoomILHooks : IOwnHooks
    {
        public void PreApply()
        {
            IL.ShelterDoor.Update += ILAction(ShelterDoor_Update);
            IL.Room.Loaded += ILAction(Room_Loaded);
        }

        private void ShelterDoor_Update(ILCursor c)
        {
            static bool GhostPings(bool isSaint, ShelterDoor self)
            {
                return isSaint || (self.room.game.TryGetFeature(GameFeaturesExt.ghostPing, out bool pings) && pings);
            }

            c.TryMoveToNextSlugcatBool(
                nameof(MoreSlugcatsEnums.SlugcatStatsName.Saint).GetSlugcatFieldInfo()
                ); // if (ModManager.MSC && this.room.game.IsStorySession && this.room.game.StoryCharacter == MoreSlugcatsEnums.SlugcatStatsName.Saint && this.room.world.region != null && World.CheckForRegionGhost(MoreSlugcatsEnums.SlugcatStatsName.Saint, this.room.world.region.name))
            c.EmitLdarg0Delegate(GhostPings);

            // Then unhardcode the ghost check
            static SlugcatStats.Name CheckGhosts(SlugcatStats.Name saint, ShelterDoor self)
            {
                return self.room.game.StoryCharacter;
            }

            c.GotoNext(
                MoveType.After,
                x => x.MatchLdsfld(nameof(MoreSlugcatsEnums.SlugcatStatsName.Saint).GetSlugcatFieldInfo())
                );
            c.EmitLdarg0Delegate(CheckGhosts);
        }

        private static void Room_Loaded(ILCursor c)
        {
            // Spearmaster broadcasts
            static bool DoesNotHaveBroadcasts(bool isNotSpear, Room self)
            {
                return isNotSpear && (!self.game.TryGetFeature(GameFeaturesExt.canProcessWhiteTokens, out bool canProcess) || !canProcess);
            }

            c.GotoNext(
                MoveType.After,
                x => x.MatchCallOrCallvirt(out _),
                x => x.MatchLdsfld(nameof(MoreSlugcatsEnums.SlugcatStatsName.Spear).GetSlugcatFieldInfo()),
                x => x.MatchCallOrCallvirt(out _)
                ); // AFTER: if (this.game.IsStorySession && this.game.StoryCharacter != MoreSlugcatsEnums.SlugcatStatsName.Spear)
            c.EmitLdarg0Delegate(DoesNotHaveBroadcasts);

            // Prevent misc broadcast pearls from spawning if recieving broadcasts because why would they spawn
            static bool HasBroadcastsAndIsBroadcastPearl(Room self, DataPearl.AbstractDataPearl.DataPearlType type)
            {
                return type == MoreSlugcatsEnums.DataPearlType.BroadcastMisc && self.game.TryGetFeature(GameFeaturesExt.canProcessWhiteTokens, out bool canProcess) && canProcess;
            }

            c.GotoNext(MoveType.After,
                x => x.MatchStfld(typeof(DataPearl.AbstractDataPearl).GetField(nameof(DataPearl.AbstractDataPearl.hidden)))
                ); // AFTER: (abstractPhysicalObject as DataPearl.AbstractDataPearl).hidden = (this.roomSettings.placedObjects[num21].data as PlacedObject.DataPearlData).hidden;
            
            ILCursor jump = c.CloneAndGoToNext(x => x.MatchBr(out _));
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloc, 90);
            c.EmitDelegate(HasBroadcastsAndIsBroadcastPearl);
            c.Emit(OpCodes.Brtrue, jump.MarkLabel());
        }
        

        public void OnApply()
        {
        }

        public void PostApply()
        {
        }

    }
}
