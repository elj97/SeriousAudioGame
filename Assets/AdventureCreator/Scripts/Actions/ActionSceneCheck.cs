/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2021
 *	
 *	"ActionSceneCheck.cs"
 * 
 *	This action checks the player's last-visited scene,
 *	useful for running specific "player enters the room" cutscenes.
 * 
 */

using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	[System.Serializable]
	public class ActionSceneCheck : ActionCheck
	{
		
		public enum IntCondition { EqualTo, NotEqualTo };
		public enum SceneToCheck { Current, Previous };
		public ChooseSceneBy chooseSceneBy = ChooseSceneBy.Number;
		public SceneToCheck sceneToCheck = SceneToCheck.Current;
		[SerializeField] protected int chooseSceneByPlayerSwitching = -1;
		protected enum ChooseSceneByPlayerSwitching { Number=0, Name=1, CurrentMain=2 };

		public int sceneNumberParameterID = -1;
		public int sceneNumber;

		public int sceneNameParameterID = -1;
		public string sceneName;

		public int playerID = -1;
		public int playerParameterID = -1;

		private int runtimePlayerID;
		public IntCondition intCondition;


		public override ActionCategory Category { get { return ActionCategory.Scene; }}
		public override string Title { get { return "Check"; }}
		public override string Description { get { return "Queries either the current scene, or the last one visited."; }}


		public override void AssignValues (List<ActionParameter> parameters)
		{
			sceneNumber = AssignInteger (parameters, sceneNumberParameterID, sceneNumber);
			sceneName = AssignString (parameters, sceneNameParameterID, sceneName);
			runtimePlayerID = AssignInteger (parameters, playerParameterID, playerID);
		}

		
		public override bool CheckCondition ()
		{
			int actualSceneNumber = 0;
			
			if (KickStarter.settingsManager == null || KickStarter.settingsManager.playerSwitching == PlayerSwitching.DoNotAllow)
			{
				runtimePlayerID = -1;
			}

			if (runtimePlayerID >= 0)
			{
				PlayerData playerData = KickStarter.saveSystem.GetPlayerData (runtimePlayerID);
				if (playerData != null)
				{
					if (sceneToCheck == SceneToCheck.Previous)
					{
						actualSceneNumber = playerData.previousScene;
					}
					else
					{
						actualSceneNumber = playerData.currentScene;
					}

					ChooseSceneByPlayerSwitching csbps = (ChooseSceneByPlayerSwitching) chooseSceneByPlayerSwitching;
					if (csbps == ChooseSceneByPlayerSwitching.CurrentMain)
					{
						return (actualSceneNumber == SceneChanger.CurrentSceneIndex);
					}
					chooseSceneBy = (ChooseSceneBy) chooseSceneByPlayerSwitching;
				}
				else
				{
					LogWarning ("Could not find scene data for Player ID = " + playerID);
				}
			}
			else
			{
				if (sceneToCheck == SceneToCheck.Previous)
				{
					actualSceneNumber = KickStarter.sceneChanger.PreviousSceneIndex;
				}
				else
				{
					actualSceneNumber = SceneChanger.CurrentSceneIndex;
				}
			}

			if (actualSceneNumber == -1 && sceneToCheck == SceneToCheck.Previous)
			{
				LogWarning ("The " + sceneToCheck + " scene's Build Index is currently " + actualSceneNumber + " - is this the game's first scene?");
				return false;
			}

			string actualSceneName = KickStarter.sceneChanger.IndexToName (actualSceneNumber);

			if (intCondition == IntCondition.EqualTo)
			{
				if (chooseSceneBy == ChooseSceneBy.Name && actualSceneName == AdvGame.ConvertTokens (sceneName))
				{
					return true;
				}

				if (chooseSceneBy == ChooseSceneBy.Number && actualSceneNumber == sceneNumber)
				{
					return true;
				}
			}
			else if (intCondition == IntCondition.NotEqualTo)
			{
				if (chooseSceneBy == ChooseSceneBy.Name && actualSceneName != AdvGame.ConvertTokens (sceneName))
				{
					return true;
				}

				if (chooseSceneBy == ChooseSceneBy.Number && actualSceneNumber != sceneNumber)
				{
					return true;
				}
			}
			
			return false;
		}

		
		#if UNITY_EDITOR

		public override void ShowGUI (List<ActionParameter> parameters)
		{
			bool showPlayerOptions = false;

			if (KickStarter.settingsManager != null && KickStarter.settingsManager.playerSwitching == PlayerSwitching.Allow)
			{
				playerParameterID = ChooseParameterGUI ("Player:", parameters, playerParameterID, ParameterType.Integer);
				if (playerParameterID < 0)
				{
					playerID = ChoosePlayerGUI (playerID, true);
					showPlayerOptions = (playerID >= 0);
				}
				else
				{
					showPlayerOptions = true;
				}
			}

			if (showPlayerOptions)
			{
				sceneToCheck = (SceneToCheck) EditorGUILayout.EnumPopup ("Check type:", sceneToCheck);
				if (chooseSceneByPlayerSwitching == -1)
				{
					chooseSceneByPlayerSwitching = (int) chooseSceneBy;
				}
				ChooseSceneByPlayerSwitching csbps = (ChooseSceneByPlayerSwitching) chooseSceneByPlayerSwitching;
				csbps = (ChooseSceneByPlayerSwitching) EditorGUILayout.EnumPopup ("Choose scene by:", csbps);
				chooseSceneByPlayerSwitching = (int) csbps;
				chooseSceneBy = (ChooseSceneBy) chooseSceneByPlayerSwitching;

				EditorGUILayout.BeginHorizontal ();
				switch (csbps)
				{
					case ChooseSceneByPlayerSwitching.Name:
						EditorGUILayout.LabelField ("Scene name is:", GUILayout.Width (100f));
						intCondition = (IntCondition) EditorGUILayout.EnumPopup (intCondition);

						sceneNameParameterID = ChooseParameterGUI (string.Empty, parameters, sceneNameParameterID, ParameterType.String);
						if (sceneNameParameterID < 0)
						{
							sceneName = EditorGUILayout.TextField (sceneName);
						}
						break;

					case ChooseSceneByPlayerSwitching.Number:
						EditorGUILayout.LabelField ("Scene number is:", GUILayout.Width (100f));
						intCondition = (IntCondition) EditorGUILayout.EnumPopup (intCondition);

						sceneNumberParameterID = ChooseParameterGUI (string.Empty, parameters, sceneNumberParameterID, ParameterType.Integer);
						if (sceneNumberParameterID < 0)
						{
							sceneNumber = EditorGUILayout.IntField (sceneNumber);
						}
						break;

					default:
						break;
				}
				EditorGUILayout.EndHorizontal ();
			}
			else
			{
				sceneToCheck = (SceneToCheck) EditorGUILayout.EnumPopup ("Check type:", sceneToCheck);
				chooseSceneBy = (ChooseSceneBy) EditorGUILayout.EnumPopup ("Choose scene by:", chooseSceneBy);

				EditorGUILayout.BeginHorizontal ();
				switch (chooseSceneBy)
				{
					case ChooseSceneBy.Name:
						EditorGUILayout.LabelField ("Scene name is:", GUILayout.Width (100f));
						intCondition = (IntCondition) EditorGUILayout.EnumPopup (intCondition);

						sceneNameParameterID = ChooseParameterGUI (string.Empty, parameters, sceneNameParameterID, ParameterType.String);
						if (sceneNameParameterID < 0)
						{
							sceneName = EditorGUILayout.TextField (sceneName);
						}
						break;

					case ChooseSceneBy.Number:
						EditorGUILayout.LabelField ("Scene number is:", GUILayout.Width (100f));
						intCondition = (IntCondition) EditorGUILayout.EnumPopup (intCondition);

						sceneNumberParameterID = ChooseParameterGUI (string.Empty, parameters, sceneNumberParameterID, ParameterType.Integer);
						if (sceneNumberParameterID < 0)
						{
							sceneNumber = EditorGUILayout.IntField (sceneNumber);
						}
						break;

					default:
						break;
				}
				EditorGUILayout.EndHorizontal ();
			}
		}

		#endif


		/**
		 * <summary>Creates a new instance of the 'Scene: Check' Action</summary>
		 * <param name = "sceneName">The name of the scene to check for</param>
		 * <param name = "sceneToCheck">Which scene type to check for</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionSceneCheck CreateNew (string sceneName, SceneToCheck sceneToCheck = SceneToCheck.Current)
		{
			ActionSceneCheck newAction = CreateNew<ActionSceneCheck> ();
			newAction.sceneToCheck = sceneToCheck;
			newAction.chooseSceneBy = ChooseSceneBy.Name;
			newAction.sceneName = sceneName;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Scene: Check' Action</summary>
		 * <param name = "sceneName">The build index number of the scene to check for</param>
		 * <param name = "sceneToCheck">Which scene type to check for</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionSceneCheck CreateNew (int sceneNumber, SceneToCheck sceneToCheck = SceneToCheck.Current)
		{
			ActionSceneCheck newAction = CreateNew<ActionSceneCheck> ();
			newAction.sceneToCheck = sceneToCheck;
			newAction.chooseSceneBy = ChooseSceneBy.Number;
			newAction.sceneNumber = sceneNumber;
			return newAction;
		}

	}

}