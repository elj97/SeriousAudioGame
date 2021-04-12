using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VariableTracking : MonoBehaviour
{
    [SerializeField] bool engineCorrect = false;

    public void setEngineCorrectTrue()
    {
        engineCorrect = true;
    }
    
}
