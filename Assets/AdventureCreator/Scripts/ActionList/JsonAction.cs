/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2021
 *	
 *	"JsonAction.cs"
 * 
 *	A class used to convert Action data to and from Json serialization.  It is primarily used to copying/pasting Actions.
 * 
 */

#if UNITY_2019_2_OR_NEWER
#define NewCopying
#endif

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections.Generic;

namespace AC
{

	[System.Serializable]
	public class JsonAction
	{

		#region Variables

		[SerializeField] private string className;
		[SerializeField] private string jsonData;
		public bool[] endingReferencesBuffer = new bool[0];
		public int[] endingOverrideIndex = new int[0];

		#if UNITY_EDITOR

		#if NewCopying
		private static JsonAction[] copiedActions = new JsonAction[0];
		private const string instanceChecker = "{\"instanceID\":";
		#else
		private static AC.Action[] copiedActions = new AC.Action[0];
		#endif


		#endif

		#endregion


		#region Constructors

		private JsonAction (string _className, string _jsonData, int _endings)
		{
			if (_className.StartsWith ("AC."))
			{
				_className = _className.Substring (3);
			}

			className = _className;
			jsonData = _jsonData;
			endingReferencesBuffer = new bool[_endings];
			endingOverrideIndex = new int[_endings];
		}

		#endregion


		#region PrivateFunctions

		private Action CreateAction ()
		{
			Action newAction = Action.CreateNew (className);
			JsonUtility.FromJsonOverwrite (jsonData, newAction);
			return newAction;
		}


		#if UNITY_EDITOR && NewCopying

		private void InstanceToTarget (HashSet<ActionObjectReference> objectReferences)
		{
			foreach (ActionObjectReference objectReference in objectReferences)
			{
				string _old = instanceChecker + objectReference.InstanceID + "}";
				string _new = instanceChecker + objectReference.PersistentID + "}";

				if (jsonData.Contains (_old))
				{
					jsonData = jsonData.Replace (_old, _new);
				}
			}
		}


		private void TargetToInstance (HashSet<ActionObjectReference> objectReferences)
		{
			foreach (ActionObjectReference objectReference in objectReferences)
			{
				string _old = instanceChecker + objectReference.PersistentID + "}";
				string _new = instanceChecker + objectReference.InstanceID + "}";

				if (jsonData != null && jsonData.Contains (_old))
				{
					jsonData = jsonData.Replace (_old, _new);
				}
			}
		}

		#endif

		#endregion


		#region StaticFunctions

		public static Action CreateCopy (Action action)
		{
			string jsonAction = JsonUtility.ToJson (action);
			string className = action.GetType ().ToString ();

			JsonAction copiedJsonAction = new JsonAction (className, jsonAction, action.endings.Count);
			Action copiedAction = copiedJsonAction.CreateAction ();
			return copiedAction;
		}


		#if UNITY_EDITOR

		/** Clears the copy buffer */
		public static void ClearCopyBuffer ()
		{
			#if NewCopying
			copiedActions = new JsonAction[0];
			#else
			copiedActions = new AC.Action[0];
			#endif
		}


		/**
		 * <summary>Stores an list of Actions in a temporary buffer</summary>
		 * <param name="actions">The list of Actions to store.</param>
		 */
		public static void ToCopyBuffer (List<Action> actions)
		{
			#if NewCopying
			copiedActions = BackupActions (actions);
			#else
			copiedActions = actions.ToArray ();

			copiedActions = new Action[actions.Count];
			for (int i=0; i<actions.Count; i++)
			{
				Action copyAction = Object.Instantiate (actions[i]) as Action;
				copyAction.name = copyAction.name.Replace ("(Clone)", "");
				copyAction.isMarked = false;
				copiedActions[i] = copyAction;
			}

			#endif
		}


		#if NewCopying

		public static JsonAction[] BackupActions (List<Action> actions)
		{
			int length = actions.Count;
			JsonAction[] backupActions = new JsonAction[length];

			// Create initial Json data
			for (int i = 0; i < length; i++)
			{
				if (actions[i] == null)
				{
					backupActions[i] = null;
					continue;
				}

				string jsonAction = JsonUtility.ToJson (actions[i]);

				string className = actions[i].GetType ().ToString ();
				backupActions[i] = new JsonAction (className, jsonAction, actions[i].endings.Count);

				for (int e = 0; e < actions[i].endings.Count; e++)
				{
					if (actions[i].endings[e].resultAction == ResultAction.Skip && actions[i].endings[e].skipActionActual != null && actions.Contains (actions[i].endings[e].skipActionActual))
					{
						// References an Action inside the copy buffer, so record the index in the buffer
						backupActions[i].endingReferencesBuffer[e] = true;
						backupActions[i].endingOverrideIndex[e] = actions.IndexOf (actions[i].endings[e].skipActionActual);
					}
				}
			}

			// Get reference data for all scene objects
			HashSet<ActionObjectReference> globalObjectIds = GetSceneObjectReferences ();

			// Amend Json data by replacing InstanceID references with TargetID references
			foreach (JsonAction backupAction in backupActions)
			{
				if (backupAction != null)
				{
					backupAction.InstanceToTarget (globalObjectIds);
				}
			}

			return backupActions;
		}


