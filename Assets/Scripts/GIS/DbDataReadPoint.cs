using Esri.ArcGISMapsSDK.Components;
using UnityEngine;

#if UNITY_EDITOR
public class DbDataReadPoint : MonoBehaviour
{
    public string TableName;
    private ArcGISMapComponent arcGISMapComponent;
    public GameObject Prefab;
    [Range(0.1f, 5f)]
    public float ScaleSize = 1f;
    public Material Material;

    public void LoadDataFromDb()
    {
        arcGISMapComponent = FindObjectOfType<ArcGISMapComponent>();
        var connection = DbCommonFunctions.GetNpgsqlConnection();
        DBquery.LoadPointData(connection, TableName, this, Material, arcGISMapComponent, Prefab, ScaleSize);
    }
}
#endif
