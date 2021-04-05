/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2021
 *	
 *	"SceneChanger.cs"
 * 
 *	This script handles the changing of the scene, and stores
 *	which scene was previously loaded, for use by PlayerStart.
 * 
 */

using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

namespace AC
{

	/**
	 * Handles the changing of the scene, and keeps track of which scene was previously loaded.
	 * It should be placed on the PersistentEngine prefab.
	 */
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_scene_changer.html")]
	public class SceneChanger : MonoBehaviour
	{

		#region Variables


		protected List<SceneInfo> buildScenes = new List<SceneInfo> ();
		protected int previousGlobalSceneIndex = -1;

		protected List<SubScene> subScenes = new List<SubScene>();

		protected Vector3 relativePosition;
		protected AsyncOperation preloadAsync;
		protected int preloadSceneIndex = -1;
		protected Texture2D textureOnTransition = null;
		protected bool isLoading = false;
		protected float loadingProgress = 0f;
		
		protected Vector2 simulatedCursorPositionOnExit = new Vector2 (-1f, -1f);
		protected bool completeSceneActivation;

		#endregion


		#region UnityStandards

		protected void OnEnable ()
		{
			EventManager.OnInitialiseScene += OnInitialiseScene;
			EventManager.OnAfterChangeScene += OnAfterChangeScene;
			UnityEngine.SceneManagement.SceneManager.activeSceneChanged += OnActiveSceneChanged;
		}


		protected void OnDisable ()
		{
			EventManager.OnInitialiseScene -= OnInitialiseScene;
			EventManager.OnAfterChangeScene -= OnAfterChangeScene;
			UnityEngine.SceneManagement.SceneManager.activeSceneChanged -= OnActiveSceneChanged;
		}

		#endregion


		#region PublicFunctions

		public void OnInitPersistentEngine ()
		{
			preloadSceneIndex = -1;
			previousGlobalSceneIndex = -1;

			PopulateBuildSceneData ();
		}


		public int NameToIndex (string sceneName)
		{
			sceneName = AdvGame.ConvertTokens (sceneName);

			foreach (SceneInfo buildSceneInfo in buildScenes)
			{
				if (buildSceneInfo.Filename == sceneName)
				{
					return buildSceneInfo.BuildIndex;
				}
			}
			ACDebug.LogWarning ("The scene named '" + sceneName + "' was not found in the Build settings.");
			return 0;
		}


		public string IndexToName (int sceneIndex)
		{
			SceneInfo sceneInfo = GetSceneInfo (sceneIndex);
			if (sceneInfo != null) return sceneInfo.Filename;
			return string.Empty;
		}


		/**
		 * <summary>Calculates the player's position relative to the next scene's PlayerStart.</summary>
		 * <param name = "marker">The Marker of the GameObject that marks the position that the player should be placed relative to.</param>
		 */
		public void SetRelativePosition (Marker marker)
		{
			if (KickStarter.player == null || marker == null)
			{
				relativePosition = Vector2.zero;
			}
			else
			{
				relativePosition = KickStarter.player.Transform.position - marker.Position;
				if (SceneSettings.IsUnity2D ())
				{
					relativePosition.z = 0f;
				}
				else if (SceneSettings.IsTopDown ())
				{
					relativePosition.y = 0f;
				}
			}
		}


		/**
		 * <summary>Gets the player's starting position by adding the relative position (set in ActionScene) to the PlayerStart's position.</summary>
		 * <param name = "playerStartPosition">The position of the PlayerStart object</param>
		 * <returns>The player's starting position</returns>
		 */
		public virtual Vector3 GetStartPosition (Vector3 playerStartPosition)
		{
			Vector3 startPosition = playerStartPosition + relativePosition;
			relativePosition = Vector2.zero;
			return startPosition;
		}


		/**
		 * <summary>Gets the progress of an asynchronous scene load as a decimal.</summary>
		 * <returns>The progress of an asynchronous scene load as a decimal.</returns>
		 */
		public float GetLoadingProgress ()
		{
			if (KickStarter.settingsManager.useAsyncLoading)
			{
				return loadingProgress;
			}
			else
			{
				ACDebug.LogWarning ("Cannot get the loading progress because asynchronous loading is not enabled in the Settings Manager.");
			}
			return 0f;
		}


