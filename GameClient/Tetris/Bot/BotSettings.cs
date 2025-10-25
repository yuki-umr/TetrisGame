using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using GeneticSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;

namespace GameClient.Tetris; 

public class BotSettings {
    public const string SerializedRegex = "[a-zA-Z0-9[\\];]+", SerializedRegexSequence = "[a-zA-Z0-9[\\];,]+";
    
    // bool, float, int, string is currently supported (Gene[] is ignored)
    [JsonProperty("tsd")] public bool NoTsd { get; set; } = false;
    [JsonProperty("tst")] public bool NoTst { get; set; } = false;
    [JsonProperty("pc")] public bool NoPatternCheck { get; set; } = false;
    [JsonProperty("wwp")] public bool NoWallWellPenalty { get; set; } = false;
    [JsonProperty("wb")] public bool NoInnerWellBonus { get; set; } = false;
    [JsonProperty("wwh")] public bool FixedWellHeight { get; set; } = true;
    [JsonProperty("tw")] public bool CheckTWaste { get; set; } = false;
    [JsonProperty("wwm")] public int WallWellMultiplier { get; set; } = 3;
    [JsonProperty("wwcc")] public bool WallWellCcBased { get; set; } = false;
    [JsonProperty("dmod")] public float TsdWeightMod { get; set; } = 2;
    [JsonProperty("ptst")] public int PreTstWeight { get; set; } = 40;
    [JsonProperty("tmod")] public float TstWeightMod { get; set; } = 1;
    [JsonProperty("pdmod")] public int PreTsdWeight { get; set; } = 100;
    [JsonProperty("odw")] public bool OverrideDoubleWeight { get; set; } = false;
    [JsonProperty("dw")] public int DoubleLineWeight { get; set; } = -450;

    // Flags to disable certain evaluation functions
    [JsonProperty("nh")] public bool NoHeight { get; set; } = false;
    [JsonProperty("nc")] public bool NoCeiling { get; set; } = false;
    [JsonProperty("ncd")] public bool NoCeilDepth { get; set; } = false;
    [JsonProperty("ns")] public bool NoSteepness { get; set; } = false;
    [JsonProperty("nss")] public bool NoSSteepness { get; set; } = false;
    [JsonProperty("nf")] public bool NoFlatness { get; set; } = false;
    [JsonProperty("nhl")] public bool NoHeightLevel { get; set; } = false;
    [JsonProperty("nw")] public bool NoWellDistance { get; set; } = false;
    
    // Genetic Algorithm
    [JsonProperty("gene")] public Gene[] Genes { get; set; } = null;
    
    // switching between different search algorithms
    [JsonProperty("beam")] public SearchAlgorithm SearchType { get; set; } = SearchAlgorithm.Beam;
    [JsonProperty("eval")] public EvaluatorType Evaluator { get; set; } = EvaluatorType.Default;
    [JsonProperty("bw")] public int BeamWidth { get; set; } = 12;
    [JsonProperty("bd")] public int BeamDepth { get; set; } = 5;
    [JsonProperty("mcit")] public int MCTSIterations { get; set; } = 1000;
    
    public enum SearchAlgorithm {
        Beam, MCTS
    }
    
    public enum EvaluatorType {
        Default, Thiery
    }

    private List<PropertyInfo> modifiedPropsCache;

    public BotSettings() { }

    public static BotSettings Deserialize(string settings) {
        byte[] bytes = Convert.FromBase64String(ToUnsafeSerial(settings));
        using MemoryStream stream = new(bytes);
        using BsonDataReader reader = new(stream);
        JsonSerializer serializer = new();
        BotSettings deserialized = serializer.Deserialize<BotSettings>(reader);
        
        return deserialized ?? new BotSettings();
    }

    public object GetProperty(string propName) => typeof(BotSettings).GetProperty(propName)!.GetValue(this);
    public void SetProperty(string propName, object value) => typeof(BotSettings).GetProperty(propName)!.SetValue(this, value);
    
