using Discord;
using Discord.WebSocket;
using Discord.Interactions;
using System;
using System.Threading.Tasks;
class Program
{
    private DiscordSocketClient _client;
    private InteractionService _interactionService;

    public static Task Main(string[] args) => new Program().MainAsync();

    public async Task MainAsync()
{
    _client = new DiscordSocketClient();
    _interactionService = new InteractionService(_client.Rest); // ✅ 추가

    _client.Log += Log;
    _client.MessageReceived += MessageReceivedAsync;
    _client.Ready += ReadyAsync;
    _client.InteractionCreated += HandleInteraction;

    string token = "MTM3NzI3NDMzMzU4MzY0MjcyNw.GDgukg.AeTbdPJeGy8qNkQH93cuw326OujUd2K27toM7Y";

    await _client.LoginAsync(TokenType.Bot, token);
    await _client.StartAsync();

    await Task.Delay(-1);
}


    private Task Log(LogMessage msg)
    {
        Console.WriteLine(msg.ToString());
        return Task.CompletedTask;
    }

    private async Task MessageReceivedAsync(SocketMessage message)
    {
        if (message.Content == "!hello")
            await message.Channel.SendMessageAsync("Hello, world!");
    }

   private async Task ReadyAsync()
    {
        ulong guildId = 1377521292194091121;
        await _interactionService.AddModulesAsync(System.Reflection.Assembly.GetEntryAssembly(), null);
        await _interactionService.RegisterCommandsToGuildAsync(guildId); // 전역 대신 이걸로 개발 시 빠르게 반영
        Console.WriteLine("슬래시 명령어 등록 완료");
    }

    private async Task HandleInteraction(SocketInteraction interaction)
    {
        var ctx = new SocketInteractionContext(_client, interaction);
        await _interactionService.ExecuteCommandAsync(ctx, null);
    }
}

public class SlashModule : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("hello", "봇이 인사합니다.")]
    public async Task Hello()
    {
        await RespondAsync("안녕하세요! 저는 봇입니다.");
    }

    [SlashCommand("info", "봇 정보를 출력합니다.")]
    public async Task Info()
    {
        var embed = new EmbedBuilder()
            .WithTitle("봇 정보")
            .WithDescription("이것은 예시 봇입니다.")
            .WithColor(Color.Blue)
            .WithFooter(footer => footer.Text = "Powered by Discord.Net")
            .WithTimestamp(DateTimeOffset.Now)
            .Build();

        await RespondAsync(embed: embed);
    }

    [SlashCommand("get", "입력값을 받습니다.")]
    public async Task Get(String str)
    {
        await RespondAsync(str);   
    }
}