using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Interactions;
using System.Collections;

public class GameRegisterInfo
{
    public string id { get; set; }         // messageId
    public string date { get; set; }       // 등록날짜 (예: "2025-06-09")
    public string time { get; set; }       // 할 시간 (예: "21:00")
    public string game { get; set; }       // 게임 + 모드 (예: "LOL-Normal")
    public List<string> users { get; set; } = new();       // 등록한 유저 이름 또는 ID
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

    // json 파일 저장 
    public async Task SaveAsync(List<GameRegisterInfo> list)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        var json = JsonSerializer.Serialize(list, options);
        await File.WriteAllTextAsync(_filePath, json);
    }

    // json 데이터 가져오기 
    public async Task<List<GameRegisterInfo>> LoadAsync()
    {
        // 경로에 json이 없다면 생성 
        if (!File.Exists(_filePath))
            return new List<GameRegisterInfo>();

        // json 읽어오기기
        var json = await File.ReadAllTextAsync(_filePath);

        // 파일이 비어 있는 경우 방어
        if (string.IsNullOrWhiteSpace(json))
            return new List<GameRegisterInfo>();


        return JsonSerializer.Deserialize<List<GameRegisterInfo>>(json)
            ?? new List<GameRegisterInfo>();
    }


    // 인원 추가시 기존 json 파일 수정
    public async Task<bool> AddUser(string msgId, string userName)
    {
        var list = await LoadAsync();

        foreach (var gameRegister in list)
        {
            if (gameRegister.id == msgId)
            {
                if (gameRegister.users.Contains(userName))
                    return false;

                if (gameRegister.cur >= gameRegister.max)
                    return false;

                gameRegister.users.Add(userName);
                gameRegister.cur++;

                await SaveAsync(list);
                return true;
            }
        }

        return false;
    }



    /*
    public async Task<List<GameRegisterInfo>> EditAsync(string column, string change)
    {
        if (column)
            return new List<GameRegisterInfo>;
    }
    */



    // 등록된 스케줄 json 저장
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
                cur = 1,
                max = max
            };
            newEntry.users.Add(regUser);

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