		/**
		 * <summary>Checks if a scene is being loaded</summary>
		 * <returns>True if a scene is being loaded</returns>
		 */
		public bool IsLoading ()
		{
			return isLoading;
		}


		/**
		 * <summary>Preloads a scene.  Preloaded data will be discarded if the next scene opened is not the same as the one preloaded<summary>
		 * <param name = "nextSceneIndex">The build index to load</param>
		 * </returns>
		 */
		public void PreloadScene (int nextSceneIndex)
		{
			if (nextSceneIndex < 0) return;

			if (nextSceneIndex == preloadSceneIndex)
			{
				ACDebug.Log ("Skipping preload of scene '" + nextSceneIndex + "' - already preloaded.");
				return;
			}
			StartCoroutine (PreloadLevelAsync (nextSceneIndex));
		}


		/**
		 * <summary>Loads a new scene.  This method should be used instead of Unity's own scene-switching method, because this allows for AC objects to be saved beforehand</summary>
		 * <param name = "nextSceneIndex">The build index of the scene to load</param>
		 * <param name = "sceneNumber">The number of the scene to load, if sceneName = ""</param>
		 * <param name = "saveRoomData">If True, then the states of the current scene's Remember scripts will be recorded in LevelStorage</param>
		 * <param name = "forceReload">If True, the scene will be re-loaded if it is already open.</param>
		 * <returns>True if the new scene will be loaded in</returns>
		 */
		public void ChangeScene (int nextSceneIndex, bool saveRoomData, bool forceReload = false, bool doOverlay = false)
		{
			if (nextSceneIndex < 0)
			{
				return;
			}

			if (!isLoading)
			{
				if (forceReload || nextSceneIndex != CurrentSceneIndex)
				{
					if (preloadSceneIndex >= 0 && preloadSceneIndex != nextSceneIndex)
					{
						ACDebug.LogWarning ("Opening scene " + nextSceneIndex + ", but have preloaded scene " + preloadSceneIndex + ".  Preloaded data will be scrapped.");
						if (preloadAsync != null) preloadAsync.allowSceneActivation = true;
						preloadSceneIndex = -1;
					}

					PrepareSceneForExit (!KickStarter.settingsManager.useAsyncLoading, saveRoomData, doOverlay);
					LoadLevel (nextSceneIndex, KickStarter.settingsManager.useLoadingScreen, KickStarter.settingsManager.useAsyncLoading, forceReload, doOverlay);
					return;
				}
			}
			else
			{
				ACDebug.LogWarning ("Cannot switch scene while another scene-loading operation is underway.");
			}
			return;
		}


		/*
		 * <summary>Stores a texture used as an overlay during a scene transition. This texture can be retrieved with GetAndResetTransitionTexture().</summary>
		 * <param name = "_texture">The Texture2D to store</param>
		 */
		public void SetTransitionTexture (Texture2D _texture)
		{
			textureOnTransition = _texture;
		}


		/**
		 * <summary>Gets, and removes from memory, the texture used as an overlay during a scene transition.</summary>
		 * <returns>The texture used as an overlay during a scene transition</returns>
		 */
		public Texture2D GetAndResetTransitionTexture ()
		{
			Texture2D _texture = textureOnTransition;
			textureOnTransition = null;
			return _texture;
		}


		/**
		 * <summary>Deletes a GameObject once the current frame has finished renderering.</summary>
		 * <param name = "_gameObject">The GameObject to delete</param>
		 */
		public void ScheduleForDeletion (GameObject _gameObject)
		{
			if (_gameObject.GetComponentInChildren <ActionList>())
			{
				ActionList actionList = _gameObject.GetComponent <ActionList>();
				if (actionList && KickStarter.actionListManager.IsListRunning (actionList))
				{
					actionList.Kill ();
					ACDebug.LogWarning ("The ActionList '" + actionList.name + "' is being removed from the scene while running!  Killing it now to prevent hanging.");
				}
			}

			StartCoroutine (ScheduleForDeletionCoroutine (_gameObject));
		}


		/**
		 * <summary>Saves the current scene objects, kills speech dialog etc.  This should if the scene is changed using a custom script, i.e. without using the provided 'Scene: Switch' Action.</summary>
		 */
		public void PrepareSceneForExit ()
		{
			PrepareSceneForExit (false, true, false);
		}


