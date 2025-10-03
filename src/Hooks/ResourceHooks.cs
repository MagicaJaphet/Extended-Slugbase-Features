using DevInterface;
using ExtendedSlugbaseFeatures.Resources.Resources;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RWCustom;
using SlugBase;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static ExtendedSlugbaseFeatures.Helpers.AbstractPhysicalObjectHelpers;
using static ExtendedSlugbaseFeatures.Helpers.RoomSpecificScriptHelpers;

namespace ExtendedSlugbaseFeatures.Hooks
{
	public class ResourceHooks
	{
		private static bool inputMenuOpened;
		internal static InputUI inputDevUI;

		public static void Apply()
		{
			On.Player.checkInput += Player_checkInput;
			IL.Player.Update += Player_Update;
			IL.RainWorldGame.RawUpdate += RainWorldGame_RawUpdate;

			IL.Room.Loaded += Room_Loaded;
			On.PlacedObject.GenerateEmptyData += PlacedObject_GenerateEmptyData;
			On.DevInterface.ObjectsPage.CreateObjRep += ObjectsPage_CreateObjRep;
			On.DevInterface.ObjectsPage.DevObjectGetCategoryFromPlacedType += ObjectsPage_DevObjectGetCategoryFromPlacedType;
		}

		/// <summary>
		/// Add inputs remotely to <see cref="InputUI.InputHistory"/>.
		/// </summary>
		private static void Player_checkInput(On.Player.orig_checkInput orig, Player self)
		{
			orig(self);

			if (inputDevUI != null && inputDevUI.inputHistory != null && !inputDevUI.frameByFrame)
			{
				inputDevUI.inputHistory.AddInput(self.input[0]);
			}
		}

		private static void Player_Update(ILContext il)
		{
			try
			{
				ILCursor cursor = new(il);

				for (int i = 0; i < 2; i++)
				{
					if (cursor.TryGotoNext(MoveType.After,
						x => x.MatchLdfld<RainWorldGame>(nameof(RainWorldGame.devToolsActive))))
					{
						if (i == 1)
						{
							static bool IsDevToolsAndInputInterface(bool devTools)
							{
								return devTools && inputDevUI == null;
							}
							cursor.EmitDelegate(IsDevToolsAndInputInterface);
						}
					}
				}
			}
			catch (Exception ex)
			{
				UnityEngine.Debug.LogException(ex);
			}
		}

		private static void RainWorldGame_RawUpdate(ILContext il)
		{
			try
			{
				ILCursor cursor = new(il);

				if (cursor.TryGotoNext(MoveType.After,
					x => x.MatchLdfld<RainWorldGame>(nameof(RainWorldGame.devToolsActive))))
				{
					static bool IsDevToolsAndInputInterface(bool devTools)
					{
						return devTools && inputDevUI == null;
					}
					cursor.EmitDelegate(IsDevToolsAndInputInterface);

					if (cursor.TryGotoNext(MoveType.After,
						x => x.MatchLdstr("o"),
						x => x.MatchCallOrCallvirt<Input>(nameof(Input.GetKey)),
						x => x.MatchStfld<RainWorldGame>(nameof(RainWorldGame.oDown))))
					{
						cursor.Emit(OpCodes.Ldarg_0);
						static void InputConsole(RainWorldGame self)
						{
							if (self.devToolsActive)
							{
								if (Input.GetKey(KeyCode.F1) && !inputMenuOpened)
								{
									if (inputDevUI == null && ScriptTriggerRooms.Contains(self.Players.FirstOrDefault().Room.name))
									{
										foreach (var key in CustomCutscene.Registry.Keys)
										{
											List<CustomCutscene> roomScripts = [];
											if (CustomCutscene.Registry.TryGet(key, out var potentialScript) && potentialScript.TriggerRooms.Contains(self.Players.FirstOrDefault().Room.name) && potentialScript.triggered)
											{
												roomScripts.Add(potentialScript);
											}

											if (roomScripts.Count > 0)
												inputDevUI = new InputUI(self, roomScripts, self.Players.FirstOrDefault().Room.name);
											else
												UnityEngine.Debug.Log("No suitable cutscenes found to debug in Player room! Make sure to activate the script before you debug it.");
										}
									}
									else
									{
										inputDevUI?.Destroy();
										inputDevUI = null;
									}
								}
								inputMenuOpened = Input.GetKey(KeyCode.F1);
								if (inputDevUI != null)
								{
									inputDevUI.Update();
								}
							}
						}
						cursor.EmitDelegate(InputConsole);
					}
				}
			}
			catch (Exception ex)
			{
				UnityEngine.Debug.LogException(ex);
			}
		}

