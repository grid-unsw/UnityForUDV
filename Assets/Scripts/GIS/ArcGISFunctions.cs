using Esri.ArcGISMapsSDK.Components;
using Esri.ArcGISMapsSDK.Utils.GeoCoord;
using Esri.GameEngine.Geometry;
using Esri.HPFramework;
using Unity.Mathematics;
using UnityEngine;

public static class ArcGISFunctions
{
    public static void SetElevation(GameObject gameObject, ArcGISMapComponent arcGISMapComponent, float elevationOffset = 0)
    {
        // start the raycast in the air at an arbitrary to ensure it is above the ground
        var raycastHeight = 5000;
        var position = gameObject.transform.position;
        var raycastStart = new Vector3(position.x, position.y + raycastHeight, position.z);
        var layerMask = 1 << LayerMask.NameToLayer("gis");
        if (Physics.Raycast(raycastStart, Vector3.down, out RaycastHit hitInfo, raycastHeight*2,~layerMask))
        {
            var location = gameObject.GetComponent<ArcGISLocationComponent>();
            location.Position = HitToGeoPosition(hitInfo, arcGISMapComponent, elevationOffset);
        }
    }

    public static void SnapObjectToTerrain(GameObject gameObject)
    {
        // start the raycast in the air at an arbitrary to ensure it is above the ground
        var raycastHeight = 5000;
        var layerMask = 1 << LayerMask.NameToLayer("gis");
        var mesh = gameObject.GetComponent<MeshFilter>().sharedMesh;
        var vertices = mesh.vertices;
        var objectTransform = gameObject.transform;
        var extrusion = 0f;
        for (var i = 0; i < vertices.Length; i++)
        {
            var vertex = vertices[i];
            if (vertex.z != 0)
                extrusion = vertex.z;

            var vertexWorldPos = objectTransform.TransformPoint(vertex);
            var raycastStart = new Vector3(vertexWorldPos.x, vertexWorldPos.y + raycastHeight, vertexWorldPos.z);

            if (Physics.Raycast(raycastStart, Vector3.down, out RaycastHit hitInfo, raycastHeight * 2, ~layerMask))
            {
                var newVertexPosition = objectTransform.InverseTransformPoint(hitInfo.point);
                vertices[i] = new Vector3(newVertexPosition.x, newVertexPosition.y, newVertexPosition.z + vertex.z);
            }
        }

        mesh.vertices = vertices;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        var objectPosition = gameObject.transform.position;
        gameObject.transform.position = new Vector3(objectPosition.x, objectPosition.y + extrusion, objectPosition.z);
    }

    /// <summary>
    /// Return GeoPosition Based on RaycastHit; I.E. Where the user clicked in the Scene.
    /// </summary>
    /// <param name="hit"></param>
    /// <returns></returns>
    private static ArcGISPoint HitToGeoPosition(RaycastHit hit, ArcGISMapComponent arcGISMapComponent, float yOffset = 0)
    {
        var worldPosition = math.inverse(arcGISMapComponent.WorldMatrix)
            .HomogeneousTransformPoint(hit.point.ToDouble3());

        var geoPosition = arcGISMapComponent.View.WorldToGeographic(worldPosition);
        var offsetPosition = new ArcGISPoint(geoPosition.X, geoPosition.Y, geoPosition.Z + yOffset,
            geoPosition.SpatialReference);

        return GeoUtils.ProjectToSpatialReference(offsetPosition, new ArcGISSpatialReference(4326));
    }
}
