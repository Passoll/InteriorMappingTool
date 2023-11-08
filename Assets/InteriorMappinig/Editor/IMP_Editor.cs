using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(InteriorMapGenerator))]
public class IMP_Editor : Editor
{
    
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        InteriorMapGenerator myScript = (InteriorMapGenerator) target;
        if (GUILayout.Button("Setcam"))
        {
            myScript.CreateCamera();
        }
        if (GUILayout.Button("RenderAtlas"))
        {
            myScript.render1();
        }
        if (GUILayout.Button("Clearcam"))
        {
            myScript.ClearCamera();
        }
    }
}