		private static void Room_Loaded(ILContext il)
		{
			try
			{
				ILCursor cursor = new(il);

				if (cursor.TryGotoNext(MoveType.Before,
					x => x.MatchLdloc(27), 
					x => x.MatchLdcI4(1),
					x => x.MatchAdd()))
				{
					cursor.MoveAfterLabels();
					cursor.Emit(OpCodes.Ldarg_0);
					cursor.Emit(OpCodes.Ldloc, 27);
					static void InsertObjects(Room self, int i)
					{
						if (self.roomSettings.placedObjects[i].type == Enums.ScriptTriggerBox && !self.updateList.Any(x => x is ScriptTrigger))
						{
							UnityEngine.Debug.Log("Attempting to add script box to room!");

							foreach (var key in CustomCutscene.Registry.Keys) {
								if (CustomCutscene.Registry.TryGet(key, out var possibleScript) && ScriptTriggerRooms.Contains(self.abstractRoom.name))
								{
									UnityEngine.Debug.Log($"Found {possibleScript.Type} script for {self.abstractRoom.name}!");
									PlacedObject.GridRectObjectData gridRect = self.roomSettings.placedObjects[i].data as PlacedObject.GridRectObjectData;
									self.AddObject(new ScriptTrigger(self, possibleScript, gridRect.Rect));
									break;
								}
							}
						}
					}
					cursor.EmitDelegate(InsertObjects);
				}
			}
			catch (Exception ex)
			{
				UnityEngine.Debug.LogException(ex);
			}
		}

		private static void PlacedObject_GenerateEmptyData(On.PlacedObject.orig_GenerateEmptyData orig, PlacedObject self)
		{
			orig(self);

			if (self.type == Enums.ScriptTriggerBox)
			{
				self.data = new PlacedObject.GridRectObjectData(self);
				return;
			}
		}

		private static void ObjectsPage_CreateObjRep(On.DevInterface.ObjectsPage.orig_CreateObjRep orig, ObjectsPage self, PlacedObject.Type tp, PlacedObject pObj)
		{
			orig(self, tp, pObj);

			if (pObj == null)
			{
				pObj = new PlacedObject(tp, null)
				{
					pos = self.owner.room.game.cameras[0].pos + Vector2.Lerp(self.owner.mousePos, new Vector2(-683f, 384f), 0.25f) + Custom.DegToVec(UnityEngine.Random.value * 360f) * 0.2f
				};
				self.RoomSettings.placedObjects.Add(pObj);
			}

			PlacedObjectRepresentation placeObj = null;
			if (pObj != null && tp == Enums.ScriptTriggerBox)
			{
				placeObj = new ScriptTriggerBoxRepresentation(self.owner, tp.ToString() + "_Rep", self, pObj, tp.ToString());
			}
			if (placeObj != null)
			{
				self.tempNodes.Add(placeObj);
				self.subNodes.Add(placeObj);
			}
		}

		/// <summary>
		/// Sort custom Dev Tools objects.
		/// </summary>
		private static ObjectsPage.DevObjectCategories ObjectsPage_DevObjectGetCategoryFromPlacedType(On.DevInterface.ObjectsPage.orig_DevObjectGetCategoryFromPlacedType orig, ObjectsPage self, PlacedObject.Type type)
		{
			if (type == Enums.ScriptTriggerBox)
				return ObjectsPage.DevObjectCategories.Gameplay;

			return orig(self, type);
		}
	}