		/** Creates an internal record of all scenes that are in the game.  If scenes are added at runtime, this function may need to be overridden to include them. */
		public virtual void PopulateBuildSceneData ()
		{
			int numScenes = UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings;
			for (int i = 0; i < numScenes; i++)
			{
				string path = SceneUtility.GetScenePathByBuildIndex (i);

				string sceneName = System.IO.Path.GetFileNameWithoutExtension (path);

				if (!string.IsNullOrEmpty (sceneName))
				{
					SceneInfo buildScene = new SceneInfo (i, sceneName);
					buildScenes.Add (buildScene);
				}
			}

			if (numScenes == 0)
			{
				// No scenes added, add current as temp
				buildScenes.Add (new SceneInfo (0, string.Empty));
			}
		}


		/** Resets the current scene, clearing all data related to it */
		public void ResetCurrentScene ()
		{
			KickStarter.runtimeInventory.SetNull ();
			KickStarter.runtimeInventory.RemoveRecipes ();

			if (KickStarter.settingsManager.blackOutWhenInitialising)
			{
				KickStarter.mainCamera.ForceOverlayForFrames (6);
			}

			if (KickStarter.player)
			{
				DestroyImmediate (KickStarter.player.gameObject);
			}

			KickStarter.levelStorage.ClearCurrentLevelData ();
			ChangeScene (CurrentSceneIndex, false, true);
		}


		/** Displays scene-related information for the AC Status window */
		public void DrawStatus ()
		{
			if (SubScenesAreOpen ())
			{
				string openScenes = string.Empty;
				for (int i = 0; i < subScenes.Count; i++)
				{
					if (subScenes[i] == null || subScenes[i].gameObject == null) continue;

					openScenes += subScenes[i].gameObject.scene.name;
					if (i < (subScenes.Count - 1))
					{
						openScenes += ", ";
					}
				}
				GUILayout.Label ("Active scene: " + UnityVersionHandler.GetCurrentSceneName ());
				GUILayout.Label ("Sub-scenes: " + openScenes);
			}
		}

		#endregion


		#region ProtectedFunctions

		protected SceneInfo GetSceneInfo (int sceneIndex)
		{
			foreach (SceneInfo buildSceneInfo in buildScenes)
			{
				if (buildSceneInfo.BuildIndex == sceneIndex)
				{
					return buildSceneInfo;
				}
			}

			ACDebug.LogWarning ("The scene with build index " + sceneIndex + " was not found in the Build settings.");
			return null;
		}


		protected void OnActiveSceneChanged (Scene oldScene, Scene newScene)
		{
			isLoading = false;
			loadingProgress = 0f;

			SubScene newSceneSubScene = UnityVersionHandler.GetSceneInstance<SubScene> (newScene);
			if (newSceneSubScene)
			{
				// New active scene is a sub-scene
				newSceneSubScene.MakeMain ();
			}
			
			if (!string.IsNullOrEmpty (oldScene.name))
			{
				SceneInfo oldSceneInfo = GetSceneInfo (oldScene.buildIndex);
				if (oldSceneInfo != null)
				{
					MultiSceneChecker multiSceneChecker = MultiSceneChecker.GetSceneInstance (oldScene);
					if (multiSceneChecker != null)
					{
						// Register as a subscene
						GameObject subSceneOb = new GameObject ();
						UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene (subSceneOb, oldScene);
						SubScene oldSubScene = subSceneOb.AddComponent<SubScene> ();
						oldSubScene.Initialise (multiSceneChecker);
					}
				}
			}
		}


		protected void OnAfterChangeScene (LoadingGame loadingGame)
		{
			isLoading = false;
			loadingProgress = 0f;
		}


		protected void OnInitialiseScene ()
		{
			loadingProgress = 0f;
			
			if (KickStarter.settingsManager.inputMethod == InputMethod.KeyboardOrController && simulatedCursorPositionOnExit.x >= 0f)
			{
				KickStarter.playerInput.SetSimulatedCursorPosition (simulatedCursorPositionOnExit);
			}
		}


		protected IEnumerator ScheduleForDeletionCoroutine (GameObject _gameObject)
		{
			yield return new WaitForEndOfFrame ();
			if (_gameObject)
			{ 
				DestroyImmediate (_gameObject);
			}
		}


