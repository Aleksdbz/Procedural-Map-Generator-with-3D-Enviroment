using UnityEditor;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;


[ExecuteInEditMode]

public class CustomTerrain : MonoBehaviour
{
    #region Public Data
    public Vector2 randomHeightRange = new Vector2(0, 0.1f);
    public Texture2D heightMapImage;
    public Vector3 heightMapScale = new Vector3(1, 1, 1);
    public Terrain terrain;
    public TerrainData terrainData;
    #endregion

    public bool resetTerrain;

    #region PerlinNoiseSettings
    public float perlinXScale = 0.01f;
    public float perlinYScale = 0.01f;
    public int perlinOffSetX = 0;
    public int perlinOffSetY = 0;
    public int perlinOctaves = 3;
    public float perlinPersistance = 8;
    public float perlinHeightScale = 0.09f;
    #endregion

    //MULTIPLE PERLIN ----------
    [System.Serializable]
    public class PerlinParameters
    {
        public float mperlinXScale = 0.01f;
        public float mperlinYScale = 0.01f;
        public int mperlinOffSetX = 0;
        public int mperlinOffSetY = 0;
        public int mperlinOctaves = 3;
        public float mperlinPersistance = 8;
        public float mperlinHeightScale = 0.09f;
        public bool remove = false;
    }

    public List<PerlinParameters> perlinParameters = new List<PerlinParameters>()
    {
        new PerlinParameters()
    };

    float[,] GetHeightMap()
    {
        int resolution = terrainData.heightmapResolution;

        if (!resetTerrain)
        {
            return terrainData.GetHeights(0, 0, resolution, resolution);
        }
        else
            return new float[resolution, resolution];
    }

    public void MultiplePerlinTerrain()
    {
        int resolutin = terrainData.heightmapResolution; 
        float[,] heightMap = GetHeightMap();  
        for(int y=0; y<resolutin; y++)
        {
            for(int x=0; x<resolutin; x++) 
            {
                foreach (PerlinParameters p in perlinParameters)
                {
                    heightMap[x, y] += Utils.fBM((x + p.mperlinOffSetX) * p.mperlinXScale,
                        (y * p.mperlinYScale)
                        * p.mperlinYScale, p.mperlinOctaves, p.mperlinPersistance)
                        * p.mperlinHeightScale;
                }
            }
        }
        terrainData.SetHeights(0, 0, heightMap);
    }
    public void AddNewPerlin()
    {
        perlinParameters.Add(new PerlinParameters());
    }
    public void RemovePerlin()
    {
        List<PerlinParameters> keptPerlinParameters = new List<PerlinParameters>(); 
        for(int i =0;  i<perlinParameters.Count; i++)
        {
            if (!perlinParameters[i].remove)
            {
                keptPerlinParameters.Add(perlinParameters[i]);
            }
        }   
        if(keptPerlinParameters.Count == 0) //don't want to keep any
        {
            keptPerlinParameters.Add(perlinParameters[0]); //add at least 1
        }
        perlinParameters = keptPerlinParameters;
    }
    public void Perlin()
    {
        int resolution = terrainData.heightmapResolution;
        float[,] heightMap = GetHeightMap();
        for (int y=0; y<resolution; y++)
        {
            for(int x=0; x<resolution; x++)
            {
                heightMap[x,y] += Utils.fBM((x+perlinOffSetX) * perlinXScale, (y+ perlinOffSetY) * perlinYScale,perlinOctaves,
                    perlinPersistance) * perlinHeightScale;

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
                heightMap[x, z] += UnityEngine.Random.Range(randomHeightRange.x, randomHeightRange.y);
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
        float[,] heightMap = GetHeightMap();

        for (int x = 0; x < resolution; x++)
        {
            for( int z = 0; z < resolution;z++)
            {
                heightMap[x, z] += heightMapImage.GetPixel((int)(x * heightMapScale.x), 
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
