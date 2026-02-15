using System;
using ExtendedSlugbase.Features;
using ExtendedSlugbase.Helpers;
using MagicaHookingLibrary.Helpers;
using MagicaHookingLibrary.Interfaces;
using static MagicaHookingLibrary.Helpers.HookHelpers;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using UnityEngine;
using RWCustom;
using System.Linq;
using ExtendedSlugbase.Objects;
using System.Collections.Generic;
using static ExtendedSlugbase.Objects.PlayerObjects;
using System.Security.Cryptography.X509Certificates;
using SlugBase.DataTypes;

namespace ExtendedSlugbase.Hooks.ILHooks
{
    public class PlayerILHooks : IOwnHooks
    {
        internal static SpearCreatability SpearFeature(Player self)
        {
            if (self.TryGetFeature(PlayerFeaturesExt.canCreateSpears, out var result))
            {
                return result;
            }
            return null;
        }

        internal static Craftability CraftFeature(Player self)
        {
            if (self.TryGetFeature(PlayerFeaturesExt.canCraftObjects, out var result))
            {
                return result;
            }
            return null;
        }

        internal static Nullable<bool> SwallowFeature(Player self)
        {
            if (self.TryGetFeature(PlayerFeaturesExt.cantSwallowObjects, out var result))
            {
                return result;
            }
            return null;
        }

        internal static DoubleJump JumpFeature(Player self)
        {
            if (self.TryGetFeature(PlayerFeaturesExt.doubleJump, out var result))
            {
                return result;
            }
            return null;
        }

        internal static PlayerTongue TongueFeature(Player self)
        {
            if (self.TryGetFeature(PlayerFeaturesExt.saintTongue, out var result))
            {
                return result;
            }
            return null;
        }
        internal static PlayerTongue TongueFeature(Player.Tongue self)
        {
            if (self.player.TryGetFeature(PlayerFeaturesExt.saintTongue, out var result))
            {
                return result;
            }
            return null;
        }

        public void PreApply()
        {
            IL.Player.SlugSlamConditions += ILAction(Player_SlugSlamConditions);
            IL.Player.Collide += ILAction(Player_Collide);
            IL.Player.ClassMechanicsSpearmaster += ILAction(Player_ClassMechanicsSpearmaster);
            IL.Player.ThrowObject += ILAction(Player_ThrowObject);
            IL.Player.CanIPickThisUp += ILAction(Player_CanIPickThisUp);
            IL.Player.SaintTongueCheck += ILAction(Player_SaintTongueCheck);
            IL.Player.ClassMechanicsSaint += ILAction(Player_ClassMechanicsSaint);
            IL.Player.GrabUpdate += ILAction(Player_GrabUpdate);
            IL.Player.Stun += ILAction(Player_Stun);
            IL.Player.TongueUpdate += ILAction(Player_TongueUpdate);
            IL.Player.Tongue.Update += ILAction(Player_Tongue_Update);
            IL.Player.ClassMechanicsArtificer += ILAction(Player_ClassMechanicsArtificer);
            IL.Player.Regurgitate += ILAction(Player_Regurgitate);
        }

		private void Player_SlugSlamConditions(ILCursor c)
        {
            static bool CantSlam(bool isNotGourm, Player self)
            {
                return isNotGourm && !(self.TryGetFeature(PlayerFeaturesExt.canSlam, out var canSlam) && canSlam);
            }

            c.TryMoveToNextSlugcatBool(
                nameof(MoreSlugcatsEnums.SlugcatStatsName.Gourmand).GetSlugcatFieldInfo()
                );
            c.EmitLdarg0Delegate(CantSlam);
        }


        private void Player_Collide(ILCursor c)
        {
            static bool CanSlam(bool isGourm, Player self)
            {
                return isGourm || (self.TryGetFeature(PlayerFeaturesExt.canSlam, out var canSlam) && canSlam);
            }

            c.GotoNext(
                MoveType.After,
                x => x.MatchCallOrCallvirt(typeof(Player).GetProperty(nameof(Player.isGourmand), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).GetGetMethod())
                );
            c.MoveAfterLabels();
            c.EmitLdarg0Delegate(CanSlam);

            c.TryMoveToNextSlugcatBool(
                nameof(MoreSlugcatsEnums.SlugcatStatsName.Gourmand).GetSlugcatFieldInfo()
                ); // if (this.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Gourmand && this.animation == Player.AnimationIndex.Roll && this.gourmandAttackNegateTime <= 0)
            c.EmitLdarg0Delegate(CanSlam);
            		
        }


