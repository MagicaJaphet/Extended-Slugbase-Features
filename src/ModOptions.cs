using ExtendedSlugbaseFeatures.Helpers;
using ExtendedSlugbaseFeatures.Resources;
using Menu;
using Menu.Remix.MixedUI;
using RWCustom;
using SlugBase;
using SlugBase.Features;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Text.RegularExpressions;
using UnityEngine;
using static ExtendedSlugbaseFeatures.Helpers.RoomSpecificScriptHelpers.CustomCutscene;

namespace ExtendedSlugbaseFeatures;
internal class ModOptions : OptionInterface
{
	private OpTab _onlyTab;
	private static OpScrollBox _jsonBox;
	private OpScrollBox _jsonDescriptionBox;
	private OpSimpleButton applyChangesButton;
	private OpSimpleButton resetChangesButton;
	internal OpSimpleButton removeFeatureButton;
	private static List<UIelement> _temporaryElements = [];
	private static List<UIelement> _temporaryInputElements = [];
	private string _currentJSONFile;
	private bool anyJSONChanges;
	private Dictionary<string, object> _lastFeaturesDict = [];
	private Dictionary<string, object> _currentFeaturesDict = [];
	private OpSelectableGroup _selectableGroup;
	internal static List<FeatureInfo> _allFeatures = [];
	private bool firstScroll;

	internal struct FeatureInfo
	{
		private Feature feature;
		public string id;
		public string modOrigin;

		/// <summary>
		/// Specifies what derrived type of <see cref="Feature{T}"/> the feature actually is.
		/// </summary>
		public Type featureType;

		/// <summary>
		/// Specified what the <see cref="Feature{T}"/> generic is.
		/// </summary>
		public Type genericArgument;
		public int inputFields;
		internal object defaultValues;
		internal string altEnableText;
		internal string[] inputFieldNames;
		internal string[] inputFieldDescriptions;
		internal bool hideFeature;

		public FeatureInfo(Feature feature, string originMod) : this()
		{
			this.feature = feature;
			id = feature.ID;
			modOrigin = originMod;
			featureType = feature.GetType().GetGenericTypeDefinition();
			genericArgument = feature.GetType().GetGenericArguments()[0];
			inputFields = 1;
		}
	}

	public static ModOptions Instance { get; } = new();

	internal static float Margin { get; } = 10f;
	internal static Vector2 ButtonSize { get; } = new(100f, 30f);
	private float ValueEditArea { get; } = 200f;

	public static void RegisterOI()
	{
		if (MachineConnector.GetRegisteredOI(Plugin.MOD_ID) != Instance)
		{
			MachineConnector.SetRegisteredOI(Plugin.MOD_ID, Instance);
		}
	}

	// Option values
	public static Configurable<string> SelectedSlugcat { get; } = Instance.config.Bind(nameof(SelectedSlugcat), "", new ConfigurableInfo(
	   "Enable a dropdown of all enabled slugbase characters.",
	   null, "", "Changes selected slugbase character."));

	public ModOptions()
	{
	}

