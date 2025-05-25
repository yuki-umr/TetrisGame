using System.Collections.Generic;

namespace GameClient.Tetris;

public class SearchStats {
    public string lastSearchStats;
    public string totalSearchStats;
    public string searchSpeed;

    private readonly Dictionary<string, string> additionalStats = new();
    
    public IReadOnlyDictionary<string, string> AdditionalStats => additionalStats;
    
    public SearchStats(string lastSearchStats, string totalSearchStats, string searchSpeed) {
        this.lastSearchStats = lastSearchStats;
        this.totalSearchStats = totalSearchStats;
        this.searchSpeed = searchSpeed;
    }
    
    public void AddStat(string key, string value) => additionalStats[key] = value;
}