        private void Player_ClassMechanicsSpearmaster(ILCursor c, ILContext il)
        {
            var specksFeature = il.GetFeature<SpearCreatability, Player>(c, SpearFeature);

            static bool GeneratesSpears(bool isSpear, SpearCreatability specks)
            {
                return isSpear || specks != null;
            }

            c.TryMoveToNextSlugcatBool(
                nameof(MoreSlugcatsEnums.SlugcatStatsName.Spear).GetSlugcatFieldInfo()
                );
            c.EmitFeatureDelegate(specksFeature, GeneratesSpears);

            static float GenerationSpeed(float speed, SpearCreatability specks)
            {
                return specks?.GenerationSpeed ?? speed;
            }

            c.GotoNext(
                MoveType.After,
                x => x.MatchLdcR4(0.05f)
                );
            c.EmitFeatureDelegate(specksFeature, GenerationSpeed);
        }


        private void Player_ThrowObject(ILCursor c)
        {
            static bool TossSpears(bool isSaint, Player self)
            {
                return isSaint || (self.TryGetFeature(PlayerFeaturesExt.tossSpears, out bool tossSpear) && tossSpear);
            }

            c.TryMoveToNextSlugcatBool(
                nameof(MoreSlugcatsEnums.SlugcatStatsName.Saint).GetSlugcatFieldInfo()
                );
            c.EmitLdarg0Delegate(TossSpears);
        }

        private void Player_CanIPickThisUp(ILCursor c)
        {
            static bool CanPullSpears(bool isNotArtiOrMSC, Player self)
            {
                return isNotArtiOrMSC && (!self.TryGetFeature(PlayerFeaturesExt.pullSpearsFromWalls, out bool canPullSpears) || !canPullSpears);
            }

            c.TryMoveToNextSlugcatBool(
                nameof(MoreSlugcatsEnums.SlugcatStatsName.Artificer).GetSlugcatFieldInfo()
                ); // if ((obj as Weapon).mode == Weapon.Mode.StuckInWall && (!ModManager.MMF || !MMF.cfgDislodgeSpears.Value) && (!ModManager.MSC || this.SlugCatClass != MoreSlugcatsEnums.SlugcatStatsName.Artificer)            
            c.EmitLdarg0Delegate(CanPullSpears);
        }

        private void Player_SaintTongueCheck(ILCursor c)
        {
            static bool HasTongue(bool isSaint, Player self)
            {
                return isSaint || self.TryGetFeature(PlayerFeaturesExt.saintTongue, out _);
            }

            c.TryMoveToNextSlugcatBool(
                nameof(MoreSlugcatsEnums.SlugcatStatsName.Saint).GetSlugcatFieldInfo()
                );
            c.EmitLdarg0Delegate(HasTongue);
        }

        private void Player_ClassMechanicsSaint(ILCursor c)
        {
            static bool HasTongue(bool isSaint, Player self)
            {
                return isSaint || self.TryGetFeature(PlayerFeaturesExt.saintTongue, out _) || (self.room != null && self.room.game.TryGetFeature(GameFeaturesExt.ghostPing, out bool ghostPing) && ghostPing);
            }

            c.TryMoveToNextSlugcatBool(
                nameof(MoreSlugcatsEnums.SlugcatStatsName.Saint).GetSlugcatFieldInfo()
                );
            c.TryMoveToNextSlugcatBool(
                nameof(MoreSlugcatsEnums.SlugcatStatsName.Saint).GetSlugcatFieldInfo()
                );
            c.EmitLdarg0Delegate(HasTongue);

            // Ghost ping implementation
            static bool GhostPings(bool isSaint, Player self)
            {
                return self.room != null && (isSaint || (self.room.game.TryGetFeature(GameFeaturesExt.ghostPing, out bool pings) && pings));
            }

            c.GotoNext(
                MoveType.After,
                x => x.MatchLdloc(0)
                );
            c.EmitLdarg0Delegate(GhostPings);

            // Patch the region name assignment when "" so it skips if it's the Player's spawning room which isn't a shelter
            static bool IsNotStartingRoom(bool isNullOrEmpty, Player self)
            {
                return isNullOrEmpty && (self.room.abstractRoom.shelter || self.room.abstractRoom.name == "SI_SAINTINTRO" || self.AI != null);
            }

            c.GotoNext(
                MoveType.After,
                x => x.MatchLdstr(""),
                x => x.MatchCallOrCallvirt(out _)
                );
            c.EmitLdarg0Delegate(IsNotStartingRoom);

            static SlugcatStats.Name GhostForSlugcat(SlugcatStats.Name saint, Player self)
            {
                return new(SlugcatStats.Name.values.entries.FirstOrDefault(slug => slug == self.room.game.TimelinePoint.value));
            }

            c.GotoNext(
                MoveType.After, 
                x => x.MatchBrfalse(out _),
                x => x.MatchLdsfld(nameof(MoreSlugcatsEnums.SlugcatStatsName.Saint).GetSlugcatFieldInfo())
                ); // if (this.room != null && World.CheckForRegionGhost(MoreSlugcatsEnums.SlugcatStatsName.Saint, this.room.world.region.name))
            c.EmitLdarg0Delegate(GhostForSlugcat); // Removes the hardcoded World.CheckForRegionGhost for Saint
        }

