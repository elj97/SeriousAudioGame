using UnityEngine;
using UnityEditor;
using System.Collections;

namespace AC
{

	public class ToolbarLinks2DDemo : EditorWindow
	{

		[MenuItem ("Adventure Creator/Getting started/Load 2D Demo", false, 5)]
		static void Demo2D ()
		{
			ManagerPackage package = AssetDatabase.LoadAssetAtPath ("Assets/AdventureCreator/2D Demo/ManagerPackage.asset", typeof (ManagerPackage)) as ManagerPackage;
			if (package != null)
			{
				package.AssignManagers ();
				AdventureCreator.RefreshActions ();

				if (!ACInstaller.IsInstalled ())
				{
					ACInstaller.DoInstall ();
				}

				if (UnityVersionHandler.GetCurrentSceneName () != "Park")
				{
					#if UNITY_5_3 || UNITY_5_4 || UNITY_5_3_OR_NEWER
					bool canProceed = EditorUtility.DisplayDialog ("Open demo scene", "Would you like to open the 2D Demo scene, Park, now?", "Yes", "No");
					if (canProceed)
					{
						if (UnityVersionHandler.SaveSceneIfUserWants ())
						{
							UnityEditor.SceneManagement.EditorSceneManager.OpenScene ("Assets/AdventureCreator/2D Demo/Scenes/Park.unity");
						}
					}
					#else
					ACDebug.Log ("2D Demo managers loaded - you can now run the 2D Demo scene in 'Assets/AdventureCreator/2D Demo/Scenes/Park.unity'");
					#endif
				}

				AdventureCreator.Init ();
			}
		}

	}

}