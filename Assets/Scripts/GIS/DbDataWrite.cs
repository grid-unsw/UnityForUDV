using System.Collections.Generic;
using System.Linq;
using Esri.ArcGISMapsSDK.Components;
using Esri.ArcGISMapsSDK.Utils.GeoCoord;
using Esri.GameEngine.Geometry;
using UnityEngine;

#if UNITY_EDITOR
public class DbDataWrite : MonoBehaviour
{
    public string TableName;
    public bool Truncate;
    public void WritePolyhedronData()
    {
        var arcGisLocationComponents = new ArcGISLocationComponent[]{GetComponent<ArcGISLocationComponent>()};
        if (arcGisLocationComponents[0] == null)
        {
            arcGisLocationComponents =  GetComponentsInChildren<ArcGISLocationComponent>();
            if (arcGisLocationComponents.Length == 0)
            {
                Debug.Log($"{gameObject.name} does not have ArcGISLocation component attached.");
                return;
            }
        }

        var centroids = new List<ArcGISPoint>();
        var meshFilters = new List<MeshFilter>();
        foreach (var arcGisLocationComponent in arcGisLocationComponents)
        {
            var reprojectedLocation =
                GeoUtils.ProjectToSpatialReference(arcGisLocationComponent.Position, new ArcGISSpatialReference(28356));
            foreach (var meshFilter in arcGisLocationComponent.gameObject.GetComponents<MeshFilter>())
            {
                meshFilters.Add(meshFilter);
                var meshFilterTranslation = meshFilter.gameObject.transform.position;
                var centroid = new ArcGISPoint(reprojectedLocation.X - meshFilterTranslation.x,
                    reprojectedLocation.Y - meshFilterTranslation.z,
                    reprojectedLocation.Z - meshFilterTranslation.y);
                centroids.Add(centroid);
            }
            
            if (!meshFilters.Any())
            {
                foreach (var meshFilter in arcGisLocationComponent.gameObject.GetComponentsInChildren<MeshFilter>())
                {
                    meshFilters.Add(meshFilter);
                    var meshFilterTranslation = meshFilter.gameObject.transform.position;
                    var mainParentTranslation = arcGisLocationComponent.gameObject.transform.position;
                    var centroid = new ArcGISPoint(reprojectedLocation.X - mainParentTranslation.x - meshFilterTranslation.x,
                        reprojectedLocation.Y - mainParentTranslation.z - meshFilterTranslation.z,
                        reprojectedLocation.Z - mainParentTranslation.y - meshFilterTranslation.y);
                    centroids.Add(centroid);
                }
            }
        }

        if (!meshFilters.Any())
        {
            Debug.Log("No meshes detected.");
            return;
        }

        var connection = DbCommonFunctions.GetNpgsqlConnection();
        DBexport.ExportMeshesAsPolyhedrons(meshFilters.ToArray(), connection, centroids.ToArray(), TableName, Truncate);
    }
}
#endif
