using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AC;

public class HotspotToggle : MonoBehaviour
{
    [SerializeField] Hotspot setupHotspot;
    [SerializeField] Hotspot beginWorkHotspot;
    [SerializeField] Hotspot changeAudioDeviceHotspot;
    [SerializeField] Hotspot closeMenuHotspot;
    
    public void ToggleHotspot(int hotspotID)
    {
        switch (hotspotID)
        {
            case 0:
                setupHotspot.gameObject.SetActive(!setupHotspot.gameObject.activeSelf);
                break;

            case 1:
                beginWorkHotspot.gameObject.SetActive(!beginWorkHotspot.gameObject.activeSelf);
                break;

            case 2:
                changeAudioDeviceHotspot.gameObject.SetActive(!changeAudioDeviceHotspot.gameObject.activeSelf);
                break;

            case 3:
                closeMenuHotspot.gameObject.SetActive(!closeMenuHotspot.gameObject.activeSelf);
                break;
        }
    }
}