	internal class InputUI
	{
		private RainWorldGame game;
		private List<CustomCutscene> roomScripts;
		private string roomName;

		public CustomCutscene SelectedScript { get; }

		internal InputHistory inputHistory;
		private FLabel frameToggled;
		private int lastFramesPerSecond;
		private bool frameToggle;
		public bool frameByFrame;
		private bool playerInputted;
		private bool insertInput;
		private Player.InputPackage lastInput;
		private bool saving;
		private FSprite savingPrompt;
		private FLabel savingLabel;
		private bool saveButton;

		public InputUI(RainWorldGame game, List<CustomCutscene> roomScripts, string roomName)
		{
			this.game = game;
			this.roomScripts = roomScripts;
			this.roomName = roomName;

			SelectedScript = roomScripts.FirstOrDefault();

			inputHistory = new InputHistory(SelectedScript, game, roomName);

			frameToggled = new FLabel(Custom.GetFont(), "Frame by frame toggled")
			{
				isVisible = false,
				color = Color.yellow,
				x = game.manager.rainWorld.options.ScreenSize.x / 2,
				y = game.manager.rainWorld.options.ScreenSize.y - 40f
			};
			Futile.stage.AddChild(frameToggled);

			savingPrompt = new FSprite("pixel")
			{
				isVisible = false,
				scaleX = game.manager.rainWorld.options.ScreenSize.x + 1f,
				scaleY = game.manager.rainWorld.options.ScreenSize.y + 1f,
				color = new(0.01f, 0.01f, 0.01f)
			};
			Futile.stage.AddChild(savingPrompt);
			savingLabel = new FLabel(Custom.GetFont(), "Attempting to save...")
			{
				isVisible = false,
				scale = 2f,
				x = game.manager.rainWorld.options.ScreenSize.x,
				y = game.manager.rainWorld.options.ScreenSize.y,
				alpha = 0.3f
			};
			Futile.stage.AddChild(savingLabel);
		}

		internal void Destroy()
		{
			frameToggled?.RemoveFromContainer();
			savingPrompt?.RemoveFromContainer();
			savingLabel?.RemoveFromContainer();

			inputHistory?.Destroy();
		}

		internal void Update()
		{
			if (inputHistory != null && Input.GetKey("s") && Input.GetKey(KeyCode.LeftControl) && !saveButton && !saving)
			{
				saving = true;
				CustomCutscene.TrySave(SelectedScript, inputHistory, ref saving);
			}
			savingLabel.isVisible = saving;
			savingPrompt.isVisible = saving;
			saveButton = Input.GetKey("s") && Input.GetKey(KeyCode.LeftControl);

			if (!saving && !Input.GetKey(KeyCode.LeftControl))
			{
				if (Input.GetKey(KeyCode.F2))
				{
					SelectedScript.Inputs.Clear();
					SelectedScript.InputTimers.Clear();
					inputHistory?.Destroy();
					inputHistory = new(SelectedScript, game, roomName);

					foreach (AbstractCreature player in game.Players)
					{
						if (player.realizedCreature is Player actualPlayer && SelectedScript.PlayerInitialTriggerPos.TryGetValue(actualPlayer.playerState.playerNumber, out var pos))
						{
							actualPlayer.SuperHardSetPosition(pos);
							actualPlayer.firstChunk.vel = Vector2.zero;
							inputHistory?.Destroy();
							inputHistory = new(SelectedScript, game, roomName);
							return;
						}
					}
				}
				if (Input.GetKey(KeyCode.F3) && !frameToggle)
				{
					frameByFrame = !frameByFrame;
					frameToggled.isVisible = frameByFrame;

					if (!frameByFrame && inputHistory != null)
					{
						inputHistory.addedVariable = false;
						inputHistory.inputLabels.Remove(inputHistory.variableLabel);
						inputHistory.variableLabel.RemoveFromContainer();
					}
				}
				frameToggle = Input.GetKey(KeyCode.F3);

				// Change this to capture inputs from directly within Player.Update due to slight desync
				Player.InputPackage currentInput = RWInput.PlayerInput(0);
				if (!frameByFrame && (currentInput.AnyInput || Input.GetKey(KeyCode.Tab)))
				{
					game.framesPerSecond = lastFramesPerSecond;
					//inputHistory?.AddInput((game.FirstAlivePlayer.realizedCreature as Player).input[0]);
				}
				else if (frameByFrame)
				{
					game.framesPerSecond = 0;
					if (Input.GetKey(KeyCode.Tab) && !insertInput)
					{
						game.framesPerSecond = lastFramesPerSecond;
						if (inputHistory != null)
						{
							inputHistory.addedVariable = false;
							inputHistory.inputLabels.Remove(inputHistory.variableLabel);
							inputHistory.variableLabel.RemoveFromContainer();
							inputHistory.AddInput(inputHistory.variableInput);
						}
					}
					insertInput = Input.GetKey(KeyCode.Tab);
				
					if (inputHistory != null && (!InputHistory.CompareInputs(currentInput, lastInput) || !playerInputted))
					{
						inputHistory.AddVariableInput(currentInput);
					}
					playerInputted = Input.anyKey;
				}
				else
				{
					if (game.framesPerSecond != 0)
						lastFramesPerSecond = game.framesPerSecond;
					game.framesPerSecond = 0;
				}
				if (!frameByFrame || playerInputted)
					lastInput = currentInput;

				inputHistory?.Update();
			}
			else
			{
				game.framesPerSecond = 0;
			}
		}

