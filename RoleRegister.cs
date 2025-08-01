using System;
using Discord;
using Discord.WebSocket;
using System.Text.Json;
public class RoleRegisterInfo
{
    public ulong serverId;      // 서버
    public string? emojiCode;    // 역할을 부여하기 위한 이모지
    public ulong msgId;         // 역할을 줄 메세지의 정보
    public ulong roleId;        // 무슨 역할을 부여할지

}

// 역할 부여 메세지 및 이모지 관리 및 부여

public class ManageRoleGrant
{
    private const string FilePath = "role_register.json";
    private List<RoleRegisterInfo> registerList = new();

    public ManageRoleGrant()
    {
        LoadFromFile().Wait();
    }

    // 역할 부여 정보 저장
    public async Task RegisterRoleGrant(ulong serverId, ulong msgId, string emojiCode, ulong roleId)
    {
        var entry = new RoleRegisterInfo
        {
            serverId = serverId,
            msgId = msgId,
            emojiCode = emojiCode,
            roleId = roleId
        };

        registerList.Add(entry);
        await SaveToFile();
    }

    // 리액션이 발생했을 때 역할 부여
    public async Task GrantRole(SocketReaction reaction, DiscordSocketClient client)
    {
        var entry = registerList.FirstOrDefault(r =>
            r.msgId == reaction.MessageId &&
            r.emojiCode == reaction.Emote.Name &&
            (reaction.Channel as SocketGuildChannel)?.Guild.Id == r.serverId);

        if (entry == null)
            return;

        var guild = client.GetGuild(entry.serverId);
        var role = guild?.GetRole(entry.roleId);
        var user = guild?.GetUser(reaction.UserId);

        if (role != null && user != null && !user.Roles.Contains(role))
        {
            await user.AddRoleAsync(role);
        }
    }

    // 리액션 제거시 역할 제거
    public async Task RemoveRole(SocketReaction reaction, DiscordSocketClient client)
    {
        var entry = registerList.FirstOrDefault(r =>
            r.msgId == reaction.MessageId &&
            r.emojiCode == reaction.Emote.Name &&
            (reaction.Channel as SocketGuildChannel)?.Guild.Id == r.serverId);

        if (entry == null)
            return;

        var guild = client.GetGuild(entry.serverId);
        var role = guild?.GetRole(entry.roleId);
        var user = guild?.GetUser(reaction.UserId);

        if (role != null && user != null && user.Roles.Contains(role))
        {
            await user.RemoveRoleAsync(role);
        }
    }

    private async Task SaveToFile()
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        string json = JsonSerializer.Serialize(registerList, options);
        await File.WriteAllTextAsync(FilePath, json);
    }

    private async Task LoadFromFile()
    {
        if (!File.Exists(FilePath)) return;

        var json = await File.ReadAllTextAsync(FilePath);
        registerList = JsonSerializer.Deserialize<List<RoleRegisterInfo>>(json) ?? new List<RoleRegisterInfo>();
    }
}


//public Task AddRolesAsync(IEnumerable<IRole> roles, RequestOptions options = null)