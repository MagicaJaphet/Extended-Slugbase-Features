using System;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using MoreSlugcats;
using MonoMod.RuntimeDetour;
using RWCustom;
using SlugBase;
using Watcher;
using System.Linq;
using SlugBase.Features;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using static ExtendedSlugbaseFeatures.Resources;

namespace ExtendedSlugbaseFeatures
{
	/// <summary>
	/// Handles various hooks to other parts of the game with <see cref="SlugcatStats.Name"/> checks.
	/// </summary>
	public class WorldHooks
	{
		public static void Apply()
		{
			On.Room.Loaded += IntroCutsceneHandler;
			IL.Ghost.Update += Ghost_Update;
			IL.Room.Loaded += Room_Loaded;
			_ = new Hook(typeof(SaveState).GetProperty(nameof(SaveState.CanSeeVoidSpawn), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public).GetGetMethod(), SpirituallyEnlightened);

			On.RainWorldGame.TryGetPlayerStartPos += RainWorldGame_TryGetPlayerStartPos;

			_ = new Hook(typeof(StoryGameSession).GetProperty(nameof(StoryGameSession.slugPupMaxCount), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).GetGetMethod(), SpawnSlugPups);
		}

		/// <summary>
		/// Allows <see cref="SlugBaseCharacter"/> to create a <see cref="RoomSpecificScript"/> to control the slugcat and spawn external objects for a period of time.
		/// </summary>
		private static void IntroCutsceneHandler(On.Room.orig_Loaded orig, Room self)
		{
			orig(self);

			if (self?.game != null && self.game.IsStorySession && self.game.GetStorySession.saveState.cycleNumber == 0 && ExtFeatures.introCutsceneDict.TryGet(self.game, out var introVariables) && GameFeatures.StartRoom.TryGet(self.game, out string[] rooms) && rooms.Contains(self.abstractRoom.name) && introVariables.ContainsKey(self.abstractRoom.name))
			{
				self.AddObject(new MovementScript(self, introVariables[self.abstractRoom.name]));
			}
		}

		/// <summary>
		/// Updatable movement script which transcribes the json file's inputs.
		/// </summary>
		internal class MovementScript : UpdatableAndDeletable
		{
			private int timer;
			private Player player;
			private int inputIndex;
			private List<Player.InputPackage> introCutsceneInputs;
			private List<AbstractPhysicalObject> introCutsceneObjects;
			private List<int> introCutsceneTimedInputs;
			private bool parsedTiming;
			private bool parsedInputs;
			private bool parsedObjects;

			public MovementScript(Room room, Dictionary<Type, object> roomDict)
			{
				this.room = room;
				inputIndex = 0;
				timer = 0;

				foreach (Type type in roomDict.Keys)
				{
					if (type == typeof(Player.InputPackage))
					{
						object inputCanidates = roomDict[type];
						if (inputCanidates != null && inputCanidates is List<Player.InputPackage> inputs)
						{
							introCutsceneInputs = inputs;
						}
						parsedInputs = true;
					}
					else if (type == typeof(int))
					{
						object timingCanidates = roomDict[type];
						if (timingCanidates != null && timingCanidates is List<int> timings)
						{
							introCutsceneTimedInputs = timings;
						}
						parsedTiming = true;
					}
					else if (type == typeof(AbstractPhysicalObject))
					{
						object objectCanidates = roomDict[type];
						if (objectCanidates != null && objectCanidates is Dictionary<AbstractPhysicalObject.AbstractObjectType, Dictionary<string, object>> objectDict)
						{
							introCutsceneObjects = [];
							foreach (AbstractPhysicalObject.AbstractObjectType objType in objectDict.Keys)
							{
								if (ParseDictToObject(out AbstractPhysicalObject obj, room, objType, objectDict[objType]))
								{
									introCutsceneObjects.Add(obj);
								}
							}
							parsedObjects = true;
						}
					}
				}
			}

			public override void Update(bool eu)
			{
				base.Update(eu);

				if (parsedTiming && parsedObjects && parsedInputs)
				{
					if (player == null)
					{
						player = room.PlayersInRoom.FirstOrDefault();
						if (player != null)
						{
							if (introCutsceneInputs != null)
							{
								player.controller = new MoveController(this);
								UnityEngine.Debug.Log("Found player and set controller to movement inputs!");
							}

							if (introCutsceneObjects != null)
							{
								foreach (var obj in introCutsceneObjects)
								{
									if (obj != null)
									{
										room.abstractRoom.AddEntity(obj);
										obj.RealizeInRoom();
										if (player.FreeHand() != -1 && obj.realizedObject != null)
										{
											obj.realizedObject.firstChunk.pos = player.firstChunk.pos;
											player.SlugcatGrab(obj.realizedObject, player.FreeHand());
										}
									}
								}
								UnityEngine.Debug.Log("Found player and set spawn objects!");
							}
						}
					}

					if (player != null && player.controller is MoveController && introCutsceneInputs != null)
					{
						if (inputIndex < introCutsceneInputs.Count)
						{
							timer++;
							if (timer >= introCutsceneTimedInputs[inputIndex])
							{
								timer = 0;
								inputIndex++;
							}
						}
						else
						{
							Destroy();
						}
					}
				}
			}

			public override void Destroy()
			{
				base.Destroy();
				introCutsceneInputs = null;
				introCutsceneTimedInputs = null;
				player.controller = null;
			}

			internal Player.InputPackage GetInput()
			{
				if (inputIndex < introCutsceneInputs.Count)
					return introCutsceneInputs[inputIndex];

				return default;
			}

			internal class MoveController : Player.PlayerController
			{
				private MovementScript owner;

				public MoveController(MovementScript owner)
				{
					this.owner = owner;
				}

