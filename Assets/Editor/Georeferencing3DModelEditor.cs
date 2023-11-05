using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Georeferencing3DModel))]
public class Georeferencing3DModelEditor : Editor
{
    private bool vKeyIsDown;
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var dbManager = (Georeferencing3DModel)target;

        if (GUILayout.Button("Georeference Model"))
        {
            dbManager.GeoreferenceModel(); ;
        }
    }

    //void OnEnable()
    //{
    //    vKeyIsDown = false;
    //}

    //void OnSceneGUI()
    //{
    //    Event e = Event.current;
    //    switch (e.type)
    //    {
    //        case EventType.MouseDown:
    //        {
    //            if (e.isMouse && e.button == 0 && vKeyIsDown)
    //            {
    //                Debug.Log("do sth");
    //                vKeyIsDown = false;
    //                e.Use();
    //            }
    //            else if (e.isMouse && e.button == 1 && vKeyIsDown)
    //            {
    //                Debug.Log("do sth else");
    //                vKeyIsDown = false;
    //                e.Use();
    //            }

    //            break;
    //        }
    //    }

    //    if (Event.current.keyCode == KeyCode.V)
    //    {
    //        vKeyIsDown = true;
    //    }
    //}
}

