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

    private static void TestGameStateEqualOperator() {
        for (int j = 0; j < 5; j++) {
            GameField a = new(initialField: new BitMatrix(new []{ 1u, 2u, 3u, 4u, 5u }, new Vector2Int(5, 10))), 
                b = new(initialField: new BitMatrix(new []{ 1u, 2u, 3u, 4u, 5u }, new Vector2Int(5, 10)));
            GameState stateA = new(a, 0, -1, bagSeed: 100), stateB = new(b, 0, -1, bagSeed: 100);
            Stopwatch sw = Stopwatch.StartNew();
            for (int i = 0; i < 10000000; i++) {
                bool res = stateA == stateB;
            }
            
            Console.WriteLine(sw.ElapsedMilliseconds + " " + (stateA == stateB));
        }
    }

    private static void TestMinoBag() {
        MinoBag bag = new MinoBag();
        MinoBag defaultBag = default;
        for (int i = 0; i < 14; i++) {
            // ulong bagNum = bag.bag;
            // for (int j = 0; j < 64; j++) {
            //     Console.Write((bagNum & (1ul << 63)) != 0 ? '1' : '0');
            //     bagNum <<= 1;
            //     if (j % 3 == 0) Console.Write(' ');
            // }

            // Console.WriteLine($" {bag.Pop()} {bag.Count}");
            Console.WriteLine($"{bag.Pop()}, {defaultBag.Pop()}");
        }
    }

    private static void TestBotSetting() {
        BotSettings setting = new BotSettings();
        Console.WriteLine(setting.Serialized());
        setting = new BotSettings { BeamDepth = 12, CheckTWaste = true, NoTsd = true, WallWellMultiplier = 2 };
        string based = setting.Serialized();
        Console.WriteLine($"{based} ({based.Length})");
        setting = new BotSettings(based);
        based = setting.Serialized();
        Console.WriteLine($"{based} ({based.Length})");
        Console.WriteLine(setting.BeamDepth);
        Console.WriteLine(setting.CheckTWaste);
        Console.WriteLine(setting.NoTsd);
        Console.WriteLine(setting.WallWellMultiplier);
        BotSettings newSetting = setting.Clone();
        // newSetting.NoTsd = false;
        Console.WriteLine(setting.NoTsd);
    }

    private static void TestBotSettingsJson() {
        Console.WriteLine(JsonConvert.SerializeObject(new BotSettings()));
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

    private class Container {
        public int someValue;
    }
}