using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Fade : MonoBehaviour
{
    [SerializeField] SpriteRenderer image;
    // Start is called before the first frame update
    void Start()
    {
        image = this.GetComponent<SpriteRenderer>();
    }

    public void BeginFade()
    {
        image.color = new Vector4(1, 1, 1, 1);
    }
    public void EndFade()
    {
        image.color = new Vector4(1, 1, 1, 0);
    }
}
