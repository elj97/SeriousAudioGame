using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using AC;

public class ButtonToggle : MonoBehaviour
{
    [SerializeField] SpriteRenderer image;
    [SerializeField] Sprite GoodButton;
    [SerializeField] int ButtonVariable;
    // Start is called before the first frame update
    void Start()
    {
        image = this.GetComponent<SpriteRenderer>();
    }

    public void TurnButtonOn()
    {
        image.sprite = GoodButton;
    }

    private void Update()
    {
        if (GlobalVariables.GetBooleanValue(ButtonVariable))
        {
            TurnButtonOn();
        }
    }
}
