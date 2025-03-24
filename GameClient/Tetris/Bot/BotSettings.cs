using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using GeneticSharp;

namespace GameClient.Tetris; 

public class BotSettings {
    public const string SerializedRegex = "[a-zA-Z0-9[\\];]+", SerializedRegexSequence = "[a-zA-Z0-9[\\];,]+";
    private static readonly Dictionary<string, PropertyInfo> AllProperties = new();
    
    // bool, float, int, string is currently supported (Gene[] is ignored)
    [Label("tsd")] public bool NoTsd { get; init; } = false;
    [Label("tst")] public bool NoTst { get; init; } = false;
    [Label("pc")] public bool NoPatternCheck { get; init; } = false;
    [Label("wwp")] public bool NoWallWellPenalty { get; init; } = false;
    [Label("wb")] public bool NoInnerWellBonus { get; init; } = false;
    [Label("wwh")] public bool FixedWellHeight { get; init; } = true;
    [Label("tw")] public bool CheckTWaste { get; init; } = false;
    [Label("wwm")] public int WallWellMultiplier { get; init; } = 3;
    [Label("bw")] public int BeamWidth { get; init; } = 12;
    [Label("bd")] public int BeamDepth { get; init; } = 5;
    [Label("wwcc")] public bool WallWellCcBased { get; init; } = false;
    [Label("dmod")] public float TsdWeightMod { get; init; } = 2;
    [Label("ptst")] public int PreTstWeight { get; init; } = -40;
    [Label("tmod")] public float TstWeightMod { get; init; } = 1;
    [Label("pdmod")] public int PreTsdWeight { get; init; } = -100;
    [Label("dw")] public int DoubleLineWeight { get; init; } = 450;

    // Flags to disable certain evaluation functions
    [Label("nh")] public bool NoHeight { get; init; } = false;
    [Label("nc")] public bool NoCeiling { get; init; } = false;
    [Label("ncd")] public bool NoCeilDepth { get; init; } = false;
    [Label("ns")] public bool NoSteepness { get; init; } = false;
    [Label("nss")] public bool NoSSteepness { get; init; } = false;
    [Label("nf")] public bool NoFlatness { get; init; } = false;
    [Label("nhl")] public bool NoHeightLevel { get; init; } = false;
    [Label("nw")] public bool NoWellDistance { get; init; } = false;
    
    // Genetic Algorithm
    [Label("gene")] public Gene[] Genes { get; init; } = null;

    private List<PropertyInfo> modifiedProps;

    static BotSettings() {
        PropertyInfo[] props = typeof(BotSettings).GetProperties();
        foreach (PropertyInfo prop in props) {
            LabelAttribute label = prop.GetCustomAttribute<LabelAttribute>();
            Debug.Assert(label != null);
            bool exists = AllProperties.ContainsKey(label.Label);
            Debug.Assert(!exists, $"BotSettings: Bot settings label \"{label.Label}\" is defined multiple times");
            AllProperties.Add(label.Label, prop);
        }
    }

    public BotSettings() { }

    public BotSettings(string settings) {
        byte[] bytes = Convert.FromBase64String(ToUnsafeSerial(settings));
        using MemoryStream stream = new MemoryStream(bytes);
        using BinaryReader reader = new BinaryReader(stream);

        int modifiedCount = reader.ReadByte();
        for (int i = 0; i < modifiedCount; i++) {
            string label = reader.ReadString();
            if (!AllProperties.ContainsKey(label)) {
                Console.WriteLine($"BotSettings: Failed to load settings from string {settings}, label \"{label}\" is not defined");
                return;
            }
            
            PropertyInfo prop = AllProperties[label];
            if (prop.PropertyType == typeof(bool)) {
                prop.SetValue(this, reader.ReadBoolean());
            } else if (prop.PropertyType == typeof(float)) {
                prop.SetValue(this, reader.ReadSingle());
            } else if (prop.PropertyType == typeof(int)) {
                prop.SetValue(this, reader.ReadInt32());
            } else if (prop.PropertyType == typeof(string)) {
                prop.SetValue(this, reader.ReadString());
            } else {
                Console.Error.WriteLine($"BotSettings: Read function not implemented for type {prop.PropertyType}");
            }
        }
    }

    public object GetProperty(string propName) => typeof(BotSettings).GetProperty(propName)!.GetValue(this);
    public void SetProperty(string propName, object value) => typeof(BotSettings).GetProperty(propName)!.SetValue(this, value);

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
        if (modifiedProps is not null && baseSettings is null && !forceRefresh) return modifiedProps;
        
        // Uses reflection to find deltas
        PropertyInfo[] props = typeof(BotSettings).GetProperties();
        List<PropertyInfo> baseModifiedProps;
        if (baseSettings is null) {
            modifiedProps = new List<PropertyInfo>();
            baseModifiedProps = modifiedProps;
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
        using MemoryStream stream = new MemoryStream();
        using BinaryWriter writer = new BinaryWriter(stream);
        writer.Write((byte) GetModifiedProperties().Count);
        foreach (PropertyInfo prop in GetModifiedProperties()) {
            string label = prop.GetCustomAttribute<LabelAttribute>()!.Label;
            writer.Write(label);
            
            object value = prop.GetValue(this);
            if (value is bool b) {
                writer.Write(b);
            } else if (value is float f) {
                writer.Write(f);
            } else if (value is int i) {
                writer.Write(i);
            } else if (value is string s) {
                writer.Write(s);
            } else {
                Console.Error.WriteLine($"BotSettings: Write function not implemented for type {value.GetType()}");
            }
        }

        string base64 = Convert.ToBase64String(stream.ToArray());
        return ToSafeSerial(base64);
    }
    
    private static string ToSafeSerial(string unsafeSerial) => unsafeSerial.Replace('/', '[').Replace('+', ']').Replace('=', ';');
    private static string ToUnsafeSerial(string safeSerial) => safeSerial.Replace('[', '/').Replace(']', '+').Replace(';', '=');

    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
    private sealed class LabelAttribute : Attribute {
        public string Label { get; }
        
        public LabelAttribute(string label) {
            Label = label;
        }
    }
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