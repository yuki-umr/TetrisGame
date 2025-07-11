using System;

namespace GameClient.Tetris;

public static class RandomGen {
    private static Random _random = new();
    
    public static int ResetSeed() {
        int seed = _random.Next();
        SetSeed(seed);
        
        return seed;
    }
    
    public static void SetSeed(int seed) {
        _random = new Random(seed);
    }
    
    public static int Int() {
        return _random.Next();
    }
    
    public static int Int(int max) {
        return _random.Next(max);
    }
    
    public static int Int(int min, int max) {
        return _random.Next(min, max);
    }
    
    public static float Float() {
        return _random.NextSingle();
    }
    
    public static float Float(float max) {
        return _random.NextSingle() * max;
    }
    
    public static float Float(float min, float max) {
        return _random.NextSingle() * (max - min) + min;
    }
}