        private void Player_GrabUpdate(ILCursor c, ILContext il)
        {
            var specksFeature = il.GetFeature<SpearCreatability, Player>(c, SpearFeature);
            var craftFeature = il.GetFeature<Craftability, Player>(c, CraftFeature);
            var swallowFeature = il.GetFeature<Nullable<bool>, Player>(c, SwallowFeature);

            // Generates spears
            static bool GeneratesSpears(bool isSpear, SpearCreatability specks)
            {
                return isSpear || specks != null;
            }
            static bool CanMakeSpear(bool isSpear, SpearCreatability specks, Player self)
            {
                return isSpear || (specks != null && self.FoodInStomach >= specks.FoodCost);
            }
            
            static float GenerationSpeed(float speed, SpearCreatability specks)
            {
                return specks?.GenerationSpeed ?? speed;
            }

            c.TryMoveToNextSlugcatBool(
                nameof(MoreSlugcatsEnums.SlugcatStatsName.Spear).GetSlugcatFieldInfo()
                ); // if (ModManager.MSC && !this.input[0].pckp && this.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Spear && self.graphicsModule != null)
            c.EmitFeatureDelegate(specksFeature, GeneratesSpears);

            c.GotoNext(
                MoveType.After,
                x => x.MatchLdcR4(0.05f)
                );
            c.EmitFeatureDelegate(specksFeature, GenerationSpeed);

            // Feeds from spears
            static bool FeedsFromSpears(bool isNotSpear, SpearCreatability specks)
            {
                return isNotSpear && !(specks?.FeedFromSpears ?? false);
            }

            c.TryMoveToNextSlugcatBool(
                nameof(MoreSlugcatsEnums.SlugcatStatsName.Spear).GetSlugcatFieldInfo()
                );
            c.EmitFeatureDelegate(specksFeature, FeedsFromSpears);
            
            // Crafting
            static bool CanOneHandedCraft(bool isArti, Craftability craft)
            {
                return isArti || (craft != null && craft.OneHandedRecipeList.Count > 0);
            }
            c.TryMoveToNextSlugcatBool(
                nameof(MoreSlugcatsEnums.SlugcatStatsName.Artificer).GetSlugcatFieldInfo()
                );
            c.EmitFeatureDelegate(craftFeature, CanOneHandedCraft);

            // Regurgitate
            static bool CanRegurgitate(bool isGourmand, Craftability craft, Player self)
            {
                return isGourmand || (craft != null && craft.RegurgitateList.objects.Count > 0 && !self.GraspsCanBeCrafted());
            }
            
            c.GotoNext(
                MoveType.After,
                x => x.MatchCallOrCallvirt(typeof(Player).GetProperty(nameof(Player.isGourmand)).GetGetMethod())
                );
            c.EmitFeatureDelegate(craftFeature, CanRegurgitate, true);

            // Can swallow
            static bool CanSwallow(bool isNotSpear, Nullable<bool> cantSwallow)
            {
                return isNotSpear && !(cantSwallow ?? false);
            }

            c.TryMoveToNextSlugcatBool(
                nameof(MoreSlugcatsEnums.SlugcatStatsName.Spear).GetSlugcatFieldInfo()
                );
            c.EmitFeatureDelegate(swallowFeature, CanSwallow);

            // Generates spears
            c.TryMoveToNextSlugcatBool(
                nameof(MoreSlugcatsEnums.SlugcatStatsName.Spear).GetSlugcatFieldInfo()
                );
            c.EmitFeatureDelegate(specksFeature, CanMakeSpear, true);

            static bool GenerateSpearInput(int inputY, Nullable<bool> cantSwallow, Player self)
            {
                int y = (cantSwallow ?? false) ? 0 : 1;
                return !(self.input[0].y == y); // Because it's a brtrue, we have to reverse the logic
            }

            c.GotoNext( 
                MoveType.AfterLabel,
                x => x.MatchBrtrue(out _),
                x => x.MatchLdarg(0)
                ); // this.input[0].y == 0
            c.EmitFeatureDelegate(swallowFeature, GenerateSpearInput, true); // Consume the stack so we can just put our own bool

            static SoundID PullSoundID(SoundID spearPull, SpearCreatability specks)
            {
                return specks?.PullSound ?? spearPull;
            }

            c.GotoNext(
                MoveType.After,
                x => x.MatchLdsfld(typeof(MoreSlugcatsEnums.MSCSoundID).GetField(nameof(MoreSlugcatsEnums.MSCSoundID.SM_Spear_Pull)))
                ); // this.room.PlaySound(MoreSlugcatsEnums.MSCSoundID.SM_Spear_Pull, 0f, 1f, 1f + UnityEngine.Random.value * 0.5f);
            c.EmitFeatureDelegate(specksFeature, PullSoundID);

            c.GotoNext(
                MoveType.After,
                x => x.MatchLdcR4(0.05f)
                );
            c.EmitFeatureDelegate(specksFeature, GenerationSpeed);

            // Prevent head shaking if no reaction
            static bool NoHeadShake(SpearCreatability specks)
            {
                return !(specks?.ReactsToSpears ?? false);
            }

            c.GotoNext(
                MoveType.After,
                x => x.MatchLdloc(16),
                x => x.MatchLdfld(out _),
                x => x.MatchLdcR4(0.6f)
                ); // if (tailSpecks2.spearProg > 0.6f)
            ILLabel jumpLabel = (ILLabel)c.Next.Operand;
            c.GotoPrev(
                MoveType.AfterLabel,
                x => x.MatchLdloc(16)
                );
            c.EmitFeatureDelegate(specksFeature, NoHeadShake);
            c.Emit(OpCodes.Brtrue, jumpLabel);

            // Skip past all this nonsense so we have full control over spear spawning lmao
            static bool CraftsSpears(SpearCreatability self)
            {
                return self != null;
            }

            c.GotoNext(
                MoveType.After,
                x => x.MatchBneUn(out _)
                ); // if (tailSpecks2.spearProg == 1f)
            ILCursor jump = c.CloneAndGoToNext(
                MoveType.After, 
                x => x.MatchStfld(typeof(Player).GetField(nameof(Player.wantToThrow), ReflectionHelpers.anyFlag))
                );
            c.EmitFeatureDelegate(specksFeature, CraftsSpears);
            c.Emit(OpCodes.Brtrue, jump.MarkLabel());
            
            // Largely taken from self game code, just made to cater to our needs
            static void CustomSpearCrafting(SpearCreatability spearCreatability, Player self, PlayerGraphics.TailSpeckles tailSpecks)
            {
                if (spearCreatability != null)
                {
                    self.room.PlaySound(spearCreatability.SnapSound, 0f, 1f, 0.5f + UnityEngine.Random.value * 1.5f);
                    self.smSpearSoundReady = false;

                    var pGraphics = self.graphicsModule as PlayerGraphics;
                    Vector2 pos = pGraphics.tail[(int)((float)pGraphics.tail.Length / 2f)].pos;

                    if (spearCreatability.Dripping)
                    {    
                        for (int j = 0; j < 4; j++)
                        {
                            Vector2 a = Custom.DirVec(pos, self.bodyChunks[1].pos);
                            self.room.AddObject(new WaterDrip(pos + Custom.RNV() * (UnityEngine.Random.value * 1.5f), Custom.RNV() * (3f * UnityEngine.Random.value) + a * Mathf.Lerp(2f, 6f, UnityEngine.Random.value), false));
                        }
                    }
                    for (int k = 0; k < 5; k++)
                    {
                        Vector2 randomDir = Custom.RNV();
                        self.room.AddObject(new Spark(pos + randomDir * (UnityEngine.Random.value * 40f), randomDir * Mathf.Lerp(4f, 30f, UnityEngine.Random.value), Color.white, null, 4, 18));
                    }
                    tailSpecks.setSpearProgress(0f);

                    AbstractPhysicalObject abstractSpear = null;
                    if (spearCreatability.SpearObj != null && spearCreatability.SpearObj.TryGetObject(self.room.abstractRoom, self.room.GetWorldCoordinate(self.mainBodyChunk.pos), out var spearObj))
                    {
                        abstractSpear = spearObj;
                    }

					// Our default object
					abstractSpear ??= new AbstractSpear(self.room.world, null, self.room.GetWorldCoordinate(self.mainBodyChunk.pos), self.room.game.GetNewID(), false);
					
					self.room.abstractRoom.AddEntity(abstractSpear);
                    abstractSpear.pos = self.abstractCreature.pos;
                    abstractSpear.RealizeInRoom();

                    self.SubtractFood(spearCreatability.FoodCost);
                    
                    Vector2 vector = self.bodyChunks[0].pos;
                    Vector2 a3 = Custom.DirVec(self.bodyChunks[1].pos, self.bodyChunks[0].pos);
                    if (Mathf.Abs(self.bodyChunks[0].pos.y - self.bodyChunks[1].pos.y) > Mathf.Abs(self.bodyChunks[0].pos.x - self.bodyChunks[1].pos.x) && self.bodyChunks[0].pos.y > self.bodyChunks[1].pos.y)
                    {
                        vector += Custom.DirVec(self.bodyChunks[1].pos, self.bodyChunks[0].pos) * 5f;
                        a3 *= -1f;
                        a3.x += 0.4f * (float)self.flipDirection;
                        a3.Normalize();
                    }
                    abstractSpear.realizedObject.firstChunk.HardSetPosition(vector);
                    abstractSpear.realizedObject.firstChunk.vel = Vector2.ClampMagnitude((a3 * 2f + Custom.RNV() * UnityEngine.Random.value) / abstractSpear.realizedObject.firstChunk.mass, 6f);
                    if (self.FreeHand() != -1 && self.CanIPickThisUp(abstractSpear.realizedObject))
                    {
                        self.SlugcatGrab(abstractSpear.realizedObject, self.FreeHand());
                    }
                    if (abstractSpear.type == AbstractPhysicalObject.AbstractObjectType.Spear)
                    {
                        var spear = abstractSpear.realizedObject as Spear;
                        var spearCWT = CWTs.SpearCWT.GetData(abstractSpear as AbstractSpear);
						spearCWT.playerNumber = self.ArenaIndex();

                        if (spearCreatability.SpearObj == null)
                            spear.Spear_makeNeedle(tailSpecks.spearType, spear.grabbedBy != null);
						else if (spearCreatability.FeedFromSpears)
						{
							spear.spearmasterNeedle_hasConnection = spear.grabbedBy != null;
						}
							
						if (self.TryGetColorSlots(out var slots))
						{
							if (slots.FirstOrDefault(x => x.Name == "Spears") is ColorSlot spearSlot)
							{
								spearCWT.generatedSpearColor = spearSlot;
							}
							if (slots.FirstOrDefault(x => x.Name == "SpearsFade") is ColorSlot spearFadeSlot)
							{
								spearCWT.generatedSpearFadeColor = spearFadeSlot;
							}
						}
                    }
                    self.wantToThrow = 0;
                }            
            }

            jump.Emit(OpCodes.Ldloc, specksFeature);
            jump.Emit(OpCodes.Ldarg_0);
            jump.Emit(OpCodes.Ldloc, 16);
            jump.EmitDelegate(CustomSpearCrafting);

            // Regurgitate
            c.GotoNext(
                MoveType.After,
                x => x.MatchCallOrCallvirt(typeof(Player).GetProperty(nameof(Player.isGourmand)).GetGetMethod())
                );
            c.EmitFeatureDelegate(craftFeature, CanRegurgitate, true);
            c.GotoNext(
                MoveType.After,
                x => x.MatchCallOrCallvirt(typeof(Player).GetProperty(nameof(Player.isGourmand)).GetGetMethod())
                );
            c.EmitFeatureDelegate(craftFeature, CanRegurgitate, true);

            // Implement food cost
            static int RegurgitateCost(int one, Craftability craft)
            {
                return craft?.RegurgitateList.cost ?? one;
            }

            c.GotoNext(
                MoveType.After,
                x => x.MatchLdcI4(1)
                );
            c.EmitFeatureDelegate(craftFeature, RegurgitateCost);
            c.GotoNext(
                MoveType.After,
                x => x.MatchLdcI4(1)
                );
            c.EmitFeatureDelegate(craftFeature, RegurgitateCost);
        }