				public override Player.InputPackage GetInput()
				{
					return owner.GetInput();
				}
			}
		}

		/// <summary>
		/// Allows <see cref="SlugBaseCharacter"/> to talk to <see cref="Ghost"/> without the mark.
		/// </summary>
		private static void Ghost_Update(ILContext il)
		{
			try
			{
				ILCursor cursor = new(il);

				static bool CanTalkToGhosts(bool isSlugcat, Ghost self)
				{
					return isSlugcat || (self.room.game.HasFeature(ExtFeatures.enlightenedState, out bool flag) && flag);
				}

				// if (this.room.game.session is StoryGameSession && ((this.room.game.session as StoryGameSession).saveState.deathPersistentSaveData.theMark || (ModManager.MSC && this.room.game.StoryCharacter == MoreSlugcatsEnums.SlugcatStatsName.Saint) || (ModManager.Watcher && this.room.game.StoryCharacter == WatcherEnums.SlugcatStatsName.Watcher)))
				if (cursor.MoveToNextSlugcat(typeof(MoreSlugcatsEnums.SlugcatStatsName).GetField(nameof(MoreSlugcatsEnums.SlugcatStatsName.Saint))))
				{

					cursor.ImplementILCodeAssumingLdarg0(CanTalkToGhosts);

					// if (this.room.game.session is StoryGameSession && ((this.room.game.session as StoryGameSession).saveState.deathPersistentSaveData.theMark || (ModManager.MSC && this.room.game.StoryCharacter == MoreSlugcatsEnums.SlugcatStatsName.Saint) || (ModManager.Watcher && this.room.game.StoryCharacter == WatcherEnums.SlugcatStatsName.Watcher)))
					if (cursor.MoveToNextSlugcat(typeof(WatcherEnums.SlugcatStatsName).GetField(nameof(WatcherEnums.SlugcatStatsName.Watcher))))
					{
						cursor.ImplementILCodeAssumingLdarg0(CanTalkToGhosts);
					}
				}
			}
			catch (Exception ex)
			{
				UnityEngine.Debug.LogException(ex);
			}
		}

		/// <summary>
		/// Allows <see cref="SlugBaseCharacter"/> to set the character's starting position in a room, in the room tiles measurement.
		/// </summary>
		internal static bool RainWorldGame_TryGetPlayerStartPos(On.RainWorldGame.orig_TryGetPlayerStartPos orig, string room, out IntVector2 pos)
		{
			if (Custom.rainWorld.inGameSlugCat != null && SlugBaseCharacter.TryGet(Custom.rainWorld.inGameSlugCat, out var character) && ExtFeatures.possibleSpawnPositons.TryGet(character, out var startRooms) && startRooms.ContainsKey(room))
			{
				pos = startRooms[room];
				return pos != null;
			}

			return orig(room, out pos);
		}

		/// <summary>
		/// Allows <see cref="SlugBaseCharacter"/> to see void spawn without the mark.
		/// </summary>
		internal static bool SpirituallyEnlightened(Func<SaveState, bool> orig, SaveState save)
		{
			return orig(save) || Custom.rainWorld.processManager.currentMainLoop is RainWorldGame game && (game.HasFeature(ExtFeatures.enlightenedState, out bool flag) && flag);
		}

		/// <summary>
		/// Allows <see cref="SlugBaseCharacter"/> to use the Spearmaster broadcast mechanic, if the <see cref="CollectToken.whiteToken"/> object exists in it's world state, and change if <see cref="KarmaFlower"/> spawn.
		/// </summary>
		internal static void Room_Loaded(ILContext il)
		{
			try
			{
				ILCursor cursor = new(il);

				// Spearmaster broadcasts
				if (cursor.TryGotoNext(
					MoveType.After,
					x => x.MatchCallvirt(out _),
					x => x.MatchLdsfld<MoreSlugcatsEnums.SlugcatStatsName>(nameof(MoreSlugcatsEnums.SlugcatStatsName.Spear)),
					x => x.MatchCall(out _)
					))
				{
					ILLabel nextJump = (ILLabel)cursor.Next.Operand;

					cursor.Emit(OpCodes.Brfalse_S, nextJump);
					cursor.Emit(OpCodes.Ldarg_0);
					static bool HasBroadcasts(Room self)
					{
						return !ExtFeatures.canProcessWhiteTokens.TryGet(self.game, out var slug) || !slug;
					}
					cursor.EmitDelegate(HasBroadcasts);

				}

				// if (this.game.StoryCharacter != SlugcatStats.Name.Red && (!(this.game.session is StoryGameSession) || !(this.game.session as StoryGameSession).saveState.ItemConsumed(this.world, true, this.abstractRoom.index, num21)))
				cursor.MoveToNextSlugcat(typeof(SlugcatStats.Name).GetField(nameof(SlugcatStats.Name.Red)));

				cursor.Emit(OpCodes.Ldarg_0);
				static bool DontSpawnKarmaFlowers(bool isRed, Room self)
				{
					return isRed && (!ExtFeatures.shouldSpawnKarmaFlowers.TryGet(self.game, out bool canSpawn) || canSpawn);
				}
				cursor.EmitDelegate(DontSpawnKarmaFlowers);
			}
			catch (Exception ex)
			{
				UnityEngine.Debug.LogException(ex);
			}
		}

		/// <summary>
		/// Allows <see cref="SlugBaseCharacter"/> to spawn slugpups in their campaign.
		/// </summary>
		internal static int SpawnSlugPups(Func<StoryGameSession, int> orig, StoryGameSession self)
		{
			if (ModManager.MSC && self.game != null && ExtFeatures.maxSlugpupSpawns.TryGet(self.game, out int maxPups))
			{
				return maxPups;
			}

			return orig(self);
		}
	}
}