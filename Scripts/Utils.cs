using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class  Utils
{
  //Fractial Browning Motion
  
    public static float fBM(float x, float y, int oct, float persistnace, int offSetX, int offSetY)
    {
        float total = 0;
        float frequency = 1; //How close are waves togeher 
        float amplitude = 1;
        float maxValue = 0; 
        for(int i = 0; i < oct; i++)
        {
            total += Mathf.PerlinNoise((x + offSetX) * frequency, (y + offSetY) * frequency) * amplitude;
            maxValue += amplitude;
            amplitude *= persistnace;
            frequency *= 2;
        }
        return total / maxValue;
    }


}
