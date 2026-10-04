using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;

namespace CanoneerRestoration;

public static class ObjParser
{
    public static Mesh LoadObjFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError("OBJ file missing at: " + filePath);
            return null;
        }

        // Global data pools extracted from file
        List<Vector3> textVertices = new List<Vector3>();
        List<Vector2> textUVs = new List<Vector2>();
        List<Vector3> textNormals = new List<Vector3>();

        // Target parallel collections for Unity binding
        List<Vector3> meshVertices = new List<Vector3>();
        List<Vector2> meshUVs = new List<Vector2>();
        List<Vector3> meshNormals = new List<Vector3>();
        List<int> meshTriangles = new List<int>();

        // Fast configuration indexing map
        Dictionary<string, int> vertexCache = new Dictionary<string, int>();

        string[] lines = File.ReadAllLines(filePath);

        foreach (string line in lines)
        {
            string trimmed = line.Trim();
            if (trimmed.StartsWith("#") || string.IsNullOrEmpty(trimmed)) continue;

            // Isolate layout elements by filtering tabs and trailing double spaces
            string[] parts = trimmed.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) continue;

            string type = parts[0];

            if (type == "v")
            {
                textVertices.Add(new Vector3(
                    float.Parse(parts[1], CultureInfo.InvariantCulture),
                    float.Parse(parts[2], CultureInfo.InvariantCulture),
                    float.Parse(parts[3], CultureInfo.InvariantCulture)
                ));
            }
            else if (type == "vt")
            {
                textUVs.Add(new Vector2(
                    float.Parse(parts[1], CultureInfo.InvariantCulture),
                    float.Parse(parts[2], CultureInfo.InvariantCulture)
                ));
            }
            else if (type == "vn")
            {
                textNormals.Add(new Vector3(
                    float.Parse(parts[1], CultureInfo.InvariantCulture),
                    float.Parse(parts[2], CultureInfo.InvariantCulture),
                    float.Parse(parts[3], CultureInfo.InvariantCulture)
                ));
            }
            else if (type == "f")
            {
                // Triangle fan generation with absolute array-bound bounds enforcement
                for (int i = 1; i < parts.Length - 1; i++)
                {
                    // Strict safety double-check: ensure the calculated indices are well within the elements array
                    if (1 >= parts.Length || (i + 1) >= parts.Length || (i + 2) >= parts.Length)
                        continue;

                    ParseFaceVertex(parts[1], textVertices, textUVs, textNormals, vertexCache, meshVertices, meshUVs, meshNormals, meshTriangles);
                    ParseFaceVertex(parts[i + 1], textVertices, textUVs, textNormals, vertexCache, meshVertices, meshUVs, meshNormals, meshTriangles);
                    ParseFaceVertex(parts[i + 2], textVertices, textUVs, textNormals, vertexCache, meshVertices, meshUVs, meshNormals, meshTriangles);
                }
            }
        }

        if (meshVertices.Count == 0)
        {
            Debug.LogError("OBJ parser finished but extracted 0 valid vertices. Check file structure formatting.");
            return null;
        }

        // Assemble native container assets
        Mesh mesh = new Mesh(); //
        mesh.vertices = meshVertices.ToArray(); //
        
        // 1. Convert UVs to array
        Vector2[] uvArray = meshUVs.ToArray();
        mesh.uv = uvArray; // Primary texture channel
        mesh.uv2 = uvArray; // Secondary channel (Fixes many legacy Unity shader fallbacks)
        
        mesh.normals = meshNormals.ToArray(); //
        mesh.triangles = meshTriangles.ToArray(); //

        mesh.RecalculateBounds(); //
        
        // 2. Ensure Normals and Tangents are clean for lighting/textures
        mesh.RecalculateNormals(); 
        
        // Optimize the vertex buffer layout for Unity 4.6
        mesh.Optimize();

        return mesh; //
    }

    private static void ParseFaceVertex(string token, List<Vector3> textVerts, List<Vector2> textUVs, List<Vector3> textNormals,
        Dictionary<string, int> cache, List<Vector3> meshVerts, List<Vector2> meshUV, List<Vector3> meshNorm, List<int> meshTris)
    {
        if (cache.TryGetValue(token, out int existingIndex))
        {
            meshTris.Add(existingIndex);
            return;
        }

        string[] indices = token.Split('/');
        
        Vector3 pos = Vector3.zero;
        if (indices.Length > 0 && !string.IsNullOrEmpty(indices[0]))
        {
            int vIdx = int.Parse(indices[0]) - 1;
            if (vIdx >= 0 && vIdx < textVerts.Count)
            {
                pos = textVerts[vIdx];
            }
        }

        Vector2 uv = Vector2.zero;
        if (indices.Length > 1 && !string.IsNullOrEmpty(indices[1]))
        {
            int uvIdx = int.Parse(indices[1]) - 1;
            if (uvIdx >= 0 && uvIdx < textUVs.Count)
            {
                uv = textUVs[uvIdx];
            }
        }

        Vector3 norm = Vector3.up; 
        if (indices.Length > 2 && !string.IsNullOrEmpty(indices[2]))
        {
            int nIdx = int.Parse(indices[2]) - 1;
            if (nIdx >= 0 && nIdx < textNormals.Count)
            {
                norm = textNormals[nIdx];
            }
        }

        // Lock step tracking assignment guarantees array validation checks inside Unity's core layout engine pass
        int newIndex = meshVerts.Count;
        meshVerts.Add(pos);
        meshUV.Add(uv);
        meshNorm.Add(norm);
        
        meshTris.Add(newIndex);
        cache.Add(token, newIndex);
    }

    public static GameObject LoadCustomProp(string modelFileName)
    {
        string fullPath = Path.Combine(CanoneerRestoration.ModelsPath, modelFileName);
        
        // 1. Use our parser script to build a native Mesh structure from disk
        Mesh parsedMesh = LoadObjFromFile(fullPath);
        if (parsedMesh == null) return null;

        // 2. Create an empty container GameObject
        GameObject propObject = new GameObject("CustomProp_" + Path.GetFileNameWithoutExtension(modelFileName));

        // 3. Add components required to hold and view geometry
        MeshFilter filter = propObject.AddComponent<MeshFilter>();
        MeshRenderer renderer = propObject.AddComponent<MeshRenderer>();

        // 4. Assign the runtime mesh to the filter
        filter.mesh = parsedMesh;

        // 5. Give it a basic built-in diffuse shader material for now
        renderer.material = new Material(Shader.Find("Diffuse"));

        return propObject;
    }
}