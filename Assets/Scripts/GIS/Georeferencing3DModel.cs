using UnityEngine;

[ExecuteInEditMode]
public class Georeferencing3DModel : MonoBehaviour
{
    public GameObject Model3D;
    public bool ScaleModel;
    public void GeoreferenceModel()
    {
        if (transform.childCount != 4)
        {
            Debug.Log("There should be 4 points in the Georeferencing prefab.");
        }

        var sourceObject1 = transform.GetChild(2);
        var sourceObject2 = transform.GetChild(3);

        var targetPoint1 = transform.GetChild(0).transform.position;
        var targetPoint2 = transform.GetChild(1).transform.position;
        var sourcePoint1 = sourceObject1.transform.position;
        var sourcePoint2 = sourceObject2.transform.position;

        if (CheckIfVector3IsZeroVector(targetPoint1) || CheckIfVector3IsZeroVector(targetPoint2) 
                                                     || CheckIfVector3IsZeroVector(sourcePoint1) ||
                                                     CheckIfVector3IsZeroVector(sourcePoint2))
        {
            Debug.Log("At least one Target or Source point is not assigned.");
        }

        var targetVector = targetPoint2 - targetPoint1;
        var targetCentroid = (targetPoint2 + targetPoint1) / 2;
        var sourceVector = sourcePoint2 - sourcePoint1;

        var rotationAngle = Vector2.SignedAngle(GetVector2FromVector3XZ(targetVector), GetVector2FromVector3XZ(sourceVector));

        if (ScaleModel)
        {
            var scaleRatio = targetVector.magnitude / sourceVector.magnitude;

            var scaledSourcePoint1 = (sourcePoint1 - Model3D.transform.position) * scaleRatio + Model3D.transform.position;
            var scaledSourcePoint2 = (sourcePoint2 - Model3D.transform.position) * scaleRatio + Model3D.transform.position;

            var sourceObject1NewPosition = RotatePointAroundPivot(scaledSourcePoint1, Model3D.transform.position, rotationAngle);
            var sourceObject2NewPosition = RotatePointAroundPivot(scaledSourcePoint2, Model3D.transform.position, rotationAngle);
            var sourceNewCentroid = (sourceObject1NewPosition + sourceObject2NewPosition) / 2;
            var translationVector = targetCentroid - sourceNewCentroid;

            //update
            Model3D.transform.Rotate(Vector3.up, rotationAngle);
            Model3D.transform.localScale *= scaleRatio;
            Model3D.transform.position += translationVector;
            sourceObject1.transform.Translate(targetPoint1 - sourcePoint1);
            sourceObject2.transform.Translate(targetPoint2 - sourcePoint2);
        }
        else
        {
            var sourceObject1NewPosition = RotatePointAroundPivot(sourcePoint1, Model3D.transform.position, rotationAngle);
            var sourceObject2NewPosition = RotatePointAroundPivot(sourcePoint2, Model3D.transform.position, rotationAngle);
            var sourceNewCentroid = (sourceObject1NewPosition + sourceObject2NewPosition) / 2;
            var translationVector = targetCentroid - sourceNewCentroid;

            //update
            Model3D.transform.Rotate(Vector3.up, rotationAngle);
            Model3D.transform.position += translationVector;
            sourceObject1.transform.Translate(sourceObject1NewPosition - sourcePoint1 + translationVector);
            sourceObject2.transform.Translate(sourceObject2NewPosition - sourcePoint2 + translationVector);
        }
        
        Debug.DrawLine(targetPoint1, targetPoint2, Color.cyan, 30);
    }


    private static bool CheckIfVector3IsZeroVector(Vector3 vector)
    {
        return vector == Vector3.zero;
    }

    private static Vector2 GetVector2FromVector3XZ(Vector3 vector)
    {
        return new Vector2(vector.x, vector.z);
    }

    public Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, float angle)
    {
        return Quaternion.Euler(new Vector3(0,angle,0)) * (point - pivot) + pivot;
    }
}
