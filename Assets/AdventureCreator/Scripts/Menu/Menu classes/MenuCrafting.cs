/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2021
 *	
 *	"MenuCrafting.cs"
 * 
 *	This MenuElement stores multiple Inventory Items to be combined.
 * 
 */

using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	/**
	 * A MenuElement that stores multiple inventory items to be combined to create new ones.
	 */
	public class MenuCrafting : MenuElement
	{

		/** A List of UISlot classes that reference the linked Unity UI GameObjects (Unity UI Menus only) */
		public UISlot[] uiSlots;

		/** The special FX applied to the text (None, Outline, Shadow, OutlineAndShadow) */
		public TextEffects textEffects;
		/** The outline thickness, if textEffects != TextEffects.None */
		public float outlineSize = 2f;
		/** What part of the crafting process this element is used for (Ingredients, Output) */
		public CraftingElementType craftingType = CraftingElementType.Ingredients;
		/** The List of InvItem instances that are currently on display */
		private List<InvInstance> invInstances = new List<InvInstance>();
		/** How items are displayed (IconOnly, TextOnly, IconAndText) */
		public ConversationDisplayType displayType = ConversationDisplayType.IconOnly;
		/** The method by which this element (or slots within it) are hidden from view when made invisible (DisableObject, ClearContent) */
		public UIHideStyle uiHideStyle = UIHideStyle.DisableObject;
		/** If craftingType = CraftingElementType.Output, the ActionList to run if a crafting attempt is made but no succesful recipe is possible. This only works if crafting is performed manually via the Inventory: Crafting Action. */
		public ActionListAsset actionListOnWrongIngredients;
		/** What Image component the Element's Graphics should be linked to (ImageComponent, ButtonTargetGraphic) */
		public LinkUIGraphic linkUIGraphic = LinkUIGraphic.ImageComponent;
		/** If True, and craftingType = CraftingElementYpe.Output, then outputs will appear automatically when the correct ingredients are used. If False, then the player will have to run the "Inventory: Crafting" Action as an additional step. */
		public bool autoCreate = true;
		/** How the item count is displayed */
		public InventoryItemCountDisplay inventoryItemCountDisplay = InventoryItemCountDisplay.OnlyIfMultiple;
		/** If inventoryBoxType = AC_InventoryBoxType.Container, what happens to items when they are removed from the container */
		public ContainerSelectMode containerSelectMode = ContainerSelectMode.MoveToInventoryAndSelect;

		private Recipe activeRecipe;
		private string[] labels = null;


		/**
		 * Initialises the element when it is created within MenuManager.
		 */
		public override void Declare ()
		{
			uiSlots = null;
			isVisible = true;
			isClickable = true;
			numSlots = 4;
			SetSize (new Vector2 (6f, 10f));
			textEffects = TextEffects.None;
			outlineSize = 2f;
			craftingType = CraftingElementType.Ingredients;
			displayType = ConversationDisplayType.IconOnly;
			uiHideStyle = UIHideStyle.DisableObject;
			actionListOnWrongIngredients = null;
			linkUIGraphic = LinkUIGraphic.ImageComponent;
			invInstances = new List<InvInstance>();
			autoCreate = true;
			inventoryItemCountDisplay = InventoryItemCountDisplay.OnlyIfMultiple;
			containerSelectMode = ContainerSelectMode.MoveToInventoryAndSelect;
		}


		/**
		 * <summary>Creates and returns a new MenuCrafting that has the same values as itself.</summary>
		 * <param name = "fromEditor">If True, the duplication was done within the Menu Manager and not as part of the gameplay initialisation.</param>
		 * <returns>A new MenuCrafting with the same values as itself</returns>
		 */
		public override MenuElement DuplicateSelf (bool fromEditor, bool ignoreUnityUI)
		{
			MenuCrafting newElement = CreateInstance <MenuCrafting>();
			newElement.Declare ();
			newElement.CopyCrafting (this, ignoreUnityUI);
			return newElement;
		}
		
		
		private void CopyCrafting (MenuCrafting _element, bool ignoreUnityUI)
		{
			if (ignoreUnityUI)
			{
				uiSlots = null;
			}
			else
			{
				uiSlots = new UISlot[_element.uiSlots.Length];
				for (int i=0; i<uiSlots.Length; i++)
				{
					uiSlots[i] = new UISlot (_element.uiSlots[i]);
				}
			}

			isClickable = _element.isClickable;
			textEffects = _element.textEffects;
			outlineSize = _element.outlineSize;
			numSlots = _element.numSlots;
			craftingType = _element.craftingType;
			displayType = _element.displayType;
			uiHideStyle = _element.uiHideStyle;
			actionListOnWrongIngredients = _element.actionListOnWrongIngredients;
			linkUIGraphic = _element.linkUIGraphic;
			autoCreate = _element.autoCreate;
			inventoryItemCountDisplay = _element.inventoryItemCountDisplay;
			containerSelectMode = _element.containerSelectMode;

			PopulateList ();
			
			base.Copy (_element);
		}


		public override void LoadUnityUI (AC.Menu _menu, Canvas canvas, bool addEventListeners = true)
		{
			int i=0;
			foreach (UISlot uiSlot in uiSlots)
			{
				uiSlot.LinkUIElements (canvas, linkUIGraphic);

				if (addEventListeners)
				{
					if (uiSlot != null && uiSlot.uiButton)
					{
						int j=i;

						uiSlot.uiButton.onClick.AddListener (() => {
							ProcessClickUI (_menu, j, MouseState.SingleClick);
						});
					}
				}
				i++;
			}
		}


		public override GameObject GetObjectToSelect (int slotIndex = 0)
		{
			if (uiSlots != null && uiSlots.Length > slotIndex && uiSlots[slotIndex].uiButton)
			{
				return uiSlots[slotIndex].uiButton.gameObject;
			}
			return null;
		}
		

		/**
		 * <summary>Gets the boundary of the slot</summary>
		 * <param name = "_slot">The index number of the slot to get the boundary of</param>
		 * <returns>The boundary Rect of the slot</returns>
		 */
		public override RectTransform GetRectTransform (int _slot)
		{
			if (uiSlots != null && _slot >= 0 && _slot < uiSlots.Length)
			{
				return uiSlots[_slot].GetRectTransform ();
			}
			return null;
		}


		public override void SetUIInteractableState (bool state)
		{
			SetUISlotsInteractableState (uiSlots, state);
		}
		
		
		#if UNITY_EDITOR
		
		public override void ShowGUI (Menu menu)
		{
			string apiPrefix = "(AC.PlayerMenus.GetElementWithName (\"" + menu.title + "\", \"" + title + "\") as AC.MenuCrafting)";

			MenuSource source = menu.menuSource;

			CustomGUILayout.BeginVertical ();

			craftingType = (CraftingElementType) CustomGUILayout.EnumPopup ("Crafting element type:", craftingType, apiPrefix + ".craftingType", "What part of the crafting process this element is used for");

			if (craftingType == CraftingElementType.Ingredients)
			{
				numSlots = CustomGUILayout.IntSlider ("Number of slots:", numSlots, 1, 12);
				if (source == MenuSource.AdventureCreator && numSlots > 1)
				{
					slotSpacing = EditorGUILayout.Slider (new GUIContent ("Slot spacing:", "The distance between slots"), slotSpacing, 0f, 20f);
					orientation = (ElementOrientation) CustomGUILayout.EnumPopup ("Slot orientation:", orientation, apiPrefix + ".orientation", "The slot orientation");
					if (orientation == ElementOrientation.Grid)
					{
						gridWidth = CustomGUILayout.IntSlider ("Grid size:", gridWidth, 1, 10, apiPrefix + ".gridWidth");
					}
				}
				containerSelectMode = (ContainerSelectMode) CustomGUILayout.EnumPopup ("Behaviour after taking?", containerSelectMode, apiPrefix + ".containerSelectMode", "What happens to items when they are taken");
			}
			else
			{
				autoCreate = CustomGUILayout.Toggle ("Result is automatic?", autoCreate, apiPrefix + ".autoCreate", "If True, then the output ingredient will appear automatically when the correct ingredients are used. If False, then the player will have to run the 'Inventory: Crafting' Action as an additional step.");

				numSlots = 1;
				actionListOnWrongIngredients = ActionListAssetMenu.AssetGUI ("ActionList on fail:", actionListOnWrongIngredients, menu.title + "_OnFailRecipe", apiPrefix + ".actionListOnWrongIngredients", "Ahe ActionList asset to run if a crafting attempt is made but no succesful recipe is possible. This only works if crafting is performed manually via the Inventory: Crafting Action.");
				if (actionListOnWrongIngredients != null)
				{
					EditorGUILayout.HelpBox ("This ActionList will only be run if the result is calculated manually via the 'Inventory: Crafting' Action.", MessageType.Info);
				}
			}

			displayType = (ConversationDisplayType) CustomGUILayout.EnumPopup ("Display type:", displayType, apiPrefix + ".displayType", "How items are displayed");
			if (displayType == ConversationDisplayType.IconAndText && source == MenuSource.AdventureCreator)
			{
				EditorGUILayout.HelpBox ("'Icon And Text' mode is only available for Unity UI-based Menus.", MessageType.Warning);
			}

			inventoryItemCountDisplay = (InventoryItemCountDisplay) CustomGUILayout.EnumPopup ("Display item amounts:", inventoryItemCountDisplay, apiPrefix + ".inventoryItemCountDisplay", "How item counts are drawn");

			if (source != MenuSource.AdventureCreator)
			{
				CustomGUILayout.EndVertical ();
				CustomGUILayout.BeginVertical ();
				uiHideStyle = (UIHideStyle) CustomGUILayout.EnumPopup ("When invisible:", uiHideStyle, apiPrefix + ".uiHideStyle", "The method by which this element (or slots within it) are hidden from view when made invisible");
				EditorGUILayout.LabelField ("Linked button objects", EditorStyles.boldLabel);

				uiSlots = ResizeUISlots (uiSlots, numSlots);
				
				for (int i=0; i<uiSlots.Length; i++)
				{
					uiSlots[i].LinkedUiGUI (i, source);
				}

				linkUIGraphic = (LinkUIGraphic) CustomGUILayout.EnumPopup ("Link graphics to:", linkUIGraphic, "", "What Image component the element's graphics should be linked to");
			}

			isClickable = true;
			CustomGUILayout.EndVertical ();
			
			PopulateList ();
			base.ShowGUI (menu);
		}


		protected override void ShowTextGUI (string apiPrefix)
		{
			textEffects = (TextEffects) CustomGUILayout.EnumPopup ("Text effect:", textEffects, apiPrefix + ".textEffects", "The special FX applied to the text");
			if (textEffects != TextEffects.None)
			{
				outlineSize = CustomGUILayout.Slider ("Effect size:", outlineSize, 1f, 5f, apiPrefix + ".outlineSize", "The outline thickness");
			}
		}


		public override bool ReferencesAsset (ActionListAsset actionListAsset)
		{
			if (craftingType != CraftingElementType.Ingredients && actionListOnWrongIngredients == actionListAsset)
				return true;
			return false;
		}

		#endif


		public override bool ReferencesObjectOrID (GameObject gameObject, int id)
		{
			foreach (UISlot uiSlot in uiSlots)
			{
				if (uiSlot.uiButton && uiSlot.uiButton.gameObject == gameObject) return true;
				if (uiSlot.uiButtonID == id && id != 0) return true;
			}
			return false;
		}

		public override void HideAllUISlots ()
		{
			LimitUISlotVisibility (uiSlots, 0, uiHideStyle);
		}


		public override string GetHotspotLabelOverride (int _slot, int _language)
		{
			if (uiSlots != null && _slot < uiSlots.Length && !uiSlots[_slot].CanOverrideHotspotLabel) return string.Empty;

			InvItem invItem = GetItem (_slot);
			if (invItem != null)
			{
				if (_language > 0)
				{
					return KickStarter.runtimeLanguages.GetTranslation (invItem.label, invItem.lineID, _language, AC_TextType.InventoryItem);
				}

				if (!string.IsNullOrEmpty (invItem.altLabel))
				{
					return invItem.altLabel;
				}
				
				return invItem.GetLabel (_language);
			}

			return string.Empty;
		}
		
		
		public override void PreDisplay (int _slot, int languageNumber, bool isActive)
		{
			string fullText = string.Empty;
			if (displayType == ConversationDisplayType.TextOnly || displayType == ConversationDisplayType.IconAndText)
			{
				InvItem invItem = GetItem (_slot);
				if (invItem != null)
				{
					fullText = invItem.GetLabel (languageNumber);
				}

				string countText = GetCount (_slot);
				if (!string.IsNullOrEmpty (countText))
				{
					fullText += " (" + countText + ")";
				}
			}
			else
			{
				string countText = GetCount (_slot);
				if (!string.IsNullOrEmpty (countText))
				{
					fullText = countText;
				}
			}

			if (labels == null || labels.Length != numSlots)
			{
				labels = new string [numSlots];
			}
			labels [_slot] = fullText;

			if (Application.isPlaying)
			{
				if (uiSlots != null && uiSlots.Length > _slot)
				{
					LimitUISlotVisibility (uiSlots, numSlots, uiHideStyle);

					uiSlots[_slot].SetText (labels [_slot]);

					switch (displayType)
					{
						case ConversationDisplayType.IconOnly:
						case ConversationDisplayType.TextOnly:
							if ((craftingType == CraftingElementType.Ingredients && GetItem (_slot) != null) || (craftingType == CraftingElementType.Output && invInstances.Count > 0))
							{
								uiSlots[_slot].SetImage (GetTexture (_slot));
							}
							else
							{
								uiSlots[_slot].SetImage (null);
							}
							break;

						default:
							break;
					}
				}
			}
		}


		/**
		 * <summary>Draws the element using OnGUI</summary>
		 * <param name = "_style">The GUIStyle to draw with</param>
		 * <param name = "_slot">The index number of the slot to display</param>
		 * <param name = "zoom">The zoom factor</param>
		 * <param name = "isActive">If True, then the element will be drawn as though highlighted</param>
		 */
		public override void Display (GUIStyle _style, int _slot, float zoom, bool isActive)
		{
			base.Display (_style, _slot, zoom, isActive);

			if (craftingType == CraftingElementType.Ingredients)
			{
				if (Application.isPlaying && KickStarter.settingsManager.selectInventoryDisplay == SelectInventoryDisplay.HideFromMenu && ItemIsSelected (_slot))
				{
					if (!invInstances[_slot].IsPartialTransform ())
					{
						// Display as normal if we only have one selected from many
						return;
					}
				}

				if (displayType == ConversationDisplayType.IconOnly)
				{
					GUI.Label (GetSlotRectRelative (_slot), string.Empty, _style);

					if (Application.isPlaying && GetItem (_slot) == null)
					{
						return;
					}
					DrawTexture (ZoomRect (GetSlotRectRelative (_slot), zoom), _slot);
					_style.normal.background = null;
				}
				else
				{
					if (GetItem (_slot) == null && Application.isPlaying)
					{
						GUI.Label (GetSlotRectRelative (_slot), string.Empty, _style);
					}
				}

				DrawText (_style, _slot, zoom);
			}
			else if (craftingType == CraftingElementType.Output)
			{
				GUI.Label (GetSlotRectRelative (_slot), string.Empty, _style);
				if (invInstances.Count > 0)
				{
					if (displayType == ConversationDisplayType.IconOnly)
					{
						DrawTexture (ZoomRect (GetSlotRectRelative (_slot), zoom), _slot);
					}
					DrawText (_style, _slot, zoom);
				}
			}
		}


		private void DrawText (GUIStyle _style, int _slot, float zoom)
		{
			if (_slot >= labels.Length) return;
			if (textEffects != TextEffects.None)
			{
				AdvGame.DrawTextEffect (ZoomRect (GetSlotRectRelative (_slot), zoom), labels[_slot], _style, Color.black, _style.normal.textColor, outlineSize, textEffects);
			}
			else
			{
				GUI.Label (ZoomRect (GetSlotRectRelative (_slot), zoom), labels[_slot], _style);
			}
		}


		private bool HandleDefaultClick (MouseState _mouseState, int _slot)
		{
			InvInstance clickedInstance = GetInstance (_slot);

			if (_mouseState == MouseState.SingleClick)
			{
				if (!InvInstance.IsValid (KickStarter.runtimeInventory.SelectedInstance))
				{
					if (InvInstance.IsValid (clickedInstance))
					{
						// Clicked on an item while nothing selected

						switch (containerSelectMode)
						{
							case ContainerSelectMode.MoveToInventory:
							case ContainerSelectMode.MoveToInventoryAndSelect:
								bool selectItem = (containerSelectMode == ContainerSelectMode.MoveToInventoryAndSelect);

								ItemStackingMode itemStackingMode = clickedInstance.ItemStackingMode;
								if (itemStackingMode != ItemStackingMode.All)
								{
									// Only take one
									clickedInstance.TransferCount = 1;
								}

								InvInstance newInstance = KickStarter.runtimeInventory.PlayerInvCollection.AddToEnd (clickedInstance);
								if (selectItem && InvInstance.IsValid (newInstance))
								{
									KickStarter.runtimeInventory.SelectItem (newInstance);
								}
								break;

							case ContainerSelectMode.SelectItemOnly:
								KickStarter.runtimeInventory.SelectItem (clickedInstance);
								break;
						}

						return true;
					}
				}
				else
				{
					// Clicked while selected

					if (KickStarter.runtimeInventory.SelectedInstance == clickedInstance && KickStarter.runtimeInventory.SelectedInstance.CanStack ())
					{
						KickStarter.runtimeInventory.SelectedInstance.AddStack ();
					}
					else
					{
						KickStarter.runtimeInventory.CraftingInvCollection.Insert (KickStarter.runtimeInventory.SelectedInstance, _slot, OccupiedSlotBehaviour.FailTransfer);
						KickStarter.runtimeInventory.SetNull ();
					}

					return true;
				}
			}
			else if (_mouseState == MouseState.RightClick)
			{
				if (InvInstance.IsValid (KickStarter.runtimeInventory.SelectedInstance))
				{
					if (KickStarter.runtimeInventory.SelectedInstance == clickedInstance && KickStarter.runtimeInventory.SelectedInstance.ItemStackingMode == ItemStackingMode.Stack)
					{
						KickStarter.runtimeInventory.SelectedInstance.RemoveStack ();
					}
					else
					{
						KickStarter.runtimeInventory.SetNull ();
					}

					return true;
				}
			}

			return false;
		}
		

		private bool ClickOutput (AC.Menu _menu, MouseState _mouseState)
		{
			if (invInstances.Count > 0)
			{
				if (_mouseState == MouseState.SingleClick && !InvInstance.IsValid (KickStarter.runtimeInventory.SelectedInstance))
				{
					// Pick up created item
					switch (activeRecipe.onCreateRecipe)
					{
						case OnCreateRecipe.SelectItem:
							KickStarter.runtimeInventory.PerformCrafting (activeRecipe, true);
							break;

						case OnCreateRecipe.JustMoveToInventory:
							KickStarter.runtimeInventory.PerformCrafting (activeRecipe, false);
							break;

						case OnCreateRecipe.RunActionList:
							KickStarter.runtimeInventory.PerformCrafting (activeRecipe, false);
							if (activeRecipe.invActionList)
							{
								AdvGame.RunActionListAsset (activeRecipe.invActionList);
							}
							break;

						default:
							break;
					}

					return true;
				}
			}

			return false;
		}


		/**
		 * <summary>Recalculates the element's size.
		 * This should be called whenever a Menu's shape is changed.</summary>
		 * <param name = "source">How the parent Menu is displayed (AdventureCreator, UnityUiPrefab, UnityUiInScene)</param>
		 */
		public override void RecalculateSize (MenuSource source)
		{
			PopulateList ();

			if (Application.isPlaying && uiSlots != null)
			{
				ClearSpriteCache (uiSlots);
			}

			if (!isVisible)
			{
				LimitUISlotVisibility (uiSlots, 0, uiHideStyle);
			}

			base.RecalculateSize (source);
		}
		
		
		private void PopulateList ()
		{
			if (Application.isPlaying)
			{
				switch (craftingType)
				{
					case CraftingElementType.Ingredients:
						invInstances = KickStarter.runtimeInventory.CraftingInvCollection.InvInstances;
						return;

					case CraftingElementType.Output:
						if (autoCreate)
						{
							SetOutput ();
						}
						else if (activeRecipe != null)
						{
							Recipe recipe = KickStarter.runtimeInventory.CalculateRecipe ();
							if (recipe != activeRecipe)
							{
								activeRecipe = null;
								invInstances = new List<InvInstance>();
							}
						}
						return;

					default:
						break;
				}
			}
			else
			{
				invInstances = new List<InvInstance>();
				if (AdvGame.GetReferences ().inventoryManager)
				{
					foreach (InvItem _item in AdvGame.GetReferences ().inventoryManager.items)
					{
						invInstances.Add (new InvInstance (_item));

						if (craftingType == CraftingElementType.Output)
						{
							return;
						}
						else if (numSlots <= invInstances.Count)
						{
							return;
						}
					}
				}
				return;
			}
		}


		/**
		 * <summary>Creates and displays the correct InvItem, based on the current Recipe, provided craftingType = CraftingElementType.Output.</summary>
		 */
		public void SetOutput ()
		{
			if (craftingType != CraftingElementType.Output)
			{
				return;
			}

			invInstances = new List<InvInstance>();

			activeRecipe = KickStarter.runtimeInventory.CalculateRecipe ();
			if (activeRecipe != null)
			{
				if (activeRecipe.actionListOnCreate && !KickStarter.actionListAssetManager.IsListRunning (activeRecipe.actionListOnCreate))
				{
					AdvGame.RunActionListAsset (activeRecipe.actionListOnCreate);
				}

				foreach (InvItem assetItem in AdvGame.GetReferences ().inventoryManager.items)
				{
					if (activeRecipe != null && assetItem.id == activeRecipe.resultID)
					{
						invInstances.Add (new InvInstance (assetItem, 1));
					}
				}

				KickStarter.eventManager.Call_OnCraftingSucceed (activeRecipe);
			}
			else
			{
				if (!autoCreate && actionListOnWrongIngredients)
				{
					actionListOnWrongIngredients.Interact ();
				}
			}
		}

		
		private Texture GetTexture (int i)
		{
			InvItem invItem = GetItem (i);
			if (invItem != null)
			{
				return invItem.tex;
			}
			return null;
		}

		
		private void DrawTexture (Rect rect, int i)
		{
			Texture tex = GetTexture (i);

			if (tex)
			{
				GUI.DrawTexture (rect, tex, ScaleMode.StretchToFill, true, 0f);
			}
		}
		

		/**
		 * <summary>Gets the display text of the element</summary>
		 * <param name = "i">The index number of the slot</param>
		 * <param name = "languageNumber">The index number of the language number to get the text in</param>
		 * <returns>The display text of the element's slot, or the whole element if it only has one slot</returns>
		 */
		public override string GetLabel (int i, int languageNumber)
		{
			InvItem invItem = GetItem (i);
			if (invItem == null)
			{
				return string.Empty;
			}

			if (languageNumber > 0)
			{
				return KickStarter.runtimeLanguages.GetTranslation (invItem.label, invItem.lineID, languageNumber, AC_TextType.InventoryItem);
			}
			if (!string.IsNullOrEmpty (invItem.altLabel))
			{
				return invItem.altLabel;
			}
			
			return invItem.label;
		}


		public override bool IsSelectedByEventSystem (int slotIndex)
		{
			if (uiSlots != null && slotIndex >= 0 && uiSlots.Length > slotIndex && uiSlots[slotIndex] != null && uiSlots[slotIndex].uiButton)
			{
				return KickStarter.playerMenus.IsEventSystemSelectingObject (uiSlots[slotIndex].uiButton.gameObject);
			}
			return false;
		}


		/**
		 * <summary>Gets the InvInstance displayed in a specific slot.</summary>
		 * <param name = "i">The index number of the slot</param>
		 * <returns>The InvInstance displayed in the slot</returns>
		 */
		public InvInstance GetInstance (int i)
		{
			if (craftingType == CraftingElementType.Ingredients && !Application.isPlaying)
			{
				i = 0;
			}

			if (i >= 0 && i < invInstances.Count)
			{
				return invInstances[i];
			}
			return null;
		}


		public InvItem GetItem (int i)
		{
			if (craftingType == CraftingElementType.Ingredients && !Application.isPlaying)
			{
				i = 0;
			}

			if (i >= 0 && i < invInstances.Count)
			{
				if (InvInstance.IsValid (invInstances[i]))
				{
					return invInstances[i].InvItem;
				}
			}
			return null;
		}


		private string GetCount (int i)
		{
			if (inventoryItemCountDisplay == InventoryItemCountDisplay.Never) return string.Empty;

			InvInstance invInstance = GetInstance (i);
			if (InvInstance.IsValid (invInstance))
			{
				if (invInstance.Count < 2 && inventoryItemCountDisplay == InventoryItemCountDisplay.OnlyIfMultiple)
				{
					return string.Empty;
				}

				if (ItemIsSelected (i))
				{
					return invInstance.GetInventoryDisplayCount ().ToString ();
				}
				return invInstance.Count.ToString ();
			}
			return string.Empty;
		}


		private bool ItemIsSelected (int index)
		{
			if (!InvInstance.IsValid (KickStarter.runtimeInventory.SelectedInstance)) return false;

			if (index > 0 && index < invInstances.Count && (!KickStarter.settingsManager.InventoryDragDrop || KickStarter.playerInput.GetDragState () == DragState.Inventory))
			{
				return (invInstances[index] == KickStarter.runtimeInventory.SelectedInstance);
			}
			return false;
		}


		public override bool ProcessClick (AC.Menu _menu, int _slot, MouseState _mouseState)
		{
			if (KickStarter.stateHandler.gameState == GameState.Cutscene)
			{
				return false;
			}

			bool clickConsumed = false;

			switch (craftingType)
			{
				case CraftingElementType.Ingredients:
					clickConsumed = HandleDefaultClick (_mouseState, _slot);
					break;

				case CraftingElementType.Output:
					clickConsumed = ClickOutput (_menu, _mouseState);
					break;

				default:
					break;
			}

			PlayerMenus.ResetInventoryBoxes ();
			_menu.Recalculate ();

			if (clickConsumed)
			{
				base.ProcessClick (_menu, _slot, _mouseState);
				return true;
			}

			return false;
		}

		
		protected override void AutoSize ()
		{
			if (invInstances.Count > 0)
			{
				foreach (InvInstance invInstance in invInstances)
				{
					if (InvInstance.IsValid (invInstance))
					{
						switch (displayType)
						{
							case ConversationDisplayType.IconOnly:
								AutoSize (new GUIContent (invInstance.InvItem.tex));
								break;

							case ConversationDisplayType.TextOnly:
								AutoSize (new GUIContent (invInstance.InvItem.label));
								break;

							default:
								break;
						}
						return;
					}
				}
			}
			else
			{
				AutoSize (GUIContent.none);
			}
		}


		/**
		 * <summary>Gets the slot index number that a given InvItem (inventory item) appears in.</summary>
		 * <param name = "itemID">The ID number of the InvItem to search for</param>
		 * <returns>The slot index number that the inventory item appears in</returns>
		 */
		public int GetItemSlot (int itemID)
		{
			for (int i=0; i<invInstances.Count; i++)
			{
				if (InvInstance.IsValid (invInstances[i]) && invInstances[i].ItemID == itemID)
				{
					if (craftingType == CraftingElementType.Ingredients)
					{
						return i;
					}
					return i - offset;
				}
			}
			return 0;
		}


		/**
		 * <summary>Gets the slot index number that a given InvItem (inventory item) appears in.</summary>
		 * <param name = "invInstance">The instance of the InvItem to search for</param>
		 * <returns>The slot index number that the inventory item appears in</returns>
		 */
		public int GetItemSlot (InvInstance invInstance)
		{
			for (int i = 0; i < invInstances.Count; i++)
			{
				if (InvInstance.IsValid (invInstances[i]) && invInstances[i] == invInstance)
				{
					return i - offset;
				}
			}
			return 0;
		}


		/** The List of inventory item instances that are currently on display */
		public List<InvInstance> InvInstances
		{
			get
			{
				return invInstances;
			}
		}
		
	}
	
}