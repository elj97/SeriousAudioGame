using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using AC;

public class AutoWalkTo : MonoBehaviour
{

	public int walkToIconID = 9;


	private void OnEnable ()
	{
		EventManager.OnHotspotSelect += OnHotspotSelect;
		EventManager.OnHotspotDeselect += OnHotspotDeselect;
		EventManager.OnHotspotInteract += OnHotspotInteract;
	}


	private void OnDisable ()
	{
		EventManager.OnHotspotSelect -= OnHotspotSelect;
		EventManager.OnHotspotDeselect -= OnHotspotDeselect;
		EventManager.OnHotspotInteract -= OnHotspotInteract;
	}


	private void OnHotspotSelect (Hotspot hotspot)
	{
		if (HotspotHasWalkToInteraction (hotspot))
		{
			if (KickStarter.playerCursor.GetSelectedCursorID () == -1 && KickStarter.runtimeInventory.SelectedItem == null)
			{
				KickStarter.playerCursor.SetCursorFromID (walkToIconID);
			}
		}
	}


	private void OnHotspotDeselect (Hotspot hotspot)
	{
		if (HotspotHasWalkToInteraction (hotspot))
		{
			if (KickStarter.playerCursor.GetSelectedCursorID () == walkToIconID && KickStarter.runtimeInventory.SelectedItem == null)
			{
				KickStarter.playerCursor.ResetSelectedCursor ();
			}
		}
	}


	private void OnHotspotInteract (Hotspot hotspot, AC.Button button)
	{
		if (button != null && button.iconID == walkToIconID)
		{
			OnHotspotSelect (hotspot);
		}
	}


	private bool HotspotHasWalkToInteraction (Hotspot hotspot)
	{
		return (hotspot.GetUseButton (walkToIconID) != null);
	}
	
}