		public bool AnyInputPressed(Player player)
		{
			if (player != null)
			{
				var i = player.input[0];
				return i.x != 0 || i.y != 0 || i.jmp || i.thrw || i.pckp || i.mp || i.crouchToggle || i.spec;
			}
			return false;
		}

		internal class InputHistory
		{
			private CustomCutscene inputScript;
			private RainWorldGame game;
			public string roomName;
			public List<FLabel> inputLabels = [];
			public List<Player.InputPackage> recordedInputs = [];
			public List<float> recordedTimings = [];
			public Player.InputPackage variableInput = default;
			public bool addedVariable;
			public FLabel variableLabel;
			private int variableTimer;

			internal InputHistory(CustomCutscene inputScript, RainWorldGame game, string roomName)
			{
				this.inputScript = inputScript;
				this.game = game;
				this.roomName = roomName;

				for (int i = 0; i < inputScript.PlayerInitialTriggerPos.Count; i++)
				{
					if (inputScript.Inputs.TryGetValue(roomName, out var inputList) && inputList.TryGetValue(i, out var inputs) && inputScript.InputTimers.TryGetValue(roomName, out var timingList) && timingList.TryGetValue(i, out var timings))
					{
						for (int j = 0; j < inputs.Count; j++)
						{
							if (timings.Count > j)
								AddInput(inputs[j], timings[j]);
							else
								AddInput(inputs[j]);
						}
					}
				}
			}

			public void AddInput(Player.InputPackage input, float timer = 1)
			{
				if (recordedInputs.Count > 0 && CompareInputs(recordedInputs[0], input))
				{
					int prev = 0;

					recordedTimings[prev] += timer;
					string str = GetInputString(input, recordedTimings[prev]);

					inputLabels[0].text = str;
				}
				else
				{
					recordedInputs.Insert(0, input);
					recordedTimings.Insert(0, timer);
					string str = GetInputString(input, timer);
					FLabel label = new(Custom.GetFont(), str) { x = 5f, anchorX = 0, anchorY = 1 };
					inputLabels.Insert(0, label);

					Futile.stage.AddChild(label);
				}
			}

			internal void AddVariableInput(Player.InputPackage input)
			{
				variableInput = input;

				if (!addedVariable)
				{
					addedVariable = true;
					string str = GetInputString(variableInput, variableTimer + 1);
					variableLabel = new(Custom.GetFont(), str) { x = 5f, anchorX = 0, anchorY = 1 };
					inputLabels.Insert(0, variableLabel);

					Futile.stage.AddChild(variableLabel);
				}
				else
				{
					string str = GetInputString(variableInput, variableTimer + 1);
					variableLabel.text = str;
				}
			}

