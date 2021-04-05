/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2021
 *	
 *	"ActionInventoryCrafting.cs"
 * 
 *	This action is used to perform crafting-related tasks.
 * 
 */

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	[System.Serializable]
	public class ActionInventoryCrafting : Action
	{

		public enum ActionCraftingMethod { ClearRecipe, CreateRecipe };
		public ActionCraftingMethod craftingMethod;


		public override ActionCategory Category { get { return ActionCategory.Inventory; }}
		public override string Title { get { return "Crafting"; }}
		public override string Description { get { return "Either clears the current arrangement of crafting ingredients, or evaluates them to create an appropriate result (if this is not done automatically by the recipe itself)."; }}


		public override float Run ()
		{
			switch (craftingMethod)
			{
				case ActionCraftingMethod.ClearRecipe:
					KickStarter.runtimeInventory.RemoveRecipes ();
					break;

				case ActionCraftingMethod.CreateRecipe:
					PlayerMenus.CreateRecipe ();
					break;

				default:
					break;
			}

			return 0f;
		}
		
		
		#if UNITY_EDITOR
		
		public override void ShowGUI ()
		{
			craftingMethod = (ActionCraftingMethod) EditorGUILayout.EnumPopup ("Method:", craftingMethod);
		}
		
		
		public override string SetLabel ()
		{
			switch (craftingMethod)
			{
				case ActionCraftingMethod.CreateRecipe:
					return "Create recipe";
					
				case ActionCraftingMethod.ClearRecipe:
					return "Clear recipe";
					
				default:
					return string.Empty;
			}
		}
		
		#endif


		/**
		 * <summary>Creates a new instance of the 'Inventory: Crafting' Action</summary>
		 * <param name = "craftingMethod">The crafting method to perform</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionInventoryCrafting CreateNew (ActionCraftingMethod craftingMethod)
		{
			ActionInventoryCrafting newAction = CreateNew<ActionInventoryCrafting> ();
			newAction.craftingMethod = craftingMethod;
			return newAction;
		}
	}

}