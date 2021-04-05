/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2021
 *	
 *	"ACEditorPrefs.cs"
 * 
 *	This script allows for the setting of Editor-wide preferences via Unity's Player settings window (Unity 2019.2 and later).
 * 
 */

#if UNITY_2019_2_OR_NEWER && UNITY_EDITOR
#define CAN_USE_EDITOR_PREFS
using UnityEditor;
#endif

using UnityEngine;

namespace AC
{

	/**
	 * This script allows for the setting of Editor-wide preferences via Unity's Player settings window (Unity 2019.2 and later).
	 */
	public class ACEditorPrefs : ScriptableObject
	{

		#if CAN_USE_EDITOR_PREFS

		private const string settingsPath = "/Editor/ACEditorPrefs.asset";

		[SerializeField] protected int hierarchyIconOffset = 0;
		[SerializeField] protected Color hotspotGizmoColor = new Color (1f, 1f, 0f, 0.6f);
		[SerializeField] protected Color triggerGizmoColor = new Color (1f, 0.3f, 0f, 0.8f);
		[SerializeField] protected Color collisionGizmoColor = new Color (0f, 1f, 1f, 0.8f);
		[SerializeField] protected Color pathGizmoColor = Color.blue;
		[SerializeField] protected int menuItemsBeforeScroll = 15;
		[SerializeField] protected CSVFormat csvFormat = CSVFormat.Standard;
		[SerializeField] protected bool showHierarchyIcons = true;
		[SerializeField] protected int editorLabelWidth = 0;
		[SerializeField] protected int actionNodeWidth = 300;
		[SerializeField] protected bool disableInstaller = false;


		internal static ACEditorPrefs GetOrCreateSettings ()
		{
			string fullPath = Resource.MainFolderPath + settingsPath;

			var settings = AssetDatabase.LoadAssetAtPath<ACEditorPrefs> (fullPath);
			if (settings == null)
			{
				bool isValid = AssetDatabase.IsValidFolder (Resource.MainFolderPath + "/Editor");
				if (!isValid)
				{
					AssetDatabase.CreateFolder (Resource.MainFolderPath, "Editor");
					isValid = AssetDatabase.IsValidFolder (Resource.MainFolderPath + "/Editor");
				}

				if (isValid)
				{
					settings = ScriptableObject.CreateInstance<ACEditorPrefs> ();
					settings.hierarchyIconOffset = DefaultHierarchyIconOffset;
					settings.hotspotGizmoColor = DefaultHotspotGizmoColor;
					settings.triggerGizmoColor = DefaultTriggerGizmoColor;
					settings.collisionGizmoColor = DefaultCollisionGizmoColor;
					settings.pathGizmoColor = DefaultPathGizmoColor;
					settings.menuItemsBeforeScroll = DefaultMenuItemsBeforeScroll;
					settings.csvFormat = DefaultCSVFormat;
					settings.showHierarchyIcons = DefaultShowHierarchyIcons;
					settings.editorLabelWidth = DefaultEditorLabelWidth;
					settings.actionNodeWidth = DefaultActionNodeWidth;
					settings.disableInstaller = DefaultDisableInstaller;
					AssetDatabase.CreateAsset (settings, fullPath);
					AssetDatabase.SaveAssets ();
				}
				else
				{
					Debug.LogWarning ("Cannot create AC editor prefs - does the folder '" + Resource.MainFolderPath + "/Editor' exist?");
					return null;
				}
			}
			return settings;
		}


		internal static SerializedObject GetSerializedSettings ()
		{
			ACEditorPrefs settings = GetOrCreateSettings ();
			if (settings != null)
			{
				return new SerializedObject (settings);
			}
			return null;
		}

		#endif


		/** A horizontal offset to apply to Hierarchy icons */
		public static int HierarchyIconOffset
		{
			get
			{
				#if CAN_USE_EDITOR_PREFS
				ACEditorPrefs settings = GetOrCreateSettings ();
				return (settings != null) ? settings.hierarchyIconOffset : DefaultHierarchyIconOffset;
				#else
				return DefaultHierarchyIconOffset;
				#endif
			}
		}


		private static int DefaultHierarchyIconOffset
		{
			get
			{
				return 0;
			}
		}


