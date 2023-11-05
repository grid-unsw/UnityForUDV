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