		protected void LoadLevel (int nextSceneIndex, bool useLoadingScreen, bool useAsyncLoading, bool forceReload, bool doOverlay)
		{
			previousGlobalSceneIndex = CurrentSceneIndex;

			if (useLoadingScreen)
			{
				int loadingSceneIndex = (KickStarter.settingsManager.loadingSceneIs == ChooseSceneBy.Name) ? NameToIndex (KickStarter.settingsManager.loadingSceneName) : KickStarter.settingsManager.loadingScene;
				StartCoroutine (LoadLoadingScreen (nextSceneIndex, loadingSceneIndex, useAsyncLoading, doOverlay));
			}
			else
			{
				if (useAsyncLoading && !forceReload)
				{
					StartCoroutine (LoadLevelAsync (nextSceneIndex, doOverlay));
				}
				else
				{
					StartCoroutine (LoadLevelCo (nextSceneIndex, forceReload, doOverlay));
				}
			}
		}


		protected IEnumerator LoadLoadingScreen (int nextSceneIndex, int loadingSceneIndex, bool loadAsynchronously, bool doOverlay)
		{
			if (preloadSceneIndex >= 0)
			{
				ACDebug.LogWarning ("Cannot use preloaded scene " + preloadSceneIndex + " because the loading scene overrides it - discarding preloaded data.");
			}
			preloadAsync = null;
			preloadSceneIndex = -1;

			isLoading = true;
			loadingProgress = 0f;

			SceneInfo loadingSceneInfo = GetSceneInfo (loadingSceneIndex);
			if (loadingSceneInfo != null)
			{
				loadingSceneInfo.Open ();
				yield return null;

				if (KickStarter.player)
				{
					KickStarter.player.Teleport (KickStarter.player.Transform.position + new Vector3 (0f, -10000f, 0f));
				}
			}

			if (loadAsynchronously)
			{
				if (KickStarter.settingsManager.loadingDelay > 0f)
				{
					float waitForTime = Time.realtimeSinceStartup + KickStarter.settingsManager.loadingDelay;
					while (Time.realtimeSinceStartup < waitForTime && KickStarter.settingsManager.loadingDelay > 0f)
					{
						yield return null;
					}
				}

				SceneInfo nextSceneInfo = GetSceneInfo (nextSceneIndex);
				if (nextSceneInfo != null)
				{
					AsyncOperation aSync = nextSceneInfo.OpenAsync ();

					aSync.allowSceneActivation = false;

					while (aSync.progress < 0.9f)
					{
						loadingProgress = aSync.progress;
						yield return null;
					}

					loadingProgress = 1f;
					
					if (doOverlay)
					{
						yield return new WaitForEndOfFrame ();
						KickStarter.mainCamera.TakeOverlayScreenshot ();
					}

					if (KickStarter.settingsManager.manualSceneActivation)
					{
						if (KickStarter.eventManager)
						{
							completeSceneActivation = false;
							KickStarter.eventManager.Call_OnAwaitSceneActivation (nextSceneIndex);
						}

						while (!completeSceneActivation)
						{
							yield return null;
						}
						completeSceneActivation = false;
					}

					if (KickStarter.settingsManager.loadingDelay > 0f)
					{
						float waitForTime = Time.realtimeSinceStartup + KickStarter.settingsManager.loadingDelay;
						while (Time.realtimeSinceStartup < waitForTime && KickStarter.settingsManager.loadingDelay > 0f)
						{
							yield return null;
						}
					}

					aSync.allowSceneActivation = true;

					KickStarter.stateHandler.IgnoreNavMeshCollisions ();
				}
			}
			else
			{
				SceneInfo nextSceneInfo = GetSceneInfo (nextSceneIndex);
				if (nextSceneInfo != null)
				{
					nextSceneInfo.Open ();
				}
				else
				{
					isLoading = false;
				}
			}
		}


		/**
		 * <summary>Activates the loaded scene, if it must be done so manually</summary>
		 */
		public void ActivateLoadedScene ()
		{
			completeSceneActivation = true;
		}