        private void Player_Stun(ILCursor c)
        {
            static bool IsImmuneToStun(bool isNotBlunt, Player self)
            {
                return isNotBlunt && !(self.TryGetFeature(PlayerFeaturesExt.noStunGraspPenalty, out var penalities) && penalities.TryGetValue(self.stunDamageType, out bool isImmune) && isImmune);
            }

            for (int i = 0; i < 3; i++)
            {
                c.GotoNext(
                    MoveType.After,
                    x => x.MatchLdsfld(typeof(Creature.DamageType).GetField(nameof(Creature.DamageType.Blunt))),
                    x => x.MatchCallOrCallvirt(out _)
                    );
                c.EmitLdarg0Delegate(IsImmuneToStun);
            }
        }

        private void Player_TongueUpdate(ILCursor c, ILContext il)
        {
            var saintTongue = il.GetFeature<PlayerTongue, Player>(c, TongueFeature);
            static float RopeLengthFactor(float orig, PlayerTongue tongue)
            {
                return tongue?.RetractSpeed * orig ?? orig;
            }

            c.GotoNext(
                x => x.MatchCallOrCallvirt(typeof(Player.Tongue).GetMethod(nameof(Player.Tongue.decreaseRopeLength)))
                );
            c.EmitFeatureDelegate(saintTongue, RopeLengthFactor);

            c.GotoNext(
                x => x.MatchCallOrCallvirt(typeof(Player.Tongue).GetMethod(nameof(Player.Tongue.increaseRopeLength)))
                );
            c.EmitFeatureDelegate(saintTongue, RopeLengthFactor);
        }
        
