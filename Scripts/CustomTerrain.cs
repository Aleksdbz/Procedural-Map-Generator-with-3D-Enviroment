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

    #region Voronoi 
    public int voronoiPeaks = 5;
    public float voronoifallOff = 0.2f;
    public float voronoiDropOff = 0.6f;
    public float voronoiMaxHeight = 0.5f;
    public float voronoiMinHeight = 0.1f;
    public enum VoronoiType {Linear = 0, Power = 1, Combined = 2, SinPow = 4};
    public VoronoiType voronoiType = VoronoiType.Linear;
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

    public void MidPointDisplacment()
    {
        float[,] heightMap = GetHeightMap();
        int resolution = terrainData.heightmapResolution;
        int width = resolution - 1;
        int squareSize = width;
        float height = (float)squareSize / 2f * 0.01f;
        float roughness = 2.0f;
        float heightDampener = (float)Mathf.Pow(2, -1 * roughness);

        int cornerX, cornerY;
        int midX, midY;
        int pmidXL,pmidXR,pmidYI,pmidYD;

        heightMap[0, 0] = UnityEngine.Random.Range(0f, 0.2f);
        heightMap[0, resolution - 2] = UnityEngine.Random.Range(0f, 0.2f);
        heightMap[resolution - 2,0] = UnityEngine.Random.Range(0f, 0.2f);
        heightMap[resolution - 2, resolution -1] = UnityEngine.Random.Range(0f, 0.2f);

        while (squareSize > 0)
        {
            for (int x = 0; x < width; x += squareSize)
            {
                for (int y = 0; y < width; y += squareSize)
                {
                    cornerX = (x + squareSize);
                    cornerY = (y + squareSize);

                    midX = (int)(x + squareSize / 2f);
                    midY = (int)(y + squareSize / 2f);

                    heightMap[midX, midY] = (float)((heightMap[x, y] +
                        heightMap[cornerX, y] +
                        heightMap[x, cornerY] +
                        heightMap[cornerX, cornerY]) / 4f +
                        UnityEngine.Random.Range(-height, height)); 

                }
            }
            squareSize = (int)(squareSize / 2f);
            height *= heightDampener;
        }
        terrainData.SetHeights(0,0 ,heightMap); 

    }
    public void Voronoi()
    {
        int resolution = terrainData.heightmapResolution;
        float[,] heightMap = GetHeightMap();    


        for (int p = 0; p< voronoiPeaks; p++)
        {
            Vector3 peak = new Vector3(UnityEngine.Random.Range(0, terrainData.heightmapResolution),
                                      UnityEngine.Random.Range(voronoiMinHeight, voronoiMaxHeight), 
                                      UnityEngine.Random.Range(0, terrainData.heightmapResolution));

            if (heightMap[(int)peak.x, (int)peak.z] < peak.y)
                heightMap[(int)peak.x, (int)peak.z] = peak.y;
            else
                continue;

            Vector2 peakLocation = new Vector2(peak.x, peak.z);
            float maxDistance = Vector2.Distance(new Vector2(0, 0), new Vector2(resolution, resolution));
           
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    if (!(x == peak.x && y == peak.z))
                    {
                        float distanceToPeak = Vector2.Distance(peakLocation, new Vector2(x, y)) / maxDistance;
                        float h;

                        if(voronoiType == VoronoiType.Combined)
                        {
                            h = peak.y - distanceToPeak * voronoifallOff - Mathf.Pow(distanceToPeak, voronoiDropOff); //Combined
                        }
                        else if(voronoiType == VoronoiType.Power)
                        {
                            h = peak.y - MathF.Pow(distanceToPeak, voronoiDropOff) * voronoifallOff; //power
                        }
                        else if (voronoiType == VoronoiType.SinPow)
                        {
                            h = peak.y - Mathf.Pow(distanceToPeak * 3, voronoifallOff) - MathF.Sin(distanceToPeak * 2 * MathF.PI) / voronoiDropOff; //sinpow
                        }
                        else
                        {
                            h = peak.y - distanceToPeak * voronoifallOff; //linear 
                        }


                         h = peak.y - distanceToPeak * voronoifallOff - MathF.Pow(distanceToPeak, voronoiDropOff);
                        if(heightMap[x,y] < h)
                        heightMap[x, y] = h;
                    }
                }
            }
        }

         
        terrainData.SetHeights(0,0 ,heightMap);
    }
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
                        (y * p.mperlinOffSetY) * p.mperlinYScale,
                        p.mperlinOctaves, p.mperlinPersistance) * p.mperlinHeightScale;

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
        for(int i = 0;  i<perlinParameters.Count; i++)
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