			public static bool CompareInputs(Player.InputPackage a, Player.InputPackage b)
			{
				return a.Equals(b);
			}

			private string GetInputString(Player.InputPackage input, float timer)
			{
				string str = "";
				str += $"{timer} ";
				str += input.x == -1 ? "L" : input.x == 1 ? "R" : " ";
				str += input.y == -1 ? "D" : input.y == 1 ? "U" : " ";
				str += input.jmp ? "J" : " ";
				str += input.pckp ? "P" : " ";
				str += input.thrw ? "T" : " ";
				str += input.mp ? "M" : " ";
				str += input.spec ? "S" : " ";
				return str;
			}

			internal void Destroy()
			{
				inputLabels.ForEach(x => x?.RemoveFromContainer());
				inputLabels = null;
			}

			internal void Update()
			{
				for (int i = inputLabels.Count - 1; i >= 0; i--)
				{
					inputLabels[i].y = (inputLabels[i].FontLineHeight + 2f) * (i + 1);
				}
			}
		}
	}

	internal class ScriptTrigger : UpdatableAndDeletable
	{
		public bool triggered;
		public IntRect triggerSpot;
		public List<Player> Players { get; } = [];
		public Dictionary<Player, int> CurrentPhase { get; } = [];
		public Dictionary<int, List<Player.InputPackage>> Inputs { get; private set; } = [];
		public Dictionary<int, List<int>> Timers { get; private set; } = [];

		public CustomCutscene script;
		private bool spawnedObjects;
		private bool foundPlayers;
		public float sceneTimer;
		private bool inputSet;

		public ScriptTrigger(Room room, CustomCutscene script, IntRect rect = default)
		{
			this.script = script;
			this.room = room;
			triggerSpot = rect;
			if (room != null)
				UnityEngine.Debug.Log($"ScriptTrigger created in {room.abstractRoom.name}");
		}

