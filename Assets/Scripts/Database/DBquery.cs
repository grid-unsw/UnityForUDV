using System.Collections;
using System.Collections.Generic;
using Esri.ArcGISMapsSDK.Components;
using Esri.ArcGISMapsSDK.Utils.GeoCoord;
using Esri.GameEngine.Geometry;
using NetTopologySuite.Algorithm;
using NetTopologySuite.Geometries;
using Npgsql;
using UnityEngine;

#if UNITY_EDITOR
public static class DBquery
{
   public static void LoadTriangleData(NpgsqlConnection connection, string tableName, MonoBehaviour handle, string extrusion, Material material, 
       ArcGISMapComponent arcGISMapComponent)
   {
        connection.Open();
        DbCommonFunctions.CheckIfTableExist(tableName, connection);

        connection.TypeMapper.UseNetTopologySuite();

        var metadata = LoadMetadata(connection, tableName);

        var geomType = metadata.Item2[0];
        if (geomType == PostgisGeometryType.ST_Polygon.ToString() 
            || geomType == PostgisGeometryType.ST_MultiPolygon.ToString()            )
        {
            var centroids = GetCentroids(connection, tableName);
            if (extrusion == "")
            {
                DrawPolygons(connection, tableName, handle, material, arcGISMapComponent, metadata.Item1, centroids);
            }
            else
            {
                DrawExtrudedPolygon(connection, tableName, handle, material, arcGISMapComponent, metadata.Item1, centroids, extrusion);
            }
        }
        else if(geomType == PostgisGeometryType.ST_PolyhedralSurface.ToString())
        {
            var centroids = GetCentroids(connection, tableName);
            DrawPolyhedron(connection, tableName, handle, material, arcGISMapComponent, metadata.Item1, centroids);
        }
        else
        {
            Debug.Log("Not supported geometry.");
        }

        connection.Close();
    }

   public static void LoadPointData(NpgsqlConnection connection, string tableName, MonoBehaviour handle, Material material, ArcGISMapComponent arcGISMapComponent, GameObject prefab, float scaleSize)
   {
       connection.Open();
       DbCommonFunctions.CheckIfTableExist(tableName, connection);

       connection.TypeMapper.UseNetTopologySuite();

       var metadata = LoadMetadata(connection, tableName);

       var geomType = metadata.Item2[0];
       if (geomType == PostgisGeometryType.ST_Point.ToString())
       {
           DrawPoints(connection, tableName, handle, prefab, metadata.Item1, material, arcGISMapComponent, scaleSize);
       }
       else
       {
           Debug.Log("Not supported geometry.");
       }

       connection.Close();
   }

    private static (List<string[]>, string[]) LoadMetadata(NpgsqlConnection connection, string tableName)
   {
       var sqlTest = $"SELECT column_name, data_type FROM information_schema.columns WHERE table_name = '{tableName}'";
       var cmd = new NpgsqlCommand(sqlTest, connection);
       var columnList = new List<string>();
       using (var reader = cmd.ExecuteReader())
       {
           while (reader.Read())
           {
               columnList.Add(reader[0].ToString());
           }
       }

       var fieldCount = columnList.Count;
       var sqlTest1 = $"SELECT * FROM \"{tableName}\"";
       cmd = new NpgsqlCommand(sqlTest1, connection);
       cmd.AllResultTypesAreUnknown = true;
       var metadata = new List<string[]>();
       using (var reader = cmd.ExecuteReader())
       {
           while (reader.Read())
           {
               var data = new string[fieldCount];
               for (var j = 0; j < reader.FieldCount; j++)
               {
                   data[j] = $"{columnList[j]}: {reader[j]}";
               }

               metadata.Add(data);
           }
       }

       //update geom field
       var geometriesType = new string[metadata.Count];
       for (var j = 0; j < columnList.Count; j++)
       {
           var value = columnList[j];
           var valueLowerCase = value.ToLower();
           if (valueLowerCase == "geom" || valueLowerCase == "geometry")
           {
               var sqlGeometryType = $"SELECT ST_GeometryType(st_geometryN({value},1)) from \"{tableName}\"";
               var cmd1 = new NpgsqlCommand(sqlGeometryType, connection);

               using (var reader = cmd1.ExecuteReader())
               {
                   var k = 0;
                   while (reader.Read())
                   {
                       var geomType = reader[0].ToString();
                       metadata[k][j] = $"{value}: {geomType}";
                       geometriesType[k] = geomType;
                       k++;
                   }
               }

               break;
           }
       }

       return (metadata, geometriesType);
   }

