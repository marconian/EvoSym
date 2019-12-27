using Assets.Utilities;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshCollider))]
public class GenerateTerrainMesh : MonoBehaviour
{
    public int TerrainSize = 10;
    public int TileCount = 25;
    public float Strength = 10f;

    private MeshCollider Collider;
    private MeshFilter Filter;

    void Start()
    {
        Filter = transform.GetComponent<MeshFilter>();
        Collider = transform.GetComponent<MeshCollider>();

        Mesh mesh = Filter.sharedMesh;

        Vector3[] vertices = Generate(mesh.vertices);
        mesh.vertices = vertices;

        Filter.sharedMesh = mesh;
        Collider.sharedMesh = null;
        Collider.sharedMesh = mesh;

        Vector3[] submerged = vertices
            .Where(v => (transform.position.y + v.y) < 0f)
            .ToArray();
    }

    private Vector3[] Generate(Vector3[] vectors)
    {
        Vector3[] vertices = new Vector3[vectors.Length];

        //Generate some random terrain
        for (int i = 0; i < vertices.Length; i++)
            vertices[i] = new Vector3(vectors[i].x, Mathf.PerlinNoise(vectors[i].x / 3f, vectors[i].z / 3f) * 3f, vectors[i].z);

        //you could start work with tiles while terrain generation and edit 4 vertices
        //for better results we ceep track of already placed tiles
        List<int> usedVerts = new List<int>();

        for (int i = 0; i < TileCount; i++)
        {
            //get a start point
            int vertIndex = Random.Range(
                min: TileCount + 2, 
                max: vertices.Length);
            //if (vertIndex - (TileCount + 2) < 0)
            //    continue;

            //set new high
            float tileheigt = vertices[vertIndex].y;
            if (!usedVerts.Any(u => u == vertIndex))
                tileheigt = vertices[vertIndex].y * Strength;

            //add used verts to the list and update the mesh
            int[] used = new int[]
            {
                vertIndex, 
                vertIndex - 1,
                vertIndex - TerrainSize - 1,
                vertIndex - TerrainSize - 2
            };
            foreach(int vi in used)
            {
                usedVerts.Add(vi);
                vertices[vi] = new Vector3(vertices[vi].x, tileheigt, vertices[vi].z);
            }
        }

        return vertices;
    }
}