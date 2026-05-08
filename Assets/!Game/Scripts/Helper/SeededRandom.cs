using UnityEngine;

public class SeededRandom
{
    private uint state;

    public SeededRandom(uint seed)
    {
        this.state = seed;
    }

    public float NextFloat()
    {
        this.state = this.state * 1664525 + 1013904223;
        return (float)(this.state / 4294967296.0);
    }

    public int NextInt(int min, int max)
    {
        return Mathf.FloorToInt(NextFloat() * (max - min)) + min;
    }
}