using Esri.ArcGISMapsSDK.Components;
using UnityEngine;

#if UNITY_EDITOR
public class DbDataReadPolygon : MonoBehaviour
{
    public string TableName;
    public Material Material;
    public string Extrusion;
    private ArcGISMapComponent arcGISMapComponent;

    public void LoadDataFromDb()
    {
        arcGISMapComponent = FindObjectOfType<ArcGISMapComponent>();
        var connection = DbCommonFunctions.GetNpgsqlConnection();
        DBquery.LoadTriangleData(connection, TableName, this, Extrusion, Material, arcGISMapComponent);
    }
}
#endif