    public ISearcher GetSearcher() {
        if (SearchType == SearchAlgorithm.Beam) {
            return new BeamSearcher(BeamDepth, BeamWidth);
        } else if (SearchType == SearchAlgorithm.MCTS) {
            return new MonteCarloSearcher(MCTSIterations);
        } else {
            throw new NotSupportedException($"Search algorithm {SearchType} is not supported.");
        }
    }

    public IEvaluator GetEvaluator() {
        if (Evaluator == EvaluatorType.Default) {
            return DefaultEvaluator.GetDefault(this);
        } else if (Evaluator == EvaluatorType.Thiery) {
            return ThieryEvaluator.GetDefault(this);
        } else {
            throw new NotSupportedException($"Evaluator type {Evaluator} is not supported.");
        }
    }

    public static List<BotSettings> GenerateAllSettings(BotSettingsVarianceOptions options) {
        BotSettings template = new BotSettings();
        List<BotSettings> allSettings = new() { template };
        foreach (var (objectName, values) in options.objectOptions) {
            template.SetProperty(objectName, values[0]);
        }
        
        foreach (string boolName in options.boolOptions) {
            template.SetProperty(boolName, false);
        }
        
        foreach (var (objectName, values) in options.objectOptions) {
            int initialCount = allSettings.Count;
            for (int i = 0; i < initialCount; i++) {
                for (int j = 1; j < values.Length; j++) {
                    BotSettings clone = allSettings[i].Clone();
                    clone.SetProperty(objectName, values[j]);
                    allSettings.Add(clone);
                }
            }
        }

        foreach (string boolName in options.boolOptions) {
            int initialCount = allSettings.Count;
            for (int i = 0; i < initialCount; i++) {
                BotSettings clone = allSettings[i].Clone();
                clone.SetProperty(boolName, true);
                allSettings.Add(clone);
            }
        }

        return allSettings;
    }

    public BotSettings Clone() => (BotSettings)MemberwiseClone();

    public List<PropertyInfo> GetModifiedProperties(BotSettings baseSettings = null, bool forceRefresh = false) {
        if (modifiedPropsCache is not null && baseSettings is null && !forceRefresh) return modifiedPropsCache;
        
        // Uses reflection to find deltas
        PropertyInfo[] props = typeof(BotSettings).GetProperties();
        List<PropertyInfo> baseModifiedProps;
        if (baseSettings is null) {
            modifiedPropsCache = new List<PropertyInfo>();
            baseModifiedProps = modifiedPropsCache;
        } else {
            baseModifiedProps = new List<PropertyInfo>();
        }
        
        baseSettings ??= new BotSettings();
        foreach (PropertyInfo prop in props) {
            if (prop.PropertyType == typeof(int[]) || Equals(prop.GetValue(this), prop.GetValue(baseSettings))) continue;
            baseModifiedProps.Add(prop);
        }

        return baseModifiedProps;
    }

    public string Serialized() {
        using MemoryStream stream = new();
        using BsonDataWriter writer = new(stream);

        JsonSerializer serializer = new();
        serializer.Serialize(writer, this);

        string base64 = Convert.ToBase64String(stream.ToArray());
        return ToSafeSerial(base64);
    }
    
    private static string ToSafeSerial(string unsafeSerial) => unsafeSerial.Replace('/', '[').Replace('+', ']').Replace('=', ';');
    private static string ToUnsafeSerial(string safeSerial) => safeSerial.Replace('[', '/').Replace(']', '+').Replace(';', '=');
}

public class BotSettingsVarianceOptions {
    public readonly List<string> boolOptions = new();
    public readonly List<(string, object[])> objectOptions = new();

    public BotSettingsVarianceOptions AddBoolOptions(params string[] boolOptions) {
        this.boolOptions.AddRange(boolOptions);
        return this;
    }

    public BotSettingsVarianceOptions AddObjectOptions(params (string, object[])[] objectOptions) {
        this.objectOptions.AddRange(objectOptions);
        return this;
    }
}