	public override void Initialize()
	{
		base.Initialize();

		_onlyTab = new OpTab(this);
		Tabs = [_onlyTab];

		var tabContainer = new OpContainer(new Vector2(0, 0));
		_onlyTab.AddItems(tabContainer);

		var slugBaseCharacters = SlugBase.SlugBaseCharacter.Registry.Keys;

		if (slugBaseCharacters.Count() > 0)
		{
			// Selector combo box
			if (!slugBaseCharacters.Any(slugcat => slugcat.value == SelectedSlugcat.Value)) SelectedSlugcat.Value = slugBaseCharacters.First().value;
			OpComboBox slugBaseSelector = new(SelectedSlugcat, new(), 300f, slugBaseCharacters.Select(slugcat => new ListItem(slugcat.value)).ToList())
			{
				description = "Change the slugbase character to edit the features for."
			};
			slugBaseSelector.SetPos(new(Margin, _onlyTab.CanvasSize.y - Margin - slugBaseSelector.size.y));
			slugBaseSelector.OnChange += SlugBaseSelector_OnChange;

			_jsonBox = new OpScrollBox(new(Margin, Margin + ValueEditArea), _onlyTab.CanvasSize - new Vector2(Margin * 2f, Margin * 3f + ValueEditArea + slugBaseSelector.size.y), 0f);

			_jsonDescriptionBox = new OpScrollBox(new(Margin, Margin), new(ValueEditArea - Margin, ValueEditArea - Margin), 0f);

			applyChangesButton = new(new(_onlyTab.CanvasSize.x - Margin - ButtonSize.x, Margin), ButtonSize, Translate("Apply"))
			{
				description = Translate("Apply any unsaved changes to the slugcat's JSON file.")
			};
			applyChangesButton.OnUpdate += () =>
			{
				applyChangesButton.greyedOut = !anyJSONChanges;
			};
			applyChangesButton.OnClick += (UIfocusable trigger) =>
			{
				UnityEngine.Debug.Log(Newtonsoft.Json.JsonConvert.SerializeObject(_currentFeaturesDict, Newtonsoft.Json.Formatting.Indented));
				if (!string.IsNullOrEmpty(_currentJSONFile) && File.Exists(_currentJSONFile))
				{
					var slugbaseDict = JsonConverter.ToDictionary(JsonAny.Parse(File.ReadAllText(_currentJSONFile)).AsObject());
					slugbaseDict["features"] = _currentFeaturesDict;
					File.WriteAllText(_currentJSONFile, Newtonsoft.Json.JsonConvert.SerializeObject(slugbaseDict, Newtonsoft.Json.Formatting.Indented));

					_lastFeaturesDict = _currentFeaturesDict;
					anyJSONChanges = false;
					RefreshJSONFeatures();
				}
			};

			resetChangesButton = new(new(applyChangesButton.pos.x - Margin - ButtonSize.x, Margin), ButtonSize, Translate("Reset"))
			{
				description = Translate("Reset all unsaved changes, reverting back to the last state of the slugcat's JSON file.")
			};
			resetChangesButton.OnUpdate += () =>
			{
				resetChangesButton.greyedOut = !anyJSONChanges;
			};
			resetChangesButton.OnClick += (UIfocusable trigger) =>
			{
				_currentFeaturesDict = _lastFeaturesDict;
				anyJSONChanges = false;
				RefreshJSONFeatures();
			};

			removeFeatureButton = new(new(resetChangesButton.pos.x - Margin - ButtonSize.x, Margin), ButtonSize, Translate("Remove"))
			{
				description = Translate("Removes feature from the slugcat's JSON file.")
			};
			removeFeatureButton.OnUpdate += () =>
			{
				if (_selectableGroup?.selected == null)
				{
					removeFeatureButton.greyedOut = true;
					return;
				}
				removeFeatureButton.greyedOut = !_selectableGroup.selected.enabled && !_currentFeaturesDict.ContainsKey(_selectableGroup.selected.signalText);
			};
			removeFeatureButton.OnClick += (UIfocusable trigger) =>
			{
				if (_selectableGroup?.selected != null && _currentFeaturesDict.ContainsKey(_selectableGroup.selected.signalText))
				{
					anyJSONChanges = true;
					_currentFeaturesDict.Remove(_selectableGroup.selected.signalText);
					_selectableGroup.selected.Enable(false);
					_selectableGroup.selected = null;
					ClearJSONFeatureSettings();
				}
			};

			_onlyTab.AddItems([slugBaseSelector, _jsonBox, _jsonDescriptionBox, applyChangesButton, resetChangesButton, removeFeatureButton]);

			var apiFiles = AssetManager.ListDirectory("text", includeAll: true);
			foreach (var apiFile in apiFiles.Where(file => Path.GetFileName(file).ToLower() == "slugbaseapi.json"))
			{
				var jsonDict = JsonConverter.ToDictionary(JsonAny.Parse(File.ReadAllText(apiFile)).AsObject());
				
				foreach (var mod in jsonDict.Keys)
				{
					if (jsonDict[mod] is Dictionary<string, object> innerFeatures)
					{
						foreach (var feature in innerFeatures.Keys)
						{
							if (!string.IsNullOrEmpty(_allFeatures.Find(x => x.id == feature).id))
							{
								int index = _allFeatures.IndexOf(_allFeatures.Find(x => x.id == feature));
								FeatureInfo featureInfo = _allFeatures[index];
								UnityEngine.Debug.Log(featureInfo.id);

								if (innerFeatures[feature] is Dictionary<string, object> keyValuePairs)
								{
									foreach (var key in keyValuePairs.Keys)
									{
										switch (key)
										{
											case "default_value":
												featureInfo.defaultValues = keyValuePairs[key];
												break;

											case "input_fields":
												featureInfo.inputFields = Convert.ToInt32(keyValuePairs[key]);
												break;

											case "input_field_names":
												if (ConvertJSONObjectToArray(keyValuePairs[key], out string[] names))
												{
													featureInfo.inputFieldNames = names;
												}
												break;

											case "input_field_descriptions":

												if (ConvertJSONObjectToArray(keyValuePairs[key], out string[] descs))
												{
													featureInfo.inputFieldDescriptions = descs;
												}
												break;

											case "hide":
												featureInfo.hideFeature = true;
												break;

											default:
												ExtSlugbaseAPI.HandleAdditionalKeys(key);
												break;
										}
									}
									_allFeatures[index] = featureInfo;
								}
							}
						}
					}
				}
			}

			RefreshJSONFeatures();
		}
		else
		{
			OpLabelLong missingSlugcats = new(new(_onlyTab.CanvasSize.x / 2f, _onlyTab.CanvasSize.y / 2f), new(_onlyTab.CanvasSize.x - (Margin * 2f), 100f), Translate("No Slugbase characters to edit found! Load one or more slugbase characters to change their JSON features!"), alignment: FLabelAlignment.Center);
		}
	}

