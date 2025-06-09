using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Interactions;

public class GameRegisterInfo
{
    public string id { get; set; }         // messageId
    public string date { get; set; }       // 등록날짜 (예: "2025-06-09")
    public string time { get; set; }       // 할 시간 (예: "21:00")
    public string game { get; set; }       // 게임 + 모드 (예: "LOL-Normal")
    public string user { get; set; }       // 등록한 유저 이름 또는 ID
    public int cur { get; set; }           // 현재 등록된 명수
    public int max { get; set; }           // 최대 인원수
}
public class GameRegisterStorage
{
    private readonly string _filePath;

    public GameRegisterStorage(string filePath = "game_register.json")
    {
        _filePath = filePath;
    }

    public async Task SaveAsync(List<GameRegisterInfo> list)
    {
        var json = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_filePath, json);
    }

   public async Task<List<GameRegisterInfo>> LoadAsync()
    {
        if (!File.Exists(_filePath))
            return new List<GameRegisterInfo>();

        var json = await File.ReadAllTextAsync(_filePath);

        // ✅ 파일이 비어 있는 경우 방어
        if (string.IsNullOrWhiteSpace(json))
            return new List<GameRegisterInfo>();

        return JsonSerializer.Deserialize<List<GameRegisterInfo>>(json)
            ?? new List<GameRegisterInfo>();
    }


    public async Task RegisterSchedule(string msgId, string date, string time, string game, string regUser, int max)
    {
        try
        {
            var list = await LoadAsync();

            if (list.Any(entry => entry.id == msgId))
            {
                Console.WriteLine("이미 등록된 메시지 ID입니다. 추가하지 않습니다.");
                return;
            }

            var newEntry = new GameRegisterInfo
            {
                id = msgId,
                date = date,
                time = time,
                game = game,
                user = regUser,
                cur = 1,
                max = max
            };

            list.Add(newEntry);
            await SaveAsync(list);

            Console.WriteLine("저장 완료!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RegisterSchedule 오류] {ex.Message}");
        }
    }



}