   private static List<(Point, Point)> GetCentroids(NpgsqlConnection connection, string tableName)
   {
       var sqlCentroids = $"select st_centroid(ST_Points(geom)), st_transform(st_setsrid(st_centroid(ST_Points(geom)),28356),4326) from \"{tableName}\"";

       var cmd = new NpgsqlCommand(sqlCentroids, connection);
       var objectsCentroids = new List<(Point, Point)>();

       using (var reader = cmd.ExecuteReader())
       {
           while (reader.Read())
           {
               objectsCentroids.Add(((Point)reader[0], (Point)reader[1]));
           }
       }

       return objectsCentroids;
   }

   private static void DrawPoints(NpgsqlConnection connection, string tableName, MonoBehaviour handle, GameObject prefab, List<string[]> metadata, Material material, ArcGISMapComponent arcGISMapComponent, float pointSize)
   {
       var sqlCentroids = $"select geom from \"{tableName}\"";

       var cmd = new NpgsqlCommand(sqlCentroids, connection);
       var i = 0;
       using (var reader = cmd.ExecuteReader())
       {
           while (reader.Read())
           {
               var point = (Point)reader[0];
               handle.StartCoroutine(InstantiatePoint(prefab, handle.gameObject, point, metadata[i], material, arcGISMapComponent, pointSize));
               i++;
           }
       }
   }

   private static IEnumerator InstantiatePoint(GameObject prefab, GameObject parent, Point point, string[] metadata, Material material, ArcGISMapComponent arcGISMapComponent, float pointSize)
   {
       GameObject pointGO;
       var verticalOffset = 0f;
       if (prefab != null)
       {
           pointGO = Object.Instantiate(prefab);
           verticalOffset = pointGO.GetComponent<Renderer>().bounds.size.y / 2;
       }
       else
       {
           pointGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
           pointGO.GetComponent<Renderer>().material = material;
           Object.DestroyImmediate(pointGO.GetComponent<SphereCollider>());
       }

       pointGO.transform.localScale = new Vector3(pointSize, pointSize, pointSize);

       pointGO.name = metadata[0];
       pointGO.transform.parent = parent.transform;
       pointGO.layer = 6;//gis layer
       var location = pointGO.AddComponent<ArcGISLocationComponent>();
       location.Position = new ArcGISPoint(point.X, point.Y, 100, new ArcGISSpatialReference(4326));
       pointGO.AddComponent<DataPropeties>().Propeties = metadata;
       if (prefab != null)
       {
           location.Rotation = new ArcGISRotation(90, 90, 0);
       }
       yield return null;
       yield return null;
       ArcGISFunctions.SetElevation(pointGO, arcGISMapComponent, verticalOffset);
       pointGO.AddComponent<MeshCollider>();
   }

