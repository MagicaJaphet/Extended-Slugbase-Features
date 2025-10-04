using ExtendedSlugbaseFeatures.Resources;
using Menu.Remix.MixedUI;
using SlugBase;
using SlugBase.Features;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using static ExtendedSlugbaseFeatures.Helpers.RoomSpecificScriptHelpers.CustomCutscene;

namespace ExtendedSlugbaseFeatures;
internal class ModOptions : OptionInterface
{
	private OpTab _onlyTab;
	private static OpScrollBox _jsonBox;
	private static List<UIelement> _temporaryElements = [];
	private string _currentJSONFile;
	private static Dictionary<string, Type[]> _gameFeatures = [];
	private static Dictionary<string, Type[]> _playerFeatures = [];

	public static ModOptions Instance { get; } = new();

	private float Margin { get; } = 10f;
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

			_onlyTab.AddItems([slugBaseSelector, _jsonBox]);

			var featureManager = AppDomain.CurrentDomain.GetAssemblies().Where(asm => asm.GetName().Name.ToLower() == "slugbase").FirstOrDefault()?.GetTypes().Where(type => type.Name == "FeatureManager").FirstOrDefault();
			if (featureManager != null)
			{
				var allFeatures = featureManager.GetField("_features", BindingFlags.NonPublic | BindingFlags.Static);
				if (allFeatures != null && allFeatures.GetValue(null) is Dictionary<string, Feature> featureDict)
				{
					foreach (var key in featureDict.Keys.Reverse())
					{
						if (featureDict[key].GetType().Name.Contains("Game") && !_gameFeatures.ContainsKey(key))
						{
							_gameFeatures.Add(key, featureDict[key].GetType().GetGenericArguments());
						}
						else if (!_playerFeatures.ContainsKey(key))
						{
							_playerFeatures.Add(key, featureDict[key].GetType().GetGenericArguments());
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

	private void SlugBaseSelector_OnChange()
	{
		foreach (var config in _temporaryElements)
		{
			_onlyTab?.RemoveItems(config);
		}

		RefreshJSONFeatures();
	}

	public void RefreshJSONFeatures()
	{
		var files = AssetManager.ListDirectory("slugbase", includeAll: true);

		foreach (var file in files.Where(file => file.EndsWith(".json")))
		{
			if (JsonResources.IsMostRecent(SlugBaseCharacter.Registry, [file]))
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
							OpSelectableGroup selectableGroup = new(this, _jsonBox);

							float offset = Margin;
							OpLabel gameFeatureLabel = new(Margin, _jsonBox.CanvasSize.y - offset, Translate("Game Features"), true)
							{
								description = Translate("A game feature is a Slugbase feature which is used exclusively for the slugcat's campaign.")
							};
							gameFeatureLabel.SetPos(gameFeatureLabel.pos - new Vector2(0f, gameFeatureLabel.label.FontLineHeight));
							_temporaryElements.Add(gameFeatureLabel);
							_jsonBox.AddItems(gameFeatureLabel);
							offset += gameFeatureLabel.label.FontLineHeight + (Margin * 2f);

							foreach (var gameFeature in _gameFeatures.Keys)
							{
								OpLabelSelectable gameLabel = new(selectableGroup, Margin, _jsonBox.CanvasSize.y - offset, Translate($"slugbase[{gameFeature}]"), gameFeature, !featuresDict.TryGetValue(gameFeature, out _))
								{
									description = Translate($"slugbase_description[{gameFeature}]")
								};
								_temporaryElements.Add(gameLabel);
								_jsonBox.AddItems(gameLabel);
								offset += gameLabel.label.FontLineHeight + Margin;
							}

							_jsonBox.contentSize = offset;
						}
					}
					break;
				}
				catch (Exception ex)
				{
					UnityEngine.Debug.LogError(ex);
				}
			}
		}
	}
}

internal class OpSelectableGroup
{
	private ModOptions modOptions;
	internal OpScrollBox container;

	internal OpSelectableGroup(ModOptions modOptions, OpScrollBox container)
	{
		this.modOptions = modOptions;
		this.container = container;
	}

	internal void Signal(OpLabelSelectable signalObject)
	{
	}
}

internal class OpLabelSelectable : OpLabel
{
	internal OpSelectableGroup owner;
	internal string signalText;
	private bool enabled;
	private GlowGradient glow;
	private float glowTimer;
	internal static readonly float greyedOutAlpha = 0.6f;
	internal static readonly float glowTimerMax = 100f;

	internal OpLabelSelectable(OpSelectableGroup owner, float posX, float posY, string text, string signalText, bool greyedOut = false) : base(posX, posY, text)
	{
		this.owner = owner;
		this.signalText = signalText;
		enabled = !greyedOut;

		glow = new GlowGradient(myContainer, new(), new(owner.container.CanvasSize.x, 50f))
		{
			color = greyedOut ? Color.grey : Color.white
		};
		if (greyedOut)
		{
			color = greyedOut ? Color.grey : Color.white;
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
			glow.pos = label.GetPosition() - new Vector2(0, 25f);
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

		if (MouseOver && Input.GetMouseButtonDown(0))
		{
			owner.Signal(this);
		}
	}
}
