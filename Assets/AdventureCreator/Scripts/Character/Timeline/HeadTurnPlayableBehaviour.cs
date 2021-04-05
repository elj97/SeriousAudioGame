/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2021
 *	
 *	"HeadTurnPlayableBehaviour.cs"
 * 
 *	A PlayableBehaviour used by HeadTurnMixer.
 * 
 */

using UnityEngine;
using UnityEngine.Playables;
#if !ACIgnoreTimeline
using UnityEngine.Timeline;

namespace AC
{

	/**
	 * A PlayableBehaviour used by HeadTurnMixer.
	 */
	internal sealed class HeadTurnPlayableBehaviour : PlayableBehaviour
	{

		#region Variables

		public Transform headTurnTarget;
		public Vector3 headTurnOffset;

		#endregion


		#region GetSet

		public bool IsValid
		{
			get
			{
				return headTurnTarget != null;
			}
		}

		#endregion

	}

}
#endif