   private static void DrawPolygons(NpgsqlConnection connection, string tableName, MonoBehaviour handle, Material material, ArcGISMapComponent arcGISMapComponent, List<string[]> metaData, List<(Point, Point)> objectsCentroids)
   {
        var polyhedronsCount = objectsCentroids.Count;
        var sqlPoints = $"select a.id, (a.geom_pnt).geom from(SELECT id, ST_DumpPoints(st_tesselate(ST_ForcePolygonCW(geom))) As geom_pnt FROM \"{tableName}\") as a";

        var cmd = new NpgsqlCommand(sqlPoints, connection);

        var indices = new List<int>();
        var vertices = new List<Vector3>();
        var i = 0;
        var i1 = 0;
        var previousSurfaceId = 1;
        var polyhedronId = 0;
        var polyhedronCentroid = objectsCentroids[polyhedronId];
        using (var reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                switch (i)
                {
                    case 0:
                        var surfaceId = (int)reader[0];
                        if (surfaceId > previousSurfaceId)
                        {
                            handle.StartCoroutine(InstantiateMesh(handle.gameObject, material, arcGISMapComponent, $"Polygon {polyhedronId}", vertices, indices, polyhedronCentroid, metaData[polyhedronId]));
                            indices = new List<int>();
                            vertices = new List<Vector3>();
                            i1 = 0;
                            polyhedronId++;
                            if (polyhedronsCount != polyhedronId + 1)
                            {
                                polyhedronCentroid = objectsCentroids[polyhedronId];
                            }
                        }
                        previousSurfaceId = surfaceId;

                        indices.Add(i1);
                        vertices.Add(GetShiftedVector3((Point)reader[1], polyhedronCentroid.Item1));
                        i++;
                        i1++;
                        break;
                    case 1:
                        indices.Add(i1);
                        vertices.Add(GetShiftedVector3((Point)reader[1], polyhedronCentroid.Item1));
                        i++;
                        i1++;
                        break;
                    case 2:
                        indices.Add(i1);
                        vertices.Add(GetShiftedVector3((Point)reader[1], polyhedronCentroid.Item1));
                        i++;
                        i1++;
                        break;
                    default:
                        i = 0;
                        break;
                }
            }
            handle.StartCoroutine(InstantiateMesh(handle.gameObject, material, arcGISMapComponent, $"Polygon {polyhedronId}", vertices, indices, polyhedronCentroid, metaData[polyhedronId]));
        }
    }

   private static void DrawExtrudedPolygon(NpgsqlConnection connection, string tableName, MonoBehaviour handle, Material material, ArcGISMapComponent arcGISMapComponent, List<string[]> metaData, 
       List<(Point, Point)> objectsCentroids, string extrusion)
    {
        var polyhedronsCount = objectsCentroids.Count;
        var sqlPoints = $"select b.surface[2], (b.geom_pnt).geom from(SELECT(a.p_geom).path as surface, " +
                            $"ST_DumpPoints(st_tesselate((a.p_geom).geom)) As geom_pnt FROM (select ST_Dump(st_extrude(geom,0,0,{extrusion})) as p_geom from \"{tableName}\") as a) as b";

        ConstractMeshFromPolyhedronPolygons(connection, handle, material, arcGISMapComponent, metaData, objectsCentroids, polyhedronsCount, sqlPoints);
    }

    private static void DrawPolyhedron(NpgsqlConnection connection, string tableName, MonoBehaviour handle, Material material, ArcGISMapComponent arcGISMapComponent, List<string[]> metaData,
    List<(Point, Point)> objectsCentroids)
    {
        var polyhedronsCount = objectsCentroids.Count;
        var sqlPoints = $"select b.surface[2], (b.geom_pnt).geom from(SELECT(a.p_geom).path as surface, " +
                            $"ST_DumpPoints(st_tesselate((a.p_geom).geom)) As geom_pnt FROM (select ST_Dump(geom) as p_geom from \"{tableName}\") as a) as b";

        ConstractMeshFromPolyhedronPolygons(connection, handle, material, arcGISMapComponent, metaData, objectsCentroids, polyhedronsCount, sqlPoints);
    }

    private static void ConstractMeshFromPolyhedronPolygons(NpgsqlConnection connection, MonoBehaviour handle, Material material, ArcGISMapComponent arcGISMapComponent, List<string[]> metaData, List<(Point, Point)> objectsCentroids, int polyhedronsCount, string sqlPoints)
    {
        var cmd = new NpgsqlCommand(sqlPoints, connection);

        var indices = new List<int>();
        var vertices = new List<Vector3>();
        var i = 0;
        var i1 = 0;
        var previousSurfaceId = 1;
        var polyhedronId = 0;
        var polyhedronCentroid = objectsCentroids[polyhedronId];
        using (var reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                switch (i)
                {
                    case 0:
                        var surfaceId = (int)reader[0];
                        if (surfaceId < previousSurfaceId)
                        {
                            handle.StartCoroutine(InstantiateMesh(handle.gameObject, material, arcGISMapComponent, $"Polyhedron {polyhedronId}", vertices, indices, polyhedronCentroid,
                                metaData[polyhedronId]));
                            indices = new List<int>();
                            vertices = new List<Vector3>();
                            i1 = 0;
                            polyhedronId++;
                            if (polyhedronsCount != polyhedronId + 1)
                            {
                                polyhedronCentroid = objectsCentroids[polyhedronId];
                            }
                            previousSurfaceId = 1;
                        }
                        else
                        {
                            previousSurfaceId = surfaceId;
                        }

                        indices.Add(i1);
                        vertices.Add(GetShiftedVector3((Point)reader[1], polyhedronCentroid.Item1));
                        i++;
                        i1++;
                        break;
                    case 1:
                        indices.Add(i1);
                        vertices.Add(GetShiftedVector3((Point)reader[1], polyhedronCentroid.Item1));
                        i++;
                        i1++;
                        break;
                    case 2:
                        indices.Add(i1);
                        vertices.Add(GetShiftedVector3((Point)reader[1], polyhedronCentroid.Item1));
                        i++;
                        i1++;
                        break;
                    default:
                        i = 0;
                        break;
                }
            }

            handle.StartCoroutine(InstantiateMesh(handle.gameObject, material, arcGISMapComponent, $"Polyhedron {polyhedronId}", vertices, indices, polyhedronCentroid, metaData[polyhedronId]));
        }
    }

    private static IEnumerator InstantiateMesh(GameObject parent, Material material, ArcGISMapComponent arcGISMapComponent,
       string objectName, List<Vector3> vertices, List<int> indices, (Point, Point) polyhedronCentroid, string[] properties)
   {
       var gameObject = new GameObject(objectName, typeof(MeshFilter), typeof(MeshRenderer), typeof(DataPropeties));
        var mesh = new Mesh
       {
           vertices = vertices.ToArray(),
           triangles = indices.ToArray()
       };

       mesh.RecalculateNormals();
       gameObject.GetComponent<MeshFilter>().mesh = mesh;
       gameObject.GetComponent<Renderer>().material = material;
       gameObject.GetComponent<DataPropeties>().Propeties = properties;
       gameObject.transform.parent = parent.transform;
       gameObject.layer = 6;//gis layer
       var location = gameObject.AddComponent<ArcGISLocationComponent>();
       location.Position = new ArcGISPoint(polyhedronCentroid.Item2.X, polyhedronCentroid.Item2.Y, 100, new ArcGISSpatialReference(4326));
       // need a frame for location component updates to occur
       yield return null;
       yield return null;
       ArcGISFunctions.SetElevation(gameObject, arcGISMapComponent, 5);
       gameObject.AddComponent<MeshCollider>();
   }

   private static Vector3 GetShiftedVector3(Point point, Point shift)
   {
       if (double.IsNaN(point.Z))
       {
           return new Vector3((float)(point.X - shift.X), (float)(point.Y - shift.Y), 0);
       }

       return new Vector3((float)(point.X - shift.X), (float)(point.Y - shift.Y), (float)point.Z);
   }

   private static Vector3[] GetArrayOfVectorsUp(int vectorsCount)
   {
       var vectorsUp = new Vector3[vectorsCount];
       for (var i = 0; i < vectorsCount; i++)
       {
           vectorsUp[i] = Vector3.up;
       }

       return vectorsUp;
   }
}
#endif