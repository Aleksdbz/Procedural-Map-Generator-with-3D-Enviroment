
using UnityEngine;

public static class  Utils
{
 
    public static float fBM(float x, float y, int oct, float persistnace)
    {
        float total = 0; // This accumulates the final height value for the given x, y.
        float frequency = 1; //How close are waves togeher 
        float amplitude = 1;// Amplitude determines the height of the wave crests, This controls the "strength" or "impact" of each successive octave.
        float maxValue = 0;  // Used to normalize the final result

        for (int i = 0; i < oct; i++)
        {
            total += Mathf.PerlinNoise(x * frequency, y * frequency) * amplitude;
            maxValue += amplitude;
            amplitude *= persistnace;
            frequency *= 2;  // Double the frequency with each octave to make the wave crests more frequent.

        }
        return total / maxValue;
    }


}