        private void Player_Tongue_Update(ILCursor c, ILContext il)
        {
            var saintTongue = il.GetFeature<PlayerTongue, Player.Tongue>(c, TongueFeature);
            static float RetractSpeed(float orig, PlayerTongue tongue)
            {
                return tongue?.RetractSpeed ?? orig;
            }

            for (int i = 0; i < 2; i++)
            {
                c.GotoNext(
                    x => x.MatchLdcR4(1),
                    x => x.MatchLdarg(0),
                    x => x.MatchLdfld(typeof(Player.Tongue).GetField(nameof(Player.Tongue.elastic)))
                    );
                c.GotoNext(x => x.MatchLdarg(0));
                c.EmitFeatureDelegate(saintTongue, RetractSpeed);
            }
        }

        private void Player_ClassMechanicsArtificer(ILCursor c, ILContext il)
        {
            var doubleJump = il.GetFeature<DoubleJump, Player>(c, JumpFeature);

            // Allows double jumping
            static bool ExplosiveJump(bool isArtificer, DoubleJump self)
            {
                return isArtificer || self != null;
            }

            c.TryMoveToNextSlugcatBool(
                nameof(MoreSlugcatsEnums.SlugcatStatsName.Artificer).GetSlugcatFieldInfo()
                );
            c.TryMoveToNextSlugcatBool(
                nameof(MoreSlugcatsEnums.SlugcatStatsName.Artificer).GetSlugcatFieldInfo()
                );
            c.EmitFeatureDelegate(doubleJump, ExplosiveJump);

            // Controls when the visuals start playing for the exhaustion
            static int JumpSoftLimit(int artiJumps, DoubleJump doubleJump)
            {
                if (doubleJump != null)
                {
                    return Math.Max(1, doubleJump.JumpLimit[0] - (doubleJump.JumpLimit.Length > 1 ? 2 : 5));
                }
                return artiJumps;
            } 

            c.GotoNext(
                x => x.MatchStloc(2)
                );
            c.EmitFeatureDelegate(doubleJump, JumpSoftLimit);

            // Control what effects happen
            //LATER: Replace with several functions related to the type of effect being called
            static bool CustomJumpEffect(DoubleJump doubleJump)
            {
                if (doubleJump != null)
                {
                    return true; //LATER: Replace with bool when properly implemented
                }
                return false;
            }

            c.GotoNext(
                x => x.MatchCallOrCallvirt(typeof(UnityEngine.Random).GetProperty(nameof(UnityEngine.Random.value)).GetGetMethod())
            );
            ILCursor jump = c.CloneAndGoToNext(
                x => x.MatchLdloc(0)
                );
            c.EmitFeatureDelegate(doubleJump, CustomJumpEffect);
            c.Emit(OpCodes.Brtrue, jump.MarkLabel());

            c.GotoNext(
                x => x.MatchLdloc(5)
                );
            jump = c.CloneAndGoToNext(
                x => x.MatchLdarg(0),
                x => x.MatchLdfld(out _),
                x => x.MatchLdsfld(typeof(SoundID).GetField(nameof(SoundID.Fire_Spear_Explode)))
                );
            c.EmitFeatureDelegate(doubleJump, CustomJumpEffect);
            c.Emit(OpCodes.Brtrue, jump.MarkLabel()); // Jump over the other effects if true

            static SoundID JumpSound(SoundID jump, DoubleJump doubleJump)
            {
                return doubleJump?.JumpSoundID ?? jump;
            }

            c.GotoNext(
                MoveType.After,
                x => x.MatchLdsfld(typeof(SoundID).GetField(nameof(SoundID.Fire_Spear_Explode)))
                );
            c.EmitFeatureDelegate(doubleJump, JumpSound);

            static int FirstJumpLimit(int artiJumps, DoubleJump doubleJump)
            {
                if (doubleJump != null)
                {
                    return Math.Max(1, doubleJump.JumpLimit.Length > 1 ? doubleJump.JumpLimit[0] : doubleJump.JumpLimit[0] - 3);
                }
                return artiJumps;
            }

            c.GotoNext(
                x => x.MatchStloc(4)
                );
            c.EmitFeatureDelegate(doubleJump, FirstJumpLimit);

            // Jump boost time woohoo!
            static float JumpBoost(float orig, DoubleJump doubleJump, Player self)
            {
                if (doubleJump != null)
                {
                    var jump = self.Malnourished && doubleJump.JumpBoost.Length > 1 ? doubleJump.JumpBoost[1] : doubleJump.JumpBoost[0];
                    return Mathf.Max(0f, jump + (orig - 8f));
                }
                return orig;
            }

            // All of the floats we need to modify!

            void EnumerateFloats(ILCursor c, IEnumerable<float> valuesToChange)
            {
                using var values = valuesToChange.GetEnumerator();
                while (values.MoveNext())
                {
                    float toChange = values.Current;

                    c.GotoNext(
                    MoveType.After,
                    x => x.MatchLdcR4(toChange)
                    );
                    c.EmitFeatureDelegate(doubleJump, JumpBoost, true);
                }
            }

            EnumerateFloats(c, [
                9f, 9f, 8f, 8f,

                8f, 7f, 6f,

                16f, 15f, 10f,

                11f, 10f, 8f,

                10f, 8f,

                15f, 13f
            ]);

            // Then fix up the hard jump limit
            static int JumpLimit(int artiJumps, DoubleJump doubleJump)
            {
                if (doubleJump != null)
                {
                    return Math.Max(1, doubleJump.JumpLimit.Length > 1 ? doubleJump.JumpLimit[1] : doubleJump.JumpLimit[0]);
                }
                return artiJumps;
            }

            static void DeathScenerio(DoubleJump doubleJump, Player self)
            {
                if (doubleJump != null)
                {
                    switch (doubleJump.LimitReachedResult)
                    {
                        case DoubleJump.LimitResult.LongStun:
                            self.Stun(doubleJump.StunTimers.Length > 1 ? doubleJump.StunTimers[1] : doubleJump.StunTimers[0] * 3);
                            break;
                        
                        case DoubleJump.LimitResult.Die:
                            self.PyroDeath();
                            break;
                        
                        case DoubleJump.LimitResult.ConsumeFood:
                            if (self.FoodInStomach >= doubleJump.FoodCost)
                            {
                                self.SubtractFood(doubleJump.FoodCost);
                            }
                            else
                            {
                                self.Stun(doubleJump.StunTimers[0]);
                                self.SetMalnourished(true);
                            }
                            break;
                    }
                    return;
                }
                self.PyroDeath();
            }

            static void StunTimer(int jumps, DoubleJump doubleJump, Player self)
            {
                if (doubleJump != null)
                {
                    if (doubleJump.StunTimers[0] > 0)
                    {
                        self.Stun(doubleJump.StunTimers[0] * (self.pyroJumpCounter - (doubleJump.JumpLimit[0] - 1)));
                    }
                    return;
                }
                self.Stun(60 * (self.pyroJumpCounter - (jumps - 1)));
            }

            c.GotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchLdcI4(60)
                );
            jump = c.CloneAndGoToNext(
                MoveType.After,
                x => x.MatchCallOrCallvirt(typeof(Creature).GetMethod(nameof(Creature.Stun)))
                );
            c.Emit(OpCodes.Ldloc, 12);
            c.EmitFeatureDelegate(doubleJump, StunTimer, true);
            c.Emit(OpCodes.Br, jump.MarkLabel());

