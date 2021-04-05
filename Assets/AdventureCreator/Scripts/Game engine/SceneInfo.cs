/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2021
 *	
 *	SceneInfo.cs"
 * 
 *	A data container for an actual scene in the build.
 * 
 */

using UnityEngine;

namespace AC
{

	/**
	 * A data container for an actual scene in the build.
	 */
	public class SceneInfo
	{

		#region GetSet

		private int buildIndex;
		private string filename;

		#endregion


		#region Constructors

		/** The default constructor */
		public SceneInfo (int _buildIndex, string _filename)
		{
			buildIndex = _buildIndex;
			filename = _filename;
		}

		#endregion


		#region PublicFunctions

		/**
		 * <summary>Checks if this represents the currently-active main scene</summary>
		 * <returns>True if this represents the currently-active main scene</returns>
		 */
		public bool IsCurrentActive ()
		{
			return buildIndex == SceneChanger.CurrentSceneIndex;
		}


		/**
		 * <summary>Loads the scene normally.</summary>
		 * <param name = "forceReload">If True, the scene will be re-loaded if it is already open.</param>
		 */
		public bool Open (bool forceReload = false)
		{
			return Open (forceReload, UnityEngine.SceneManagement.LoadSceneMode.Single);
		}


		/**
		 * <summary>Adds the scene additively.</summary>
		 */
		public void Add ()
		{
			Open (false, UnityEngine.SceneManagement.LoadSceneMode.Additive);
		}


		/**
		 * <summary>Closes the scene additively.</summary>
		 */
		public void Close (bool evenIfCurrent = false)
		{
			if (evenIfCurrent || !IsCurrentActive ())
			{
				UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync (buildIndex);
			}
		}


		/**
		 * <summary>Loads the scene asynchronously.</summary>
		 * <returns>The generated AsyncOperation class</returns>
		 */
		public AsyncOperation OpenAsync ()
		{
			return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync (buildIndex);
		}

		#endregion


		#region PrivateFunctions

		private bool Open (bool forceReload, UnityEngine.SceneManagement.LoadSceneMode loadSceneMode)
		{
			if (KickStarter.settingsManager.reloadSceneWhenLoading)
			{
				forceReload = true;
			}

			try
			{
				if (forceReload || !IsCurrentActive ())
				{
					UnityEngine.SceneManagement.SceneManager.LoadScene (buildIndex, loadSceneMode);
					return true;
				}
			}
			catch (System.Exception e)
			{
				Debug.LogWarning ("Error when opening scene " + buildIndex + ": " + e);
			}
			return false;
		}

		#endregion


		#region GetSet

		/** The scene's filename, without extension or filepath */
		public string Filename
		{
			get
			{
				return filename;
			}
		}


		/** The scene's build index number */
		public int BuildIndex
		{
			get
			{
				return buildIndex;
			}
		}

		#endregion

	}

}