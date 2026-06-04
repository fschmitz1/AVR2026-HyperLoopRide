using System;
using TMPro;
using UnityEngine;

public class Clock : MonoBehaviour
{
    public TextMeshPro textMesh;
 
    void Update()
    {
        textMesh.text = DateTime.Now.ToString("HH:mm:ss");
    }
}