	private bool ConvertJSONObjectToArray<T>(object jsonObject, out T[] array)
	{
		array = null;
		if (jsonObject is List<object> objList)
		{
			array = new T[objList.Count];
			for (int i = 0; i < objList.Count; i++)
			{
				if (Convert.ChangeType(objList[i], typeof(T)) is T a)
				{
					array[i] = a;
				}
			}
		}
		return array != null;
	}

	private void SlugBaseSelector_OnChange()
	{
		foreach (var element in _temporaryElements)
		{
			_onlyTab?.RemoveItems(element);
			element.scrollBox?._RemoveFromScrollBox();
			if (element is UIconfig config)
				config.Unload();
			element.Deactivate();
		}
		_temporaryElements.Clear();

		RefreshJSONFeatures();
	}

	public void RefreshJSONFeatures()
	{
		ClearJSONFeatureSettings();
		var files = AssetManager.ListDirectory("slugbase", includeAll: true);

		foreach (var file in files.Where(file => file.EndsWith(".json")))
		{
			if (SlugBaseCharacter.Registry.IsMostRecent(file))
			{
				try
				{
					file.Replace('/', Path.DirectorySeparatorChar);

					var jsonValue = JsonAny.Parse(File.ReadAllText(file)).AsObject();
					if (jsonValue.TryGet("id")?.TryString() is string slugcatName && slugcatName == SelectedSlugcat.Value)
					{
						_currentJSONFile = file;

						var jsonDict = JsonConverter.ToDictionary(jsonValue);
						if (jsonDict.TryGetValue("features", out var features) && features is Dictionary<string, object> featuresDict)
						{
							_lastFeaturesDict = featuresDict;
							_currentFeaturesDict = featuresDict;
							_selectableGroup = new OpSelectableGroup(this, _jsonBox);

							float offset = Margin;
							// Grab all of the valid Feature types
							foreach (var featureType in from asm in AppDomain.CurrentDomain.GetAssemblies() where !AbstractPhysicalObjectHelpers.dllBlacklist.Contains(asm.GetName().Name)
														from type in asm.GetTypes() where typeof(Feature).IsAssignableFrom(type) select type)
							{
								bool createdHeader = false;
								foreach (var feature in _allFeatures.OrderBy(x => x.id).Reverse().OrderBy((x) => x.modOrigin).Reverse())
								{
									if (feature.hideFeature) continue;
									if (feature.featureType == featureType)
									{
										if (!createdHeader)
										{
											createdHeader = true;
											string withoutGeneric = featureType.Name.Substring(0, featureType.Name.IndexOf('`') == -1 ? featureType.Name.Length - 1 : featureType.Name.IndexOf('`')).ToLower();
											OpLabel headerLabel = new(Margin, _jsonBox.CanvasSize.y - offset, Translate($"{withoutGeneric}_header"), true)
											{
												description = Translate($"{withoutGeneric}_description")
											};
											headerLabel.SetPos(headerLabel.pos - new Vector2(0f, headerLabel.label.FontLineHeight));
											_temporaryElements.Add(headerLabel);
											_jsonBox.AddItems(headerLabel);
											offset += headerLabel.label._textRect.height + (Margin * 2.5f);
										}

										OpLabelSelectable featureLabel = new(_selectableGroup, Margin, _jsonBox.CanvasSize.y - offset, Translate($"slugbase[{feature.id}]"), feature.id, feature.modOrigin, !featuresDict.TryGetValue(feature.id, out _))
										{
											description = Translate($"slugbase_description[{feature.id}]")
										};
										_temporaryElements.Add(featureLabel);
										_jsonBox.AddItems(featureLabel);
										offset += featureLabel.label._textRect.height + Margin;
									}
								}
							}

							_jsonBox.contentSize = offset;
							if (!firstScroll)
							{
								firstScroll = true;
								_jsonBox.ScrollToTop();
							}
						}
						break;
					}
				}
				catch (Exception ex)
				{
					UnityEngine.Debug.LogError(ex);
				}
			}
		}
	}