		protected IEnumerator LoadLevelAsync (int nextSceneIndex, bool doOverlay)
		{
			if (nextSceneIndex >= 0)
			{
				isLoading = true;
				loadingProgress = 0f;
				
				AsyncOperation aSync = null;
				if (nextSceneIndex == preloadSceneIndex)
				{
					aSync = preloadAsync;
					aSync.allowSceneActivation = true;

					while (!aSync.isDone)
					{
						loadingProgress = aSync.progress;
						yield return null;
					}
					loadingProgress = 1f;
				}
				else
				{
					SceneInfo nextSceneInfo = GetSceneInfo (nextSceneIndex);
					if (nextSceneInfo != null)
					{
						aSync = nextSceneInfo.OpenAsync ();

						if (aSync != null)
						{
							aSync.allowSceneActivation = false;

							while (aSync.progress < 0.9f)
							{
								loadingProgress = aSync.progress;
								yield return null;
							}

							loadingProgress = 1f;
							//isLoading = false;

							if (doOverlay)
							{
								yield return new WaitForEndOfFrame ();
								KickStarter.mainCamera.TakeOverlayScreenshot ();
							}

							if (KickStarter.settingsManager.manualSceneActivation)
							{
								if (KickStarter.eventManager)
								{
									completeSceneActivation = false;
									KickStarter.eventManager.Call_OnAwaitSceneActivation (nextSceneIndex);
								}

								while (!completeSceneActivation)
								{
									yield return null;
								}
								completeSceneActivation = false;
							}

							yield return new WaitForEndOfFrame ();

							aSync.allowSceneActivation = true;
						}
						else
						{
							isLoading = false;
						}
					}
				}

				KickStarter.stateHandler.IgnoreNavMeshCollisions ();
				//isLoading = false;
				preloadAsync = null;
				preloadSceneIndex = -1;
			}
		}


		protected IEnumerator PreloadLevelAsync (int nextSceneIndex)
		{
			// Wait for any other loading operations to complete
			while (isLoading)
			{
				yield return null;
			}

			loadingProgress = 0f;

			preloadSceneIndex = nextSceneIndex;

			SceneInfo nextSceneInfo = GetSceneInfo (nextSceneIndex);
			if (nextSceneInfo != null)
			{
				preloadAsync = nextSceneInfo.OpenAsync ();

				preloadAsync.allowSceneActivation = false;

				// Wait until done and collect progress as we go.
				while (!preloadAsync.isDone)
				{
					loadingProgress = preloadAsync.progress;
					if (loadingProgress >= 0.9f)
					{
						// Almost done.
						break;
					}
					loadingProgress = 1f;
					yield return null;
				}

				if (KickStarter.eventManager)
				{
					KickStarter.eventManager.Call_OnCompleteScenePreload (nextSceneIndex);
				}
			}
		}


		protected IEnumerator LoadLevelCo (int nextSceneIndex, bool forceReload, bool doOverlay)
		{
			isLoading = true;
			yield return new WaitForEndOfFrame ();

			if (doOverlay)
			{
				KickStarter.mainCamera.TakeOverlayScreenshot ();
			}

			SceneInfo nextSceneInfo = GetSceneInfo (nextSceneIndex);
			if (nextSceneInfo != null)
			{
				bool opened = nextSceneInfo.Open (forceReload);
				if (!opened)
				{
					isLoading = false;
				}
			}
		}


		protected virtual void PrepareSceneForExit (bool isInstant, bool saveRoomData, bool doOverlay)
		{
			if (isInstant)
			{
				if (!doOverlay)
				{
					KickStarter.mainCamera.FadeOut (0f);
				}
				
				if (KickStarter.player)
				{
					KickStarter.player.EndPath ();
					KickStarter.player.Halt (false);
				}
			}

			if (KickStarter.dialog) KickStarter.dialog.KillDialog (true, true);

			Sound[] sounds = FindObjectsOfType (typeof (Sound)) as Sound[];
			foreach (Sound sound in sounds)
			{
				sound.TryDestroy ();
			}

			KickStarter.playerMenus.ClearParents ();

			if (saveRoomData)
			{
				KickStarter.levelStorage.StoreAllOpenLevelData ();
				
				KickStarter.saveSystem.SaveNonPlayerData (true);
				KickStarter.saveSystem.SaveCurrentPlayerData ();
			}
			subScenes.Clear ();

			if (KickStarter.playerInput)
			{
				simulatedCursorPositionOnExit = KickStarter.playerInput.GetMousePosition ();
			}

			if (KickStarter.eventManager)
			{
				KickStarter.eventManager.Call_OnBeforeChangeScene ();
			}
		}