            c.GotoNext(
                MoveType.After,
                x => x.MatchLdsfld(typeof(MoreSlugcats.MoreSlugcats).GetField(nameof(MoreSlugcats.MoreSlugcats.cfgArtificerExplosionCapacity))),
                x => x.MatchCallOrCallvirt(out _)
                );
            c.EmitFeatureDelegate(doubleJump, JumpLimit);

            c.GotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchCallOrCallvirt(typeof(Player).GetMethod(nameof(Player.PyroDeath)))
                );
            jump = c.CloneAndGoToNext(
                MoveType.After,
                x => x.MatchCallOrCallvirt(typeof(Player).GetMethod(nameof(Player.PyroDeath)))
                );
            c.EmitFeatureDelegate(doubleJump, DeathScenerio, true);
            c.Emit(OpCodes.Br, jump.MarkLabel());

            // Enable/disable parrying
            static bool CanParry(bool flag, DoubleJump doubleJump)
            {
                return flag && (doubleJump == null || doubleJump.Parry);
            }

            c.GotoNext(
                MoveType.After,
                x => x.MatchLdloc(0)
                );
            c.EmitFeatureDelegate(doubleJump, CanParry);

            EnumerateFloats(c, [
                8f, 6f, 6f
            ]);

            // Edit parry effects
            c.GotoNext(
                MoveType.After,
                x => x.MatchStloc(10)
                );
            jump = c.CloneAndGoToNext(
                x => x.MatchLdarg(0),
                x => x.MatchLdfld(out _),
                x => x.MatchLdsfld(typeof(SoundID).GetField(nameof(SoundID.Fire_Spear_Explode)))
                );
            c.EmitFeatureDelegate(doubleJump, CustomJumpEffect); //LATER: replace with its own local check
            c.Emit(OpCodes.Brtrue, jump.MarkLabel());

