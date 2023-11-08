using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(InteriorMapGenerator_Cubemap))]
public class IMPCube_Editor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        InteriorMapGenerator_Cubemap myScript = (InteriorMapGenerator_Cubemap) target;
        if (GUILayout.Button("Preproject"))
        {
            myScript.CreatBox();
            Debug.Log("Please adjust each depth");
        }
        if (GUILayout.Button("Merge"))
        {
           myScript.Mergeall();
        }
       
    }
}