		public static List<Action> RestoreActions (JsonAction[] jsonActions, bool createNew = false)
		{
			HashSet<ActionObjectReference> globalObjectIds = GetSceneObjectReferences ();

			// Amend Json data by replacing TargetID references with InstanceID references
			foreach (JsonAction jsonAction in jsonActions)
			{
				jsonAction.TargetToInstance (globalObjectIds);
			}

			// Create Actions fromJson
			List<Action> newActions = new List<Action> ();
			for (int i = 0; i < jsonActions.Length; i++)
			{
				if (jsonActions[i] == null)
				{
					continue;
				}

				Action newAction = jsonActions[i].CreateAction ();

				if (newAction == null)
				{
					ACDebug.LogWarning ("Error when pasting Action - cannot find original.");
				}
				else if (createNew)
				{
					newAction.ClearIDs ();
					//newAction.NodeRect = new Rect (0, 0, 300, 60);
				}

				newActions.Add (newAction);
			}

			// Correct skip endings for those that reference others in the same list
			for (int i = 0; i < newActions.Count; i++)
			{
				if (newActions[i] == null)
				{
					continue;
				}

				for (int e = 0; e < jsonActions[i].endingReferencesBuffer.Length; e++)
				{
					bool endingIsOffset = jsonActions[i].endingReferencesBuffer[e];
					if (endingIsOffset)
					{
						newActions[i].endings[e].skipAction = -1;
						newActions[i].endings[e].skipActionActual = newActions[jsonActions[i].endingOverrideIndex[e]];
					}
				}
			}

			// Remove nulls
			for (int i = 0; i < newActions.Count; i++)
			{
				if (newActions[i] == null)
				{
					newActions.RemoveAt (i);
					i = -1;
				}
			}

			return newActions;
		}


		private static HashSet<ActionObjectReference> GetSceneObjectReferences ()
		{
			HashSet<Object> sceneObjects = new HashSet<Object> ();
			GameObject[] sceneGameObjects = Object.FindObjectsOfType<GameObject> ();
			foreach (GameObject sceneGameObject in sceneGameObjects)
			{
				sceneObjects.Add (sceneGameObject);
				Component[] components = sceneGameObject.GetComponents<Component> ();
				foreach (Component component in components)
				{
					sceneObjects.Add (component);
				}
			}

			HashSet<ActionObjectReference> objectReferences = new HashSet<ActionObjectReference> ();
			foreach (Object sceneObject in sceneObjects)
			{
				if (sceneObject == null) continue;
				objectReferences.Add (new ActionObjectReference (sceneObject));
			}
			return objectReferences;
		}

		#endif


		/**
		 * <summary>Generates Actions based on the buffer created with ToCopyBuffer</summary>
		 * <returns>The Actions stored in the buffer, recreated.</returns>
		 */
		public static List<Action> CreatePasteBuffer ()
		{
			#if NewCopying

			return RestoreActions (copiedActions, true);

			#else

			List<AC.Action> tempList = new List<AC.Action>();
			foreach (AC.Action action in copiedActions)
			{
				if (action != null)
				{
					Action copyAction = Object.Instantiate (action) as Action;
					copyAction.ClearIDs ();
					foreach (ActionEnd ending in copyAction.endings)
					{
						ending.skipActionActual = null;
					}
					tempList.Add (copyAction);
				}
			}
			
			copiedActions = new AC.Action[0];
			return tempList;

			#endif
		}


		/** Return True if Action data is stored in the copy buffer */
		public static bool HasCopyBuffer ()
		{
			return (copiedActions != null && copiedActions.Length > 0);
		}

		#endif

		#endregion


		#region PrivateClasses

		#if UNITY_EDITOR && NewCopying

		private class ActionObjectReference
		{

			public string PersistentID { get; private set; }
			public string InstanceID { get; private set; }


			public ActionObjectReference (Object _object)
			{
				GlobalObjectId globalObjectId = GlobalObjectId.GetGlobalObjectIdSlow (_object);
				PersistentID = globalObjectId.targetObjectId.ToString ();
				InstanceID = _object.GetInstanceID ().ToString ();
			}

		}

		#endif

		#endregion

	}

}