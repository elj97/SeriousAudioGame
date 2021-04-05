/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2021
 *	
 *	"SpeechPlayableClip.cs"
 * 
 *	A PlayableAsset used by SpeechPlayableBehaviour
 * 
 */

#if UNITY_EDITOR
using UnityEditor;
#endif

#if !ACIgnoreTimeline
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace AC
{

	/**
	 * A PlayableAsset used by SpeechPlayableBehaviour
	 */
	[System.Serializable]
	public class SpeechPlayableClip : PlayableAsset, ITimelineClipAsset
	{

		#region Variables

		/** If True, the line is spoken by the Player */
		public bool isPlayerLine;
		/** The ID of the Player, if not the active one */
		public int playerID = -1;
		/** The speaking character */
		public Char speaker;
		/** Data for the speech line itself */
		public SpeechPlayableData speechPlayableData;
		/** The playback mode */
		public SpeechTrackPlaybackMode speechTrackPlaybackMode;

		public int trackInstanceID;


		#endregion


		#region PublicFunctions

		public override Playable CreatePlayable (PlayableGraph graph, GameObject owner)
		{
			SpeechPlayableBehaviour template = new SpeechPlayableBehaviour ();
			var playable = ScriptPlayable<SpeechPlayableBehaviour>.Create (graph, template);
			SpeechPlayableBehaviour clone = playable.GetBehaviour ();

			clone.Init (speechPlayableData, speaker, isPlayerLine, playerID, speechTrackPlaybackMode, trackInstanceID);

			return playable;
		}


		public string GetDisplayName ()
		{
			if (!string.IsNullOrEmpty (speechPlayableData.messageText))
			{
				return speechPlayableData.messageText;
			}
			return "Speech text";
		}

		#endregion


		#if UNITY_EDITOR

		public void ShowGUI ()
		{
			if (speechPlayableData.lineID > -1)
			{
				EditorGUILayout.LabelField ("Speech Manager ID:", speechPlayableData.lineID.ToString ());
			}

			speechPlayableData.messageText = CustomGUILayout.TextArea ("Line text:", speechPlayableData.messageText);
		}

		#endif


		#region GetSet

		public ClipCaps clipCaps
		{
			get
			{
				return ClipCaps.None;
			}
		}

		#endregion

	}

}

#endif