		/** How wide to render labels in the Managers and other Editors */
		public static int EditorLabelWidth
		{
			get
			{
				#if CAN_USE_EDITOR_PREFS
				ACEditorPrefs settings = GetOrCreateSettings ();
				return (settings != null) ? settings.editorLabelWidth : DefaultEditorLabelWidth;
				#else
				return DefaultHierarchyIconOffset;
				#endif
			}
		}


		private static int DefaultEditorLabelWidth
		{
			get
			{
				return 0;
			}
		}


		/** How wide Action nodes are in the ActionList Editor window */
		public static int ActionNodeWidth
		{
			get
			{
				int value = DefaultActionNodeWidth;
				#if CAN_USE_EDITOR_PREFS
				ACEditorPrefs settings = GetOrCreateSettings ();
				if (settings != null) value = settings.actionNodeWidth;
				#endif

				if (value <= 0) return DefaultActionNodeWidth;
				return Mathf.Clamp (value, 200, 800);
			}
		}


		private static int DefaultActionNodeWidth
		{
			get
			{
				return 300;
			}
		}


		/** If True, then icons can be displayed in the Hierarchy window */
		public static bool ShowHierarchyIcons
		{
			get
			{
				#if CAN_USE_EDITOR_PREFS
				ACEditorPrefs settings = GetOrCreateSettings ();
				return (settings != null) ? settings.showHierarchyIcons : DefaultShowHierarchyIcons;
				#else
				return DefaultShowHierarchyIcons;
				#endif
			}
		}


		private static bool DefaultShowHierarchyIcons
		{
			get
			{
				return true;
			}
		}


		/** The colour to paint Hotspot gizmos with */
		public static Color HotspotGizmoColor
		{
			get
			{
				#if CAN_USE_EDITOR_PREFS
				ACEditorPrefs settings = GetOrCreateSettings ();
				return (settings != null) ? settings.hotspotGizmoColor : DefaultHotspotGizmoColor;
				#else
				return DefaultHotspotGizmoColor;
				#endif
			}
		}


		private static Color DefaultHotspotGizmoColor
		{
			get
			{
				return new Color (1f, 1f, 0f, 0.6f);
			}
		}


		/** The colour to paint Trigger gizmos with */
		public static Color TriggerGizmoColor
		{
			get
			{
				#if CAN_USE_EDITOR_PREFS
				ACEditorPrefs settings = GetOrCreateSettings ();
				return (settings != null) ? settings.triggerGizmoColor : DefaultTriggerGizmoColor;
				#else
				return DefaultTriggerGizmoColor;
				#endif
			}
		}


		private static Color DefaultTriggerGizmoColor
		{
			get
			{
				return new Color (1f, 0.3f, 0f, 0.8f);
			}
		}


		/** The colour to paint Collision gizmos with */
		public static Color CollisionGizmoColor
		{
			get
			{
				#if CAN_USE_EDITOR_PREFS
				ACEditorPrefs settings = GetOrCreateSettings ();
				return (settings != null) ? settings.collisionGizmoColor : DefaultCollisionGizmoColor;
				#else
				return DefaultCollisionGizmoColor;
				#endif
			}
		}


		private static Color DefaultCollisionGizmoColor
		{
			get
			{
				return new Color (0f, 1f, 1f, 0.8f);
			}
		}


		/** The colour to paint Paths with */
		public static Color PathGizmoColor
		{
			get
			{
				#if CAN_USE_EDITOR_PREFS
				ACEditorPrefs settings = GetOrCreateSettings ();
				return (settings != null) ? settings.pathGizmoColor : DefaultPathGizmoColor;
				#else
				return DefaultPathGizmoColor;
				#endif
			}
		}


		private static Color DefaultPathGizmoColor
		{
			get
			{
				return Color.blue;
			}
		}


		/** The format to read/write CSV files */
		public static CSVFormat CSVFormat
		{
			get
			{
				#if CAN_USE_EDITOR_PREFS
				ACEditorPrefs settings = GetOrCreateSettings ();
				return (settings != null) ? settings.csvFormat : DefaultCSVFormat;
				#else
				return CSVFormat.Legacy;
				#endif
			}
		}


		private static CSVFormat DefaultCSVFormat
		{
			get
			{
				return CSVFormat.Standard;
			}
		}


		/** How many menu items can be displayed in the Editor window before scrolling is required */
		public static int MenuItemsBeforeScroll
		{
			get
			{
				#if CAN_USE_EDITOR_PREFS
				ACEditorPrefs settings = GetOrCreateSettings ();
				return (settings != null) ? settings.menuItemsBeforeScroll : DefaultMenuItemsBeforeScroll;
				#else
				return DefaultMenuItemsBeforeScroll;
				#endif
			}
		}


