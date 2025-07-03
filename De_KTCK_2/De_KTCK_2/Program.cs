using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

public class Player
{
    public string Name { get; set; }
    public bool IsActive { get; set; }
    public DateTime LastLogin { get; set; }
    public int Level { get; set; }
    public int VipLevel { get; set; }
    public int Gold { get; set; }
}

class Program
{
    private static readonly HttpClient httpClient = new HttpClient();
    private static readonly string githubJsonUrl = "https://raw.githubusercontent.com/NTH-VTC/OnlineDemoC-/refs/heads/main/lab12_players.json";
    private static readonly DateTime referenceTime = new DateTime(2025, 6, 30);

    static async Task Main(string[] args)
    {
        var players = await LoadPlayersFromGitHub();

        var top3VIP = players
            .Where(p => p.VipLevel > 0)
            .OrderByDescending(p => p.Level)
            .ThenByDescending(p => p.VipLevel)
            .Take(3)
            .Select((p, i) => new
            {
                Rank = i + 1,
                p.Name,
                p.VipLevel,
                p.Level,
                CurrentGold = p.Gold,
                AwardedGoldAmount = i == 0 ? 2000 : i == 1 ? 1500 : 1000
            })
            .ToList();

        Console.WriteLine("\n-- 2. TOP 3 NGUOI CHOI VIP CAP CAO NHAT VA GOLD THUONG DU KIEN --");
        Console.WriteLine("----------------------------------------------------------------------------");
        Console.WriteLine("| Hang | Ten Nguoi choi   | VIP Level | Level | Gold Hien Tai | Gold Duoc Thuong |");
        Console.WriteLine("----------------------------------------------------------------------------");
        foreach (var p in top3VIP)
        {
            Console.WriteLine($"| {p.Rank,-4} | {p.Name,-17} | {p.VipLevel,-9} | {p.Level,-5} | {p.CurrentGold,14:N0} | {p.AwardedGoldAmount,12:N0} |");
            Console.WriteLine("----------------------------------------------------------------------------");
        }

        var vipPayload = top3VIP.ToDictionary(
            p => p.Rank.ToString(),
            p => new
            {
                p.Name,
                p.CurrentGold,
                p.VipLevel,
                p.Level,
                p.AwardedGoldAmount
            });

        await PushToFirebase("final_exam_bai2_top3_vip_awards", vipPayload);
    }

    private static async Task<List<Player>> LoadPlayersFromGitHub()
    {
        var json = await httpClient.GetStringAsync(githubJsonUrl);
        return JsonSerializer.Deserialize<List<Player>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new List<Player>();
    }

    private static async Task PushToFirebase<T>(string path, T data)
    {
        string firebaseUrl = "https://fbtask-ba600-default-rtdb.asia-southeast1.firebasedatabase.app/";
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        var content = new StringContent(json);
        var url = $"{firebaseUrl}/{path}.json";

        var response = await httpClient.PutAsync(url, content);
        if (response.IsSuccessStatusCode)
            Console.WriteLine($"Data pushed to Firebase: {path}");
        else
            Console.WriteLine($"Failed to push to Firebase: {path}, Status: {response.StatusCode}");
    }
}