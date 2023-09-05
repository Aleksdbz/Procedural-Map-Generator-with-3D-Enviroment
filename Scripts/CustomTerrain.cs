using UnityEditor;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using static UnityEditor.PlayerSettings;
using System.Security.Cryptography;


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
    public enum VoronoiType { Linear = 0, Power = 1, Combined = 2, SinPow = 4 };
    public VoronoiType voronoiType = VoronoiType.Linear;
    #endregion

    #region MidPointDisplacment
    public float minheight = -10f;
    public float maxheight = 10f;
    public float MPDroughness = 2f;
    public float MDPheightDampener = 2f;
    public int SmoothAmount = 2;
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
    #region SplatMaps

    [System.Serializable]
    public class SplatHeights
    {
        public Texture2D texture = null;
        public float minHeight = 0.1f;
        public float maxHeight = 0.2f;
        public bool remove = false;
        public Vector2 tileOffset = new Vector2(0, 0);
        public Vector2 tileSize = new Vector2(50, 50);

    }
    public List<SplatHeights> splatHeights = new List<SplatHeights>()
    {
       new SplatHeights()
    };
    
    public void AddNewSplatHeights() //It will called to GUI when you hit +
    {
        splatHeights.Add(new SplatHeights()); 
    }
    public void RemoveSplatHeights() //same just -
    {
        List<SplatHeights> keptSplatHeights = new List<SplatHeights>();
        for(int i = 0; i < splatHeights.Count; i++)
        {
            if (!splatHeights[i].remove)
            {
                keptSplatHeights.Add(splatHeights[i]);
            }
        }
        if(keptSplatHeights.Count == 0) //dont want to keep any
        {
            keptSplatHeights.Add(splatHeights[0]); //add at least 1 
        }
        splatHeights = keptSplatHeights;    
    }
    public void SplatMaps()
    {
        TerrainLayer[] newSplatProtoypes;
        newSplatProtoypes = new TerrainLayer[splatHeights.Count];
        int spinIndex = 0;
        foreach(SplatHeights sh in splatHeights)
        {
            newSplatProtoypes[spinIndex] = new TerrainLayer();
            newSplatProtoypes[spinIndex].diffuseTexture = sh.texture;
            newSplatProtoypes[spinIndex].tileOffset = sh.tileOffset;
            newSplatProtoypes[spinIndex].tileSize = sh.tileSize;
            newSplatProtoypes[spinIndex].diffuseTexture.Apply();
            spinIndex++;

        }
        terrainData.terrainLayers = newSplatProtoypes;
    }
    #endregion
    List<Vector2> GenerateNeugbours(Vector2 pos, int width, int height)
    {
        List<Vector2> neighbours = new List<Vector2>();
        for (int y = -1; y < 2; y++)
        {
            for (int x = -1; x < 2; x++)
            {
                if (!(x == 0 && y == 0))
                {
                    Vector2 nPos = new Vector2(Mathf.Clamp(pos.x + x, 0, width - 1), Mathf.Clamp(pos.y + y, 0, height - 1));
                    if (!neighbours.Contains(nPos))
                        neighbours.Add(nPos);
                }

            }
        }
        return neighbours;
    }
    public void Smooth()
    {
        
        int resolution = terrainData.heightmapResolution;
        float[,] heightMap = terrainData.GetHeights(0, 0, resolution, resolution);
        float smoothProgress = 0;

        for(int s = 0; s < SmoothAmount; s++)
        {
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float avgHeight = heightMap[x, y];
                    List<Vector2> neigbours = GenerateNeugbours(new Vector2(x, y), resolution, resolution);

                    foreach (Vector2 n in neigbours)
                    {
                        avgHeight += heightMap[(int)n.x, (int)n.y];

                    }
                    heightMap[x, y] = avgHeight / ((float)neigbours.Count + 1);
                }

            }
            smoothProgress++;
        }
      
        terrainData.SetHeights(0, 0, heightMap);
    }
    public void MidPointDisplacment()
    {
        float[,] heightMap = GetHeightMap();
        int resolution = terrainData.heightmapResolution;
        int width = resolution - 1;
        int squareSize = width;
        float heightMin = minheight;
        float heightMax = maxheight;
        float  heightDampener = (float)Mathf.Pow(MDPheightDampener, -1 * MPDroughness);

        int cornerX, cornerY;
        int midX, midY;
        int pmidXL,pmidXR,pmidYU,pmidYD;

      //heightMap[0, 0] = UnityEngine.Random.Range(0f, 0.2f);
       // heightMap[0, resolution - 2] = UnityEngine.Random.Range(0f, 0.2f);
      //  heightMap[resolution - 2,0] = UnityEngine.Random.Range(0f, 0.2f);
       // heightMap[resolution - 2, resolution -1] = UnityEngine.Random.Range(0f, 0.2f);

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
                        UnityEngine.Random.Range(heightMin, heightMax)); 

                }
            }
            for(int x = 0; x < width; x+= squareSize)
            {
                for (int y = 0; y < width; y += squareSize)
                {
                    cornerX = (x + squareSize);
                    cornerY = (y + squareSize);

                    midX = (int)(x + squareSize / 2f);
                    midY = (int)(y + squareSize / 2f); 

                    pmidXR = (int)(midX + squareSize);
                    pmidYU = (int)(midY + squareSize);
                    pmidXL = (int)(midX - squareSize);
                    pmidYD = (int)(midY - squareSize);

                    if (pmidXL < -0 || pmidYD <= 0 || pmidXR >= width - 1 || pmidYU >= width - 1) continue;

                    //Saquare value for the bottom mid side 
                    heightMap[midX, y] = (float)((heightMap[midX, midY] + heightMap[x, y] + heightMap[midX, pmidYD] + heightMap[cornerX, y]) / 4.0f + UnityEngine.Random.Range(heightMin, heightMax));
                    //Square value for left mid side
                    heightMap[x, midY] = (float)((heightMap[midX, midY] + heightMap[x, y] + heightMap[pmidXL, midY] + heightMap[x, cornerY]) / 4f + UnityEngine.Random.Range(heightMin, heightMax));
                    //Square value for top mid side 
                    heightMap[midX, cornerY] = (float)((heightMap[midX, midY] + heightMap[x, cornerY] + heightMap[midX, pmidYU] + heightMap[cornerX, cornerY]) / 4f + UnityEngine.Random.Range(heightMin, heightMax));
                    //Square value for mid right side
                    heightMap[cornerX, midY] = (float)((heightMap[cornerX,y] + heightMap[cornerX, cornerY] + heightMap[pmidXR,midY] + heightMap[midY, midX]) / 4f + UnityEngine.Random.Range(heightMin, heightMax));

                }
            }

            squareSize = (int)(squareSize / 2f);
            heightMin *= heightDampener;
            heightMax *= heightDampener;
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


            if (heightMap[(int)peak.x, (int)peak.z] >= peak.y) 
            {
                continue;
            }
            heightMap[(int)peak.x, (int)peak.z] = peak.y;


            Vector2 peakLocation = new Vector2(peak.x, peak.z);
            float maxDistance = Vector2.Distance(new Vector2(0, 0), new Vector2(resolution, resolution));
           
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {

                    if(x == peak.x && y == peak.z) 
                    {
                        continue;
                    }

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


                    if(heightMap[x,y] < h)
                    heightMap[x, y] = h;
                    
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
