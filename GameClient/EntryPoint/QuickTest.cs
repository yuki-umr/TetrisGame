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
        List<long> values = new() {
            6017209, 5870929, 4977361, 4892944, 4752400, 4635409, 4910656, 7166329, 5579044, 5740816, 7952400, 7767369,
            7767369, 7241481, 6906384, 6859161, 6754801, 8076964, 8042896, 7333264, 7447441, 7102225, 6864400, 3591025, 5856400, 4173849, 6375625,
            5442889, 5419584, 5419584, 4981824, 4986289, 5198400, 5193841, 3892729, 7102225, 6115729, 6091024, 5779216, 5377761, 5555449, 8122500,
            4826809, 7535025, 7017201, 6843456, 6843456, 6512704, 6125625, 3968064, 3763600
        };

        Console.WriteLine(values.Count);
        Console.WriteLine(values.Max());
        Console.WriteLine(values.IndexOf(values.Max()));
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