	internal void ClearJSONFeatureSettings()
	{
		foreach (var element in _temporaryInputElements)
		{
			_onlyTab?.RemoveItems(element);
			element.scrollBox?._RemoveFromScrollBox();
			if (element is UIconfig config)
				config.Unload();
			element.Deactivate();
		}
		_temporaryInputElements.Clear();
	}

	internal void LoadJSONFeatureSettings(OpLabelSelectable selectable, Type featureValue)
	{
		ClearJSONFeatureSettings();

		OpLabel fullDescription = new(Margin, Margin, Translate($"slugbase_full_description[{selectable.signalText}]").WrapText(false, _jsonDescriptionBox.size.x - ((Margin * 2.2f) + _jsonDescriptionBox._SliderSize.x)));
		fullDescription.PosY = _jsonDescriptionBox.size.y - ((Margin / 2f) + fullDescription.label._textRect.height);
		fullDescription.lastScreenPos = fullDescription.pos;
		_jsonDescriptionBox.AddItems(fullDescription);
		_jsonDescriptionBox.contentSize = fullDescription.label._textRect.height + (Margin * 2f);
		_jsonDescriptionBox.ScrollToTop();
		_temporaryInputElements.Add(fullDescription);

		// Add support for API overrides with a mod json file containing the information needed for each special type
		float yOffset = _jsonBox.pos.y - Margin;
		FeatureInfo currentFeature = _allFeatures.Find(x => x.id == selectable.signalText);

		// Handle bool cases
		if (featureValue == typeof(bool))
		{
			OpCheckBox enabled = new(config.Bind("_" + selectable.signalText, (_currentFeaturesDict.TryGetValue(selectable.signalText, out var value) && value is bool flag && flag) || (currentFeature.defaultValues is bool defaultBool && defaultBool)), new());
			enabled.pos = new(_jsonDescriptionBox.pos.x + _jsonDescriptionBox.size.x + Margin, yOffset - enabled.size.y);
			enabled.OnValueUpdate += (UIconfig config, string value, string oldValue) =>
			{
				config.value = value;
				_currentFeaturesDict[selectable.signalText] = bool.Parse(value);
				anyJSONChanges = true;
			};
			_onlyTab.AddItems(enabled);
			_temporaryInputElements.Add(enabled);

			string input = "True / False";
			OpLabel inputText = new(enabled.pos + new Vector2((Margin / 2f) + enabled.size.x, -(enabled.size.y / 2f)), new(50f, 50f), input, FLabelAlignment.Left);
			_onlyTab.AddItems(inputText);
			_temporaryInputElements.Add(inputText);
		}
		else if (featureValue == typeof(bool[]) && currentFeature.inputFields < 4)
		{
			for (int i = 0; i < currentFeature.inputFields; i++)
			{
				OpCheckBox enabled = new(config.Bind("_" + selectable.signalText + i.ToString(), !(!_currentFeaturesDict.TryGetValue(selectable.signalText, out var value) && ConvertJSONObjectToArray(value, out bool[] storedValues) && storedValues?.Length > i && storedValues[i]) != ((currentFeature.defaultValues is bool defaultBool && defaultBool) || (ConvertJSONObjectToArray(currentFeature.defaultValues, out bool[] defaultValues) && defaultValues?.Length > i && defaultValues[i]))), new())
				{
					description = currentFeature.inputFieldDescriptions != null && currentFeature.inputFieldDescriptions.Length > i && !string.IsNullOrEmpty(currentFeature.inputFieldDescriptions[i]) ? Translate(currentFeature.inputFieldDescriptions[i]) : ""
				};
				enabled.pos = new(_jsonDescriptionBox.pos.x + _jsonDescriptionBox.size.x + Margin + (i % 2 != 0 ? 170f : 0f), yOffset - enabled.size.y);
				enabled.OnValueUpdate += (UIconfig config, string value, string oldValue) =>
				{
					config.value = value;
					int index = int.Parse(config.Key.Substring(config.Key.Length - 1));
					UnityEngine.Debug.Log(index);
					if (!_currentFeaturesDict.ContainsKey(selectable.signalText)) _currentFeaturesDict.Add(selectable.signalText, currentFeature.defaultValues);
					if (_currentFeaturesDict.TryGetValue(selectable.signalText, out var values) && ConvertJSONObjectToArray(values, out bool[] bools) && bools.Length > index)
					{
						bools[index] = bool.Parse(value);
						_currentFeaturesDict[selectable.signalText] = bools.Select(x => x as object).ToList();
						UnityEngine.Debug.Log(string.Join(", ", bools));
						anyJSONChanges = true;
					}
				};
				_onlyTab.AddItems(enabled);
				_temporaryInputElements.Add(enabled);

				string input = currentFeature.inputFieldNames != null && currentFeature.inputFieldNames.Length > i && !string.IsNullOrEmpty(currentFeature.inputFieldNames[i]) ? currentFeature.inputFieldNames[i] : "true_or_false";
				OpLabel inputText = new(enabled.pos + new Vector2((Margin / 2f) + enabled.size.x, -(enabled.size.y / 2f)), new(50f, 50f), Translate(input), FLabelAlignment.Left);
				_onlyTab.AddItems(inputText);
				_temporaryInputElements.Add(inputText);

				if (i % 2 != 0)
					yOffset -= enabled.size.y + Margin;
			}
		}

		if (featureValue == typeof(int) || featureValue == typeof(float))
		{
			OpDragger dragger = new(config.Bind("_" + selectable.signalText, _currentFeaturesDict.TryGetValue(selectable.signalText, out var obj) && obj is double value ? Convert.ToInt32(value)
				: currentFeature.defaultValues is double intDefault ? Convert.ToInt32(intDefault)
				: 0), new());
			dragger.pos = new(_jsonDescriptionBox.pos.x + _jsonDescriptionBox.size.x + Margin, yOffset - dragger.size.y);
			dragger.OnValueUpdate += (UIconfig config, string value, string oldValue) =>
			{
				config.value = value;
				_currentFeaturesDict[selectable.signalText] = int.Parse(value);
				anyJSONChanges = true;
			};
			_onlyTab.AddItems(dragger);
			_temporaryInputElements.Add(dragger);
		}

		if (typeof(ExtEnumBase).IsAssignableFrom(featureValue))
		{
			var validEnums = (featureValue.BaseType.GetField("values", BindingFlags.Public | BindingFlags.Static).GetValue(null) is ExtEnumType type) ? type.entries.OrderBy(x => x) : null;

			if (validEnums != null && validEnums.Any())
			{
				OpComboBox comboBox = new(config.Bind("_" + selectable.signalText, _currentFeaturesDict.TryGetValue(selectable.signalText, out object maybeText) && maybeText is string text ? text : "None", new ConfigAcceptableList<string>([.. validEnums])), new(), 150f);
				comboBox.pos = new(_jsonDescriptionBox.pos.x + _jsonDescriptionBox.size.x + Margin, yOffset - comboBox.size.y);
				comboBox.OnValueUpdate += (UIconfig config, string value, string oldValue) =>
				{
					config.value = value;
					_currentFeaturesDict[selectable.signalText] = value;
					anyJSONChanges = true;
				};
				_onlyTab.AddItems(comboBox);
				_temporaryInputElements.Add(comboBox);
			}
		}
	}
}