		#endregion


		#region SubScenes

		public bool SubScenesAreOpen ()
		{
			return (subScenes.Count > 0);
		}


		/**
		 * <summary>Adds a new scene as a sub-scene, without affecting any other open scenes.</summary>
		 * <param name = "subSceneIndex">The index of the new scene to open</param>
		 * <returns>True if the scene was succesfully added</returns>
		 */
		public bool AddSubScene (int subSceneIndex)
		{
			// Check if scene is already open
			if (subSceneIndex == CurrentSceneIndex)
			{
				return false;
			}
		
			foreach (SubScene subScene in subScenes)
			{
				if (subScene.SceneIndex == subSceneIndex)
				{
					return false;
				}
			}

			SceneInfo subSceneInfo = GetSceneInfo (subSceneIndex);
			if (subSceneInfo != null)
			{
				subSceneInfo.Add ();
				return true;
			}

			return false;
		}


		/**
		 * <summary>Registers a SubScene component with the SceneChanger.</summary>
		 * <param name = "subScene">The SubScene component to register</param>
		 */
		public void RegisterSubScene (SubScene subScene)
		{
			if (subScene == null) return;

			foreach (SubScene existingSubScene in subScenes)
			{
				if (subScene == existingSubScene)
				{
					return;
				}

				if (subScene.SceneIndex == existingSubScene.SceneIndex)
				{
					return;
				}
			}

			if (!subScenes.Contains (subScene))
			{
				subScenes.Add (subScene);

				KickStarter.saveSystem.SaveNonPlayerData (false);

				KickStarter.levelStorage.ReturnSubSceneData (subScene);
				KickStarter.eventManager.Call_OnAddSubScene (subScene);
			}
		}


		public void UnregisterSubScene (SubScene subScene)
		{
			if (subScene == null) return;

			foreach (SubScene existingSubScene in subScenes)
			{
				if (subScene == existingSubScene)
				{
					subScenes.Remove (existingSubScene);
					return;
				}
			}
		}


		/**
		 * <summary>Removes a scene, without affecting any other open scenes, provided multiple scenes are open. If the active scene is removed, the last-added sub-scene will become the new active scene.</summary>
		 * <param name = "sceneInfo">The SceneInfo of the new scene to remove</param>
		 * <returns>True if the scene was succesfully removed</returns>
		 */
		public bool RemoveScene (int sceneIndex)
		{
			// Kill actionlists
			KickStarter.actionListManager.KillAllFromScene (sceneIndex);

			if (CurrentSceneIndex == sceneIndex)
			{
				// Want to close active scene

				if (subScenes == null || subScenes.Count == 0)
				{
					ACDebug.LogWarning ("Cannot remove scene " + sceneIndex + ", as it is the only one open!");
					return false;
				}

				// Save active scene
				KickStarter.levelStorage.StoreCurrentLevelData ();

				StartCoroutine (CloseScene (sceneIndex));
				return true;
			}

			// Want to remove a sub-scene
			for (int i=0; i<subScenes.Count; i++)
			{
				if (subScenes[i].SceneIndex == sceneIndex)
				{
					// Save sub scene
					KickStarter.levelStorage.StoreSubSceneData (subScenes[i]);

					StartCoroutine (CloseScene (subScenes[i].SceneIndex));
					subScenes.RemoveAt (i);
					return true;
				}
			}

			return false;
		}


		protected IEnumerator CloseScene (int sceneIndex)
		{
			yield return new WaitForEndOfFrame ();
			SceneInfo sceneInfo = GetSceneInfo (sceneIndex);
			if (sceneInfo != null)
			{
				sceneInfo.Close (true);
			}
		}


		/**
		 * <summary>Saves data used by this script in a PlayerData class.</summary>
		 * <param name = "playerData">The PlayerData to save in.</param>
		 * <returns>The updated PlayerData</returns>
		 */
		public PlayerData SavePlayerData (PlayerData playerData)
		{
			System.Text.StringBuilder subSceneData = new System.Text.StringBuilder ();
			foreach (SubScene subScene in subScenes)
			{
				subSceneData.Append (subScene.SceneIndex + SaveSystem.pipe);
			}
			if (subSceneData.Length > 0)
			{
				subSceneData.Remove (subSceneData.Length-1, 1);
			}
			playerData.openSubScenes = subSceneData.ToString ();

			return playerData;
		}


