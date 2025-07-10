using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using GameClient.Tetris;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace GameClient.EntryPoint;

public class QuickTest {
    private static readonly int[] TestFlags = OldBotSettings.GetAllTestFlagPatterns(
        OldBotSettings.Flag.NoPatternCheck,
        OldBotSettings.Flag.NoWallWellPenalty,
        OldBotSettings.Flag.NoInnerWellBonus,
        OldBotSettings.Flag.FixedWellHeight,
        OldBotSettings.Flag.CheckTWaste
    );
    
    public QuickTest() {
        ConvertAllJson();
    }

    private static void ConvertAllJson() {
        string[] plotFiles = Directory.GetFiles("log/221228/", "*.json");
        Console.WriteLine(plotFiles.Length);
        foreach (string file in plotFiles) {
            string json = File.ReadAllText(file);
            PlayOutResult old = JsonConvert.DeserializeObject<PlayOutResult>(json);
            Console.WriteLine(old.settings);
            PlayOutSerializedResult result = new PlayOutSerializedResult(old.settings.Serialized());
            result.statistics.AddRange(old.statistics);
            File.WriteAllText(file, JsonConvert.SerializeObject(result));
        }
    }
}