using UnityEditor;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;


[ExecuteInEditMode]

public class CustomTerrain : MonoBehaviour
{

    public Vector2 randomHeightRange = new Vector2(0, 0.1f);
    public Texture2D heightMapImage;
    public Vector3 heightMapScale = new Vector3(1,1, 1);
    public Terrain terrain;
    public TerrainData terrainData;


    //Perlin Noise----------------------------
    public float perlinXScale = 0.01f;
    public float perlinYScale = 0.01f;
    public int perlinOffSetX = 0;
    public int perlinOffSetY = 0;


    public void Perlin()
    {
        int resolution = terrainData.heightmapResolution;
        float[,] heightMap = terrainData.GetHeights(0, 0, resolution, resolution);
        for(int y=0; y<resolution; y++)
        {
            for(int x=0; x<resolution; x++)
            {
                heightMap[x,y] = Mathf.PerlinNoise((x + perlinOffSetX) * perlinXScale, (y + perlinOffSetY) * perlinYScale); 
            }
        }
        terrainData.SetHeights(0, 0, heightMap);
    }


    public void RandomTerrain()
    {
        int resolution = terrainData.heightmapResolution;
        float[,] heightMap = terrainData.GetHeights(0, 0, resolution, resolution);
        for (int x = 0; x < resolution; x++)
        {
            for(int z = 0; z < resolution; z++)
            {
                heightMap[x, z] = UnityEngine.Random.Range(randomHeightRange.x, randomHeightRange.y);
            }

        }
        terrainData.SetHeights(0, 0, heightMap);
    
    }
    public void ResetTerrain()
    {
        int resolution = terrainData.heightmapResolution;
        float[,] heightMap = terrainData.GetHeights(0, 0, resolution, resolution);
        for (int x = 0; x < resolution; x++)
        {
            for (int z = 0; z < resolution; z++)
            {
                heightMap[x, z] = 0;
            }

        }
        terrainData.SetHeights(0, 0, heightMap);

    }

    public void LoadTexture()
    {
        int resolution = terrainData.heightmapResolution;
        float[,] heightMap = new float[resolution, resolution];

        for(int x = 0; x < resolution; x++)
        {
            for( int z = 0; z < resolution;z++)
            {
                heightMap[x, z] = heightMapImage.GetPixel((int)(x * heightMapScale.x), 
                    (int)(z * heightMapScale.z)).grayscale * heightMapScale.y;
                                                        
            }
        }
        terrainData.SetHeights(0,0, heightMap); 
    }

    private void OnEnable()
    {
        Debug.Log("Inirialising Terrain Data");
        terrain = GetComponent<Terrain>();
        terrainData = Terrain.activeTerrain.terrainData;

    }
    private void Awake()
    {
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");

        AddTag(tagsProp, "Terrain");
        AddTag(tagsProp, "Cloud");
        AddTag(tagsProp, "Shore");
        
        //apply tag changes to tag database
        tagManager.ApplyModifiedProperties();

        gameObject.tag = "Terrain";
    }

    void AddTag(SerializedProperty tagsProp, string newTag)
    {
        bool found = false;

        //Ensure that tag doesn't exist already
        for (int i = 0; i < tagsProp.arraySize; i++)
        {
            SerializedProperty t = tagsProp.GetArrayElementAtIndex(i);
            if (t.stringValue.Equals(newTag)) { found = true; break; }
        }

        //add new tawg
        if(!found)
        {
            tagsProp.InsertArrayElementAtIndex(0);
            SerializedProperty newTagProp = tagsProp.GetArrayElementAtIndex(0);
            newTagProp.stringValue = newTag;   
        }
    }
}