		private static int DefaultMenuItemsBeforeScroll
		{
			get
			{
				return 15;
			}
		}


		/** If True, checks for AC's required Input and Layer settings will be bypassed */
		public static bool DisableInstaller
		{
			get
			{
				#if CAN_USE_EDITOR_PREFS
				ACEditorPrefs settings = GetOrCreateSettings ();
				return (settings != null) ? settings.disableInstaller : DefaultDisableInstaller;
				#else
				return DefaultDisableInstaller;
				#endif
			}
		}


		private static bool DefaultDisableInstaller
		{
			get
			{
				return false;
			}
		}

	}


	#if CAN_USE_EDITOR_PREFS

	static class ACEditorPrefsIMGUIRegister
	{

		[SettingsProvider]
		public static SettingsProvider CreateMyCustomSettingsProvider ()
		{
			var provider = new SettingsProvider ("Project/AdventureCreator", SettingsScope.Project)
			{
				label = "Adventure Creator",

				guiHandler = (searchContext) =>
				{
					var settings = ACEditorPrefs.GetSerializedSettings ();
					if (settings != null)
					{
						EditorGUILayout.LabelField ("Gizmo colours", EditorStyles.boldLabel);
						EditorGUILayout.PropertyField (settings.FindProperty ("hotspotGizmoColor"), new GUIContent ("Hotspots:", "The colour to tint Hotspot gizmos with"));
						EditorGUILayout.PropertyField (settings.FindProperty ("triggerGizmoColor"), new GUIContent ("Triggers:", "The colour to tint Trigger gizmos with"));
						EditorGUILayout.PropertyField (settings.FindProperty ("collisionGizmoColor"), new GUIContent ("Collisions:", "The colour to tint Collision gizmos with"));
						EditorGUILayout.PropertyField (settings.FindProperty ("pathGizmoColor"), new GUIContent ("Paths:", "The colour to draw Path gizmos with"));

						EditorGUILayout.Space ();
						EditorGUILayout.LabelField ("Hierarchy icons", EditorStyles.boldLabel);
						EditorGUILayout.PropertyField (settings.FindProperty ("showHierarchyIcons"), new GUIContent ("Show icons?", "If True, save and node icons will appear in the Hierarchy window"));
						if (settings.FindProperty ("showHierarchyIcons") == null || settings.FindProperty ("showHierarchyIcons").boolValue)
						{
							EditorGUILayout.PropertyField (settings.FindProperty ("hierarchyIconOffset"), new GUIContent ("Horizontal offset:", "A horizontal offset to apply to AC icons in the Hierarchy"));
						}

						EditorGUILayout.Space ();
						EditorGUILayout.LabelField ("Editor settings", EditorStyles.boldLabel);
						EditorGUILayout.PropertyField (settings.FindProperty ("editorLabelWidth"), new GUIContent ("Label widths:", "How wide to render labels in Managers and other editors"));
						EditorGUILayout.PropertyField (settings.FindProperty ("menuItemsBeforeScroll"), new GUIContent ("Items before scrolling:", "How many Menus, Inventory items, Variables etc can be listed in the AC Game Editor before scrolling becomes necessary"));
						EditorGUILayout.PropertyField (settings.FindProperty ("actionNodeWidth"), new GUIContent ("Action node widths:", "How wide Actions are when rendered as nodes in the ActionList Editor window"));
						EditorGUILayout.PropertyField (settings.FindProperty ("disableInstaller"), new GUIContent ("Bypass install checks?", "If True, checks for AC's required Input and Layer settings will be bypassed"));

						EditorGUILayout.Space ();
						EditorGUILayout.LabelField ("Import / export", EditorStyles.boldLabel);
						EditorGUILayout.PropertyField (settings.FindProperty ("csvFormat"), new GUIContent ("CSV format:", "The formatting method to apply to CSV files"));

						settings.ApplyModifiedProperties ();
					}
					else
					{
						EditorGUILayout.HelpBox ("Cannot create AC editor prefs - does the folder '" + Resource.MainFolderPath + "/Editor' exist?", MessageType.Warning);
					}
				},
			};

			return provider;
		}
	}

	#endif

}