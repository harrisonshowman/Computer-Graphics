/****************************************************************************
 * Copyright ©2021 Khoa Nguyen and Quan Dang. Adapted from CSE 457 Modeler by
 * Brian Curless. All rights reserved. Permission is hereby granted to
 * students registered for University of Washington CSE 457.
 * No other use, copying, distribution, or modification is permitted without
 * prior written consent. Copyrights for third-party components of this work
 * must be honored.  Instructors interested in reusing these course materials
 * should contact the authors below.
 * Khoa Nguyen: https://github.com/akkaneror
 * Quan Dang: https://github.com/QuanGary
 ****************************************************************************/
using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Mathf;

/// <summary>
/// SurfaceOfRevolution is responsible for generating a mesh given curve points.
/// </summary>

#if (UNITY_EDITOR)
public class SurfaceOfRevolution : MonoBehaviour
{
    private Mesh mesh;

    private List<Vector2> curvePoints;
    private int _mode;
    private int _numCtrlPts;
    private readonly string _curvePointsFile = "curvePoints.txt";
    private Vector3[] normals;
    private int[] triangles;
    private Vector2[] UVs;
    private Vector3[] vertices;

    private int subdivisions;
    public TextMeshProUGUI subdivisionText;

    private void Start()
    {
        subdivisions = 16;
        subdivisionText.text = "Subdivision: " + subdivisions.ToString();
    }

    private void Update()
    {
    }

    public void Initialize()
    {
        // Create an empty mesh
        mesh = new Mesh();
        mesh.indexFormat =
            UnityEngine.Rendering.IndexFormat.UInt32; // Set Unity's max number of vertices for a mesh to be ~4 billion
        GetComponent<MeshFilter>().mesh = mesh;

        // Load curve points
        ReadCurveFile(_curvePointsFile);

        // Invalid number of control points
        if (_mode == 0 && _numCtrlPts < 4 || _mode == 1 && _numCtrlPts < 2) return;
        
        // Calculate and draw mesh
        ComputeMeshData();
        UpdateMeshData();
    }

    
    /// <summary>
    /// Computes the surface revolution mesh given the curve points and the number of radial subdivisions.
    /// 
    /// Inputs:
    /// curvePoints : the list of sampled points on the curve.
    /// subdivisions: the number of radial subdivisions
    /// 
    /// Outputs:
    /// vertices : a list of `Vector3` containing the vertex positions
    /// normals  : a list of `Vector3` containing the vertex normals. The normal should be pointing out of
    ///            the mesh.
    /// UVs      : a list of `Vector2` containing the texture coordinates of each vertex
    /// triangles: an integer array containing vertex indices (of the `vertices` list). The first three
    ///            elements describe the first triangle, the fourth to sixth elements describe the second
    ///            triangle, and so on. The vertex must be oriented counterclockwise when viewed from the 
    ///            outside.
    /// </summary>
    private void ComputeMeshData()
    {
        // TODO: Compute and set vertex positions, normals, UVs, and triangle faces
        // You will want to use curvePoints and subdivisions variables, and you will
        // want to change the size of these arrays
        int numVertices = subdivisions * curvePoints.Count;

        vertices = new Vector3[numVertices + curvePoints.Count];
        normals = new Vector3[numVertices + curvePoints.Count];
        UVs = new Vector2[numVertices + curvePoints.Count];
        triangles = new int[(numVertices+curvePoints.Count)*6];

        double[,] rotationMatrix = new double[3, 3];
        int count = 0;
        for(int i = 0; i < subdivisions; i++){

            // build rotation matrix for each subdivision
            double theta = (2*Math.PI / subdivisions)*(i);
            rotationMatrix[0,0] = Math.Cos(theta);
            rotationMatrix[1,0] = 0;
            rotationMatrix[2,0] = Math.Sin(theta);
            rotationMatrix[0,1] = 0;
            rotationMatrix[1,1] = 1;
            rotationMatrix[2,1] = 0;
            rotationMatrix[0,2] = -1*Math.Sin(theta);
            rotationMatrix[1,2] = 0;
            rotationMatrix[2,2] = Math.Cos(theta);

            double vCount = 0.0;
            foreach(Vector2 point in curvePoints) {

                // multiply each point by the current rotation and add it to the array of vertices
                double xVertex = rotationMatrix[0,0]*point[0] + rotationMatrix[1,0]*point[1];
                double yVertex = rotationMatrix[0,1]*point[0] + rotationMatrix[1,1]*point[1];
                double zVertex = rotationMatrix[0,2]*point[0] + rotationMatrix[1,2]*point[1];
                vertices[count] = new Vector3((float)xVertex, (float)yVertex, (float)zVertex);
                if(count < curvePoints.Count) {
                    vertices[numVertices + count] = new Vector3((float)xVertex, (float)yVertex, (float)zVertex);
                }

                // calculate the UVs from the vertices
                double u = (theta / (2*Math.PI));

                double topDistance = 0, bottomDistance = 0;
                for(int j = 0; j < curvePoints.Count - 1; j++){
                    bottomDistance += Vector2.Distance(curvePoints[j], curvePoints[j+1]);
                    if(j < vCount){
                        topDistance += Vector2.Distance(curvePoints[j], curvePoints[j+1]);
                    }
                }
                double v = topDistance / bottomDistance;
                if(count < (numVertices)){
                    UVs[count] = new Vector2((float)(u), (float)(v));
                }
                
                if(count < curvePoints.Count) {
                    UVs[numVertices + count] = new Vector2(1, (float)(v));
                }
                count++;
                vCount++;
            }
        }

        

        // build the triangles from the vertices
        for(int i = 0; i < numVertices; i++) {
            triangles[6*i] = i;
            if(i + 1 >= numVertices) {
                triangles[6*i + 1] = (i + 1) % (numVertices + curvePoints.Count);
            } else {
                triangles[6*i + 1] = i + 1;
            }
            if(i >= numVertices) {
                triangles[6*i + 2] = (i + curvePoints.Count) % (numVertices + curvePoints.Count);
            } else {
                triangles[6*i + 2] = i + curvePoints.Count;
            }

            triangles[6*i + 3] = i;
            if(i >= numVertices) {
                triangles[6*i + 4] = (i + curvePoints.Count) % (numVertices + curvePoints.Count);
            } else {
                triangles[6*i + 4] = i + curvePoints.Count;
            }
            if(i - 1 >= numVertices) {
                triangles[6*i + 5] = (i + curvePoints.Count - 1) % (numVertices + curvePoints.Count);
            } else {
                triangles[6*i + 5] = i + curvePoints.Count - 1;
            }


            // create the normal vectors using the edges of the triangles just created
            Vector3 edgeOne = vertices[triangles[6*i + 1]] - vertices[triangles[6*i]];
            Vector3 edgeTwo = vertices[triangles[6*i + 2]] - vertices[triangles[6*i]];
            normals[i] = Vector3.Cross(edgeOne, edgeTwo).normalized;
            if(i < curvePoints.Count) {
                normals[numVertices + i] = Vector3.Cross(edgeOne, edgeTwo).normalized;
            }
        }
    }

