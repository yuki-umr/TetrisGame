using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using GameClient.Tetris;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace GameClient.EntryPoint;

public class LogGraphViewer : WindowManager {
    private const int CheckboxSize = 16, CheckboxSpace = 32, CheckboxCheckSize = 12, CheckboxTextShift = 32, MaxTextWidth = 160, SmoothnessFactor = 4;
    private static readonly Vector2Int CheckboxCheckShift = new (2, 2), CheckboxStart = new (768, 160);
    
    public override Vector2Int InitialWindowSize => new(Constants.WindowWidth, Constants.WindowHeight);
    public override bool FixedTimeStep => true;
    public override int MinimumFrameTime => 4;

    private string folderName = "230121_exp";
    // private readonly Dictionary<int, Texture2D> textures = new();
    private ButtonState previousLeftButtonState, previousRightButtonState;

    private readonly BotSettings currentSettings = new();
    private readonly Dictionary<string, Texture2D> textures = new();
    private readonly Dictionary<string, List<object>> allSettingOptions = new();
    private readonly Dictionary<string, int> settingValueIndexes = new();
    private readonly Dictionary<string, string> fileNames = new();
    private string currentSerial;

    // file format: {date}/ind{index}_st{serializedSetting}.png
    
    protected override void OnInitialize() { }

    protected override int OnUpdate() {
        MouseState mouseState = Mouse.GetState();
        bool leftPressed = mouseState.LeftButton == ButtonState.Pressed && previousLeftButtonState == ButtonState.Released;
        bool rightPressed = mouseState.RightButton == ButtonState.Pressed && previousRightButtonState == ButtonState.Released;
        if (leftPressed || rightPressed) {
            int i = 0;
            foreach (var (propName, values) in allSettingOptions) {
                if (CheckboxStart.x - SmoothnessFactor <= mouseState.X && mouseState.X <= CheckboxStart.x + CheckboxTextShift + MaxTextWidth + SmoothnessFactor 
                                                                       && CheckboxStart.y + CheckboxSpace * i - SmoothnessFactor <= mouseState.Y && 
                                                                       mouseState.Y <= CheckboxStart.y + CheckboxSpace * i + CheckboxSize + SmoothnessFactor) {
                    settingValueIndexes[propName] += leftPressed ? 1 : -1;
                    if (settingValueIndexes[propName] >= values.Count) settingValueIndexes[propName] = 0;
                    else if (settingValueIndexes[propName] < 0) settingValueIndexes[propName] = values.Count - 1;
                    
                    UpdateCurrentSettings();
                    break;
                }

                i++;
            }
        }

        previousLeftButtonState = mouseState.LeftButton;
        previousRightButtonState = mouseState.RightButton;
        return 0;
    }

    private void UpdateCurrentSettings() {
        foreach (var (propName, values) in allSettingOptions) {
            object old = currentSettings.GetProperty(propName);
            currentSettings.SetProperty(propName, values[settingValueIndexes[propName]]);
            object newValue = currentSettings.GetProperty(propName);
            if (!Equals(old, newValue)) Console.WriteLine($"{propName}: {old} -> {newValue}");
        }
        
        currentSettings.GetModifiedProperties(forceRefresh: true);
        currentSerial = currentSettings.Serialized();
    }

    protected override void OnLoadContent(GraphicsDevice graphicsDevice) {
        foreach (string arg in Program.GetOptions().Args) {
            if (arg.StartsWith("folder=")) folderName = arg[7..];
        }
        
        string[] plotFiles = Directory.GetFiles($"log/{folderName}/", "ind*.png");
        Console.WriteLine(plotFiles.Length);
        BotSettings baseSettings = null;
        foreach (string file in plotFiles) {
            string serializedSetting = Regex.Match(file, $"_st{BotSettings.SerializedRegex}").Value[3..];
            BotSettings settings = new BotSettings(serializedSetting);
            if (baseSettings == null) {
                baseSettings = settings;
                foreach (PropertyInfo prop in typeof(BotSettings).GetProperties()) {
                    allSettingOptions[prop.Name] = new List<object> { prop.GetValue(baseSettings) };
                    settingValueIndexes[prop.Name] = 0;
                }
            } else {
                foreach (PropertyInfo prop in settings.GetModifiedProperties(baseSettings)) {
                    allSettingOptions[prop.Name].Add(prop.GetValue(settings));
                }
            }
            
            textures[serializedSetting] = Texture2D.FromFile(graphicsDevice, file);
            fileNames[serializedSetting] = file;
        }

        foreach (string key in allSettingOptions.Keys.ToList()) {
            allSettingOptions[key] = allSettingOptions[key].ToImmutableSortedSet().ToList();
            if (allSettingOptions[key].Count == 1) allSettingOptions.Remove(key);
        }

        UpdateCurrentSettings();
        base.OnLoadContent(graphicsDevice);
    }

    protected override void OnDraw(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch) {
        int i = 0;
        foreach (var (propName, values) in allSettingOptions) {
            Primitives.DrawSquare(spriteBatch, CheckboxStart.x, CheckboxStart.y + CheckboxSpace * i, CheckboxSize, 0xff808080u);
            string text = $"{propName} ({values.Count}) : {values[settingValueIndexes[propName]]}";
            Primitives.DrawText(spriteBatch, text, CheckboxStart.x + CheckboxTextShift, CheckboxStart.y + CheckboxSpace * i, 16, 0xffffffffu);
            i++;
        }

        if (textures.ContainsKey(currentSerial) && textures[currentSerial] != null) {
            spriteBatch.Draw(textures[currentSerial], new Vector2(64, 32), Color.White);
            Primitives.DrawText(spriteBatch, fileNames[currentSerial], 0, 0, 16, Color.White);
        } else {
            Primitives.DrawText(spriteBatch, "No data", 256, 256, 16, Color.White);
        }
        
        base.OnDraw(graphicsDevice, spriteBatch);
    }
}