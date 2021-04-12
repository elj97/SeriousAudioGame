using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class changeButton : MonoBehaviour
{
    private SpriteRenderer spriteRender;
    public Sprite onSprite;
    public Sprite offSprite;

    private void Start()
    {
        spriteRender = this.GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            spriteRender.sprite = onSprite;
        }
        if ( Input.GetKeyDown(KeyCode.S) )
        {
            spriteRender.sprite = offSprite;
        }
    }

}