    private void UpdateMeshData()
    {
        // Assign data to mesh
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.triangles = triangles;
        mesh.uv = UVs;
    }

    // Export mesh as an asset
    public void ExportMesh()
    {
        string path = EditorUtility.SaveFilePanel("Save Mesh Asset", "Assets/ExportedMesh/", mesh.name, "asset");
        if (string.IsNullOrEmpty(path)) return;
        path = FileUtil.GetProjectRelativePath(path);
        AssetDatabase.CreateAsset(mesh, path);
        AssetDatabase.SaveAssets();
    }

    public void SubdivisionValueChanged(Slider slider)
    {
        subdivisions = (int)slider.value;
        subdivisionText.text = "Subdivision: " + subdivisions.ToString();
    }
    
    private void ReadCurveFile(string file)
    {
        curvePoints = new List<Vector2>();
        string line;

        var f =
            new StreamReader(file);
        if ((line = f.ReadLine()) != null)
        {
            var curveData = line.Split(' ');
            _mode = Convert.ToInt32(curveData[0]);
            _numCtrlPts = Convert.ToInt32(curveData[1]);
        }

        while ((line = f.ReadLine()) != null)
        {
            var curvePoint = line.Split(' ');
            var x = float.Parse(curvePoint[0]);
            var y = float.Parse(curvePoint[1]);
            curvePoints.Add(new Vector2(x, y));
        }

        f.Close();
    }
}
#endif