            // Change parry sound
            c.GotoNext(
                MoveType.After,
                x => x.MatchLdsfld(typeof(SoundID).GetField(nameof(SoundID.Fire_Spear_Explode)))
                );
            c.EmitFeatureDelegate(doubleJump, JumpSound);

            // Change soft limit on parry
            c.GotoNext(
                x => x.MatchStloc(12)
                );
            c.EmitFeatureDelegate(doubleJump, JumpSoftLimit);

            c.GotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchLdcI4(0x3C)
                );
            jump = c.CloneAndGoToNext(
                MoveType.After,
                x => x.MatchCallOrCallvirt(typeof(Creature).GetMethod(nameof(Creature.Stun)))
                );
            c.Emit(OpCodes.Ldloc, 12);
            c.EmitFeatureDelegate(doubleJump, StunTimer, true);
            c.Emit(OpCodes.Br, jump.MarkLabel());

            // And the other hard limit again
            c.GotoNext(
                MoveType.After,
                x => x.MatchLdsfld(typeof(MoreSlugcats.MoreSlugcats).GetField(nameof(MoreSlugcats.MoreSlugcats.cfgArtificerExplosionCapacity))),
                x => x.MatchCallOrCallvirt(out _)
                );
            c.EmitFeatureDelegate(doubleJump, JumpLimit);

            c.GotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchCallOrCallvirt(typeof(Player).GetMethod(nameof(Player.PyroDeath)))
                );
            jump = c.CloneAndGoToNext(
                MoveType.After,
                x => x.MatchCallOrCallvirt(typeof(Player).GetMethod(nameof(Player.PyroDeath)))
                );
            c.EmitFeatureDelegate(doubleJump, DeathScenerio, true);
            c.Emit(OpCodes.Br, jump.MarkLabel());
        }

        private void Player_Regurgitate(ILCursor c, ILContext il)
        {
            var craftFeature = il.GetFeature<Craftability, Player>(c, CraftFeature);
            static bool CanRegurgitate(bool isGourmand, Craftability craft)
            {
                return isGourmand || (craft != null && craft.RegurgitateList.objects.Count > 0);
            }

            c.GotoNext(
                MoveType.After,
                x => x.MatchCallOrCallvirt(typeof(Player).GetProperty(nameof(Player.isGourmand)).GetGetMethod())
                );
            c.EmitFeatureDelegate(craftFeature, CanRegurgitate);

            static AbstractPhysicalObject StomachItem(AbstractPhysicalObject gourmItem, Craftability craft, Player self)
            {
                if (craft != null)
                {
                    if (craft.TryGetRegurgitate(self, craft.RegurgitateList, out var result))
                    {
                        return result;
                    }
                    return null;
                }
                return gourmItem;
            }
            c.GotoNext(
                x => x.MatchStfld(typeof(Player).GetField(nameof(Player.objectInStomach)))
                );
            c.EmitFeatureDelegate(craftFeature, StomachItem, true);
        }


        public void OnApply()
        {
        }

        public void PostApply()
        {
        }
    }
}