		public override void Update(bool eu)
		{
			base.Update(eu);

			if (room != null && script != null && room.abstractRoom.world.game?.manager.fadeToBlack < 1f)
			{
				if (!foundPlayers)
				{
					foreach (Player player in room.PlayersInRoom)
					{
						if (player != null && !Players.Contains(player))
						{
							Players.Add(player);
							CurrentPhase.Add(player, 0);
							UnityEngine.Debug.Log($"Found players for custom script {Players.Count}");
							if (!script.PlayerInitialTriggerPos.ContainsKey(player.playerState.playerNumber))
								script.PlayerInitialTriggerPos.Add(player.playerState.playerNumber, player.firstChunk.pos);
							else
								script.PlayerInitialTriggerPos[player.playerState.playerNumber] = player.firstChunk.pos;
						}
					}

					//if (script.PlayerValues.Count > 0 && script.PlayerValues.TryGetValue(room.abstractRoom.name, out var playerValues))
					//{
					//	UnityEngine.Debug.Log("Found player values!");
					//	foreach (Player player in Players)
					//	{
					//		if (playerValues.TryGetValue(player.playerState.playerNumber, out var values))
					//		{
					//			UnityEngine.Debug.Log("AAAAAAAAAAAA");
					//			foreach (var key in values.Keys)
					//			{
					//				UnityEngine.Debug.Log($"Attempting to set {key} for Player {player.playerState.playerNumber}");
					//				typeof(Player).GetField(key).SetValue(player, values[key]);
					//			}
					//		}
					//	}
					//}
					foundPlayers = true;
				}

				if (foundPlayers && !spawnedObjects && script.SpawnObjects.Count > 0 && script.SpawnObjects.TryGetValue(room.abstractRoom.name, out var objects))
				{
					UnityEngine.Debug.Log("Attempting to spawn objects!");
					spawnedObjects = true;
					foreach (Player player in Players)
					{
						if (objects.TryGetValue(player.playerState.playerNumber, out var objectList))
						{
							foreach (var obj in objectList)
							{
								if (obj.TrySpawn(room.abstractRoom, out var abstractObj))
								{
									room.abstractRoom.AddEntity(abstractObj);

									if (obj.SpawnPos != null)
										abstractObj.pos = new(room.abstractRoom.index, obj.SpawnPos.x, obj.SpawnPos.y, -1);

									if (abstractObj.realizedObject == null)
										abstractObj.RealizeInRoom();

									if (obj.Spawn == AbstractData.SpawnType.Grasp && Players.Count > 0 && Players[0].FreeHand() != -1)
										Players[0].SlugcatGrab(abstractObj.realizedObject, Players[0].FreeHand());
								}
								else
								{
									UnityEngine.Debug.Log($"Failed to spawn {obj.ObjectType}");
								}
							}
						}
					}
				}

				if (!triggered)
				{
					triggered = script.Type == CustomCutscene.CutsceneType.Intro || Players.Count > 0 && IsPlayerWithinScriptTrigger(Players[0]);
					if (triggered)
					{
						script.triggered = true;
					}
				}

				if (triggered)
				{
					if (!inputSet)
					{
						UnityEngine.Debug.Log("Script triggered, attempting to set inputs");
						if (script.Inputs.TryGetValue(room.abstractRoom.name, out var inputDict))
							Inputs = inputDict;
						else
							Plugin.Logger.LogInfo($"No input information found for {room.abstractRoom.name}!");
						if (script.InputTimers.TryGetValue(room.abstractRoom.name, out var timerDict))
							Timers = timerDict;

						sceneTimer = 0;

						if (Timers.Count > 0 || Inputs.Count > 0)
						{
							foreach (Player player in Players)
							{
								player.controller = new ScriptController(this, player, player.playerState.playerNumber);
							}
						}
						inputSet = true;
					}

					if (Timers.Count > 0 && Inputs.Count > 0 && inputSet)
					{
						sceneTimer++;
						UnityEngine.Debug.Log(sceneTimer);

						foreach (Player player in Players)
						{
							int pNum = player.playerState.playerNumber;
							if (Timers.TryGetValue(pNum, out var timings) && CurrentPhase.TryGetValue(player, out var phase))
							{
								if (timings.Count > phase && sceneTimer >= timings[phase] && phase < timings.Count)
								{
									CurrentPhase[player]++;
									UnityEngine.Debug.Log("Advancing phase...");
								}
								else if (phase++ >= timings.Count && sceneTimer > script.ScriptDuration)
								{
									player.controller = null;
									Destroy();
								}
							}
						}
					}
					else if (inputSet)
					{
						Destroy();
					}
				}
			}
		}

		public override void Destroy()
		{
			base.Destroy();

			UnityEngine.Debug.Log($"Destroying {script.ID.value}!");
		}

		private bool IsPlayerWithinScriptTrigger(Player player)
		{
			return triggerSpot.Includes(player.abstractCreature.pos.Tile);
		}

		public class ScriptController : Player.PlayerController
		{
			private ScriptTrigger owner;
			private Player player;
			private int pNum;

			public ScriptController(ScriptTrigger owner, Player player, int pNum)
			{
				this.owner = owner;
				this.player = player;
				this.pNum = pNum;
			}

			public override Player.InputPackage GetInput()
			{
				if (owner.Inputs.TryGetValue(pNum, out var inputs) && owner.CurrentPhase.TryGetValue(player, out var phase) && inputs.Count > phase)
				{
					if (inputs[phase].jmp)
					{
						player.wantToJump = 1;
					}
					return inputs[phase];
				}

				return default;
			}
		}
	}

	public class ScriptTriggerBoxRepresentation : GridRectObjectRepresentation
	{
		public ScriptTriggerBoxRepresentation(DevUI owner, string IDstring, DevUINode parentNode, PlacedObject pObj, string name) : base(owner, IDstring, parentNode, pObj, name)
		{

		}

		public override void Refresh()
		{
			base.Refresh();
		}
	}
}