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
using Microsoft.VisualBasic;

public class GameRegisterInfo
{
    public ulong id { get; set; }         // messageId
    public string date { get; set; }       // 등록날짜 (예: "2025-06-09")
    public string time { get; set; }       // 할 시간 (예: "21:00")
    public string game { get; set; }       // 게임 + 모드 (예: "LOL-Normal")
    public List<ulong> users { get; set; } = new();       // 등록한 유저 이름 또는 ID
    public int cur { get; set; }           // 현재 등록된 명수
    public int max { get; set; }           // 최대 인원수
}
public class GameRegisterStorage
{
    private readonly string _filePath;
    private List<GameRegisterInfo> regisrerList = new List<GameRegisterInfo>();
    public List<ulong> msgIdList = new List<ulong>();

    public GameRegisterStorage(string filePath = "game_register.json")
    {
        _filePath = filePath;
        InitScheduleList();
    }

    public async void InitScheduleList()
    {
        await LoadAsync();
        msgIdList = await LoadMsgIdList(regisrerList);
        foreach (ulong a in msgIdList)
            Console.Write($"{a}\n");
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
    public async Task LoadAsync()
    {
        if (!File.Exists(_filePath))
        {
            regisrerList = new List<GameRegisterInfo>();
            return;
        }

        var json = await File.ReadAllTextAsync(_filePath);

        if (string.IsNullOrWhiteSpace(json))
        {
            regisrerList = new List<GameRegisterInfo>();
            return;
        }

        regisrerList = JsonSerializer.Deserialize<List<GameRegisterInfo>>(json)
                    ?? new List<GameRegisterInfo>();
    }


    public async Task<List<ulong>> LoadMsgIdList(List<GameRegisterInfo> list)
    {

        List<ulong> msgIds = new List<ulong>();

        foreach (var info in list)
            msgIds.Add(info.id);

        return msgIds;
    }


    // 인원 추가시 기존 json 파일 수정
    public async Task<GameRegisterInfo> AddUser(ulong msgId, ulong userId)
    {
        GameRegisterInfo gameRegister = SearchGameSchedule(msgId);
        Console.Write($"{gameRegister.id}\n");

        // 이미 참가중일 때
        if (gameRegister.users.Contains(userId))
            return null;

        // 이미 인원수가 가장 찼을 때      
        if (gameRegister.cur >= gameRegister.max)
            return null;

        gameRegister.users.Add(userId);
        gameRegister.cur++;

        await SaveAsync(regisrerList);

        return gameRegister;

    }

    public async Task<GameRegisterInfo> RemoveUser(ulong msgId, ulong userId)
    {
        GameRegisterInfo gameRegister = SearchGameSchedule(msgId);

        // users에 있다면 유저 삭제
        if (gameRegister.users.Contains(userId))
        {
            gameRegister.users.Remove(userId);
            gameRegister.cur--;
        }
        // 수정된 내용 수정 후 저장
        await SaveAsync(regisrerList);

        return gameRegister;
        
    }


    public async Task<int> RemoveSchedule(string msgId)
    {
        try
        {
            ulong msgIDLong = ulong.Parse(msgId);
            GameRegisterInfo info = SearchGameSchedule(msgIDLong);
            if (info == null)
            {
                Console.WriteLine("❌ 해당 ID의 스케줄을 찾을 수 없습니다.");
                return 3; // 해당 없음
            }

            regisrerList.Remove(info); // 리스트에서 제거

            await SaveAsync(regisrerList); // JSON 파일에도 반영 (동기 처리)

            Console.WriteLine($"✅ 스케줄 삭제 완료: {msgId}");
            return 0; // 성공
        }
        catch (FormatException)
        {
            Console.WriteLine("❌ 형식 오류: 숫자가 아님");
            return 1;   // 오류 코드 리턴 
        }
        catch (OverflowException)
        {
            Console.WriteLine("❌ 범위 오류: ulong 범위를 초과함");
            return 2;   // 오류 코드 리턴 
        }

    }



    public GameRegisterInfo SearchGameSchedule(ulong msgId)
    {


        foreach (var gameRegister in regisrerList)
        {
            if (gameRegister.id == msgId)
                return gameRegister;
        }

        return null;
    }



    /*
    public async Task<List<GameRegisterInfo>> EditAsync(string column, string change)
    {
        if (column)
            return new List<GameRegisterInfo>;
    }
    */



    // 등록된 스케줄 json 저장
    public async Task RegisterSchedule(ulong msgId, string date, string time, string game, ulong regUser, int max)
    {
        try
        {

            if (regisrerList.Any(entry => entry.id == msgId))
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

            regisrerList.Add(newEntry);
            await SaveAsync(regisrerList);

            Console.WriteLine("저장 완료!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RegisterSchedule 오류] {ex.Message}");
        }
    }



}