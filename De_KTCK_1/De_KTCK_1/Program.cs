using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;

public class Player
{
    public string Name { get; set; }
    public bool IsActive { get; set; }
    public DateTime LastLogin { get; set; }
    public int Level { get; set; }
    public int VipLevel { get; set; }
    public int Gold { get; set; }
}

public class Program
{
    private static readonly DateTime referenceTime = new DateTime(2025, 6, 30, 0, 0, 0, DateTimeKind.Utc);
    private static readonly HttpClient httpClient = new HttpClient();
    private static readonly string githubJsonUrl = "https://raw.githubusercontent.com/NTH-VTC/OnlineDemoC-/refs/heads/main/lab12_players.json";

    public static async Task Main(string[] args)
    {
        var players = await LoadPlayersFromGitHub();

        var filteredInactive = players
            .Where(p => p.IsActive == false)
            .OrderBy(p => p.LastLogin) // optional, for cleaner output
            .ToList();

        Console.WriteLine("--- 1.1 DANH SACH NGUOI CHOI KHONG HOAT DONG GAN DAY ---");
        Console.WriteLine("-------------------------------------------");
        Console.WriteLine("Ten Nguoi Choi | Hoat Dong | Dang Nhap Cuoi");
        Console.WriteLine("-------------------------------------------");

        foreach (var p in filteredInactive)
        {
            Console.WriteLine($"{p.Name,-15}| {p.IsActive,-10}| {p.LastLogin:dd/MM/yyyy HH:mm:ss}Z");
        }
        var inactiveData = filteredInactive
        .Select((p, i) => new
        {
            Key = (i + 1).ToString(),
            Player = new
            {
                p.Name,
                p.IsActive,
                LastLogin = p.LastLogin.ToString("o")
            }
        })
        .ToDictionary(x => x.Key, x => x.Player);

        await PushToFirebase("final_exam_bai1_1_inactive_players", inactiveData);


        var lowLevelPlayers = players
            .Where(p => p.Level < 10)
            .OrderBy(p => p.Name)
            .ToList();

        Console.WriteLine("\n--- 1.2. DANH SACH NGUOI CHOI CAP THAP ---");
        Console.WriteLine("-----------------------------------------");
        Console.WriteLine("| Ten Nguoi Choi | Level | Gold Hien Tai |");
        Console.WriteLine("-----------------------------------------");

        foreach (var p in lowLevelPlayers)
        {
            Console.WriteLine($"| {p.Name,-15}| {p.Level,-6}| {p.Gold,14:N0} |");
        }

        Console.WriteLine("-----------------------------------------");

        var lowLevelData = lowLevelPlayers
            .Select((p, i) => new
            {
                Key = (i + 1).ToString(),
                Player = new
                {
                    p.Name,
                    p.Level,
                    CurrentGold = p.Gold
                }
            })
            .ToDictionary(x => x.Key, x => x.Player);

        await PushToFirebase("final_exam_bai1_2_low_level_players", lowLevelData);
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
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        var content = new StringContent(json);
        var url = $"https://fbtask-ba600-default-rtdb.asia-southeast1.firebasedatabase.app/{path}.json";

        var response = await httpClient.PutAsync(url, content);
        if (response.IsSuccessStatusCode)
            Console.WriteLine($"Data pushed to Firebase: {path}");
        else
            Console.WriteLine($"Failed to push to Firebase: {path}, Status: {response.StatusCode}");
    }
}