internal class OpSelectableGroup
{
	private ModOptions modOptions;
	internal OpScrollBox container;
	internal OpLabelSelectable selected;

	internal OpSelectableGroup(ModOptions modOptions, OpScrollBox container)
	{
		this.modOptions = modOptions;
		this.container = container;
	}

	internal void Signal(OpLabelSelectable signalObject)
	{
		if (ModOptions._allFeatures.Find(x => x.id == signalObject.signalText).genericArgument is Type featureValue)
		{
			selected = signalObject;
			modOptions.LoadJSONFeatureSettings(signalObject, featureValue);
		}
		signalObject.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
	}
}

internal class OpLabelSelectable : OpLabel
{
	internal OpSelectableGroup owner;
	internal string signalText;
	internal bool enabled;
	private GlowGradient glow;
	private float glowTimer;
	private FSprite icon;
	private bool clicked;
	internal static readonly float greyedOutAlpha = 0.6f;
	internal static readonly float glowTimerMax = 100f;
	internal static readonly Color darkGrey = new(0.35f, 0.35f, 0.35f);

	internal OpLabelSelectable(OpSelectableGroup owner, float posX, float posY, string text, string signalText, string mod, bool greyedOut = false) : base(posX, posY, text)
	{
		this.owner = owner;
		this.signalText = signalText;
		enabled = !greyedOut;

		string iconName = $"modicon-{mod.ToLower()}";
		Color overrideColor = greyedOut ? darkGrey : MenuColorEffect.rgbMediumGrey;
		icon = new(Futile.atlasManager.DoesContainElementWithName(iconName) ? iconName : "pixel")
		{
			anchorX = 1f,
			scale = Futile.atlasManager.DoesContainElementWithName(iconName) ? 1f : 24f,
			color = overrideColor
		};
		myContainer.AddChild(icon);
		SetPos(pos + new Vector2(icon.width + (ModOptions.Margin / 2f), 0f));
		icon.SetPosition(label.GetPosition().x - (ModOptions.Margin / 2f), label.GetPosition().y);


		glow = new GlowGradient(myContainer, new(), new(owner.container.size.x * 2f, 50f))
		{
			color = overrideColor
		};
		if (greyedOut)
		{
			color = overrideColor;
		}
	}

	public override void GrafUpdate(float timeStacker)
	{
		base.GrafUpdate(timeStacker);

		glowTimer++;
		if (glowTimer > glowTimerMax)
			glowTimer = 0;

		if (MouseOver)
		{
			glow.sprite.isVisible = true;
			glow.pos = label.GetPosition() - new Vector2(glow.size.x / 4f, 25f);
			glow.alpha = Mathf.Lerp(enabled ? 0.4f : 0.2f, enabled ? 0.7f : 0.4f, Mathf.Sin((glowTimer / glowTimerMax) * 3.1416f));
		}
		else
		{
			glow.sprite.isVisible = false;
		}
	}

	public override void Update()
	{
		base.Update();

		if (MouseOver && Input.GetMouseButton(0) && !clicked)
		{
			owner.Signal(this);
		}
		clicked = Input.GetMouseButton(0);
	}

	internal void Enable(bool enable)
	{
		enabled = enable;
		Color overrideColor = !enable ? darkGrey : Color.white;
		color = overrideColor;
		icon.color = color;
		glow.color = color;
	}
}