		/**
		 * <summary>Loads data used by this script from a PlayerData class.</summary>
		 * <param name = "playerData">The PlayerData to load from.</param>
		 * <param name = "loadSubScenes">If True, then sub-scenes will be loaded</param>
		 */
		public void LoadPlayerData (PlayerData playerData, bool loadSubScenes = true)
		{
			foreach (SubScene subScene in subScenes)
			{
				SceneInfo sceneInfo = GetSceneInfo (subScene.SceneIndex);
				if (sceneInfo != null)
				{
					sceneInfo.Close ();
				}
			}
			subScenes.Clear ();
			
			if (loadSubScenes && playerData.openSubScenes != null && playerData.openSubScenes.Length > 0)
			{
				string[] subSceneArray = playerData.openSubScenes.Split (SaveSystem.pipe[0]);
				foreach (string chunk in subSceneArray)
				{
					string[] chunkData = chunk.Split (SaveSystem.colon[0]);

					int _index = chunkData.Length - 1; // For backwards-compat
					int _number = 0;
					if (_index >= 0 && int.TryParse (chunkData[_index], out _number))
					{
						AddSubScene (_number);
					}
				}
			}
		}


		/**
		 * <summary>Saves data used by this script in a MainData class.</summary>
		 * <param name = "mainData">The MainData to save in.</param>
		 * <returns>The updated MainData</returns>
		 */
		public MainData SaveMainData (MainData mainData)
		{
			mainData.previousSceneIndex = previousGlobalSceneIndex;
			return mainData;
		}



		/**
		 * <summary>Loads data used by this script from a MainData class.</summary>
		 * <param name = "mainData">The MainData to load from.</param>
		 */
		public void LoadMainData (MainData mainData)
		{
			previousGlobalSceneIndex = mainData.previousSceneIndex;
		}


		/**
		 * <summary>Gets the previous scene index.
		 * <param name = "forPlayer">If True, the current Player's previous scene will be returned - which may be different from the "global" index if the game makes use of player-switching</param>
		 * <returns>The previous scene index</returns>
		 */
		public int GetPreviousSceneIndex (bool forPlayer = false)
		{
			if (forPlayer)
			{
				PlayerData playerData = KickStarter.saveSystem.GetPlayerData (KickStarter.saveSystem.CurrentPlayerID);
				if (playerData != null)
				{
					return playerData.previousScene;
				}
				return -1;
			}
			return previousGlobalSceneIndex;
		}


		/**
		 * <summary>Gets a SubScene class associated with a given scene. The scene must be currently opened as a sub-scene</summary>
		 * <param name = "sceneIndex">The index of the scene to check for</param>
		 * <returns>The scene's associated SubScene class</returns>
		 */
		public SubScene GetSubScene (int sceneIndex)
		{
			if (sceneIndex < 0) return null;

			foreach (SubScene subScene in subScenes)
			{
				if (subScene.SceneIndex == sceneIndex)
				{
					return subScene;
				}
			}
			return null;
		}

		#endregion


		#region GetSet

		/** All open SubScenes */
		public List<SubScene> SubScenes
		{
			get
			{
				return subScenes;
			}
		}


		/** The current scene index. If multiple scenes are open, this will be the main scene. */
		public static int CurrentSceneIndex
		{
			get
			{
				#if UNITY_EDITOR && UNITY_2019_3_OR_NEWER
				if (UnityEditor.EditorSettings.enterPlayModeOptions.HasFlag (UnityEditor.EnterPlayModeOptions.DisableSceneReload) && UnityEngine.SceneManagement.SceneManager.GetActiveScene ().buildIndex == -1)
				{
					return 0;
				}
				#endif
				return UnityEngine.SceneManagement.SceneManager.GetActiveScene ().buildIndex;
			}
		}


		/** The current scene. If multiple scenes are open, this will be the main scene. */
		public static Scene CurrentScene
		{
			get
			{
				return UnityEngine.SceneManagement.SceneManager.GetActiveScene ();
			}
		}


		/** The index of the previous scene loaded.  This is not necessarily the current Player's previous scene - for that, use GetPreviousSceneIndex (true) */
		public int PreviousSceneIndex
		{
			get
			{
				return previousGlobalSceneIndex;
			}
		}

		#endregion

	}

}