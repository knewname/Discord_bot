using Discord;
using Discord.WebSocket;
using Discord.Interactions;
using System;
using System.Threading.Tasks;
class Program
{
    private DiscordSocketClient? _client;
    private InteractionService? _interactionService;
    private GameRegisterStorage? gameRegister;
    public static Task Main(string[] args) => new Program().MainAsync();

    public async Task MainAsync()
    {
        _client = new DiscordSocketClient();
        _interactionService = new InteractionService(_client.Rest); // ✅ 추가

        _client.Log += Log;
        _client.MessageReceived += MessageReceivedAsync;
        _client.Ready += ReadyAsync;
        _client.InteractionCreated += HandleInteraction;
        _client.ReactionAdded += OnReactionAddedAsync;
        _client.ReactionRemoved += OnReactionRemovedAsync;


        gameRegister = new GameRegisterStorage();

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

    private async Task OnReactionAddedAsync(Cacheable<IUserMessage, ulong> cacheableMessage,
                                        Cacheable<IMessageChannel, ulong> cacheableChannel,
                                        SocketReaction reaction)
    {

        var message = await cacheableMessage.GetOrDownloadAsync();
        var channel = await cacheableChannel.GetOrDownloadAsync();
        var user = await channel.GetUserAsync(reaction.UserId);
        Console.Write("asdf\n");


        if (reaction.Emote.Name == "🆗")
        {
            GameRegisterInfo info = await gameRegister.AddUser(reaction.MessageId, reaction.UserId);
            // 정상적으로 추가 완료시 기존 메세지 변경 
            if (info != null)
                await EditGameRegisterMessage(message, info);

            else if (info == null && !user.IsBot)
            {
                await channel.SendMessageAsync($"{user} 님은 참여하실수 없습니다.");
                // 해당 리액션 제거
                await message.RemoveReactionAsync(reaction.Emote, user);
            }
        }
    }

    private async Task OnReactionRemovedAsync(Cacheable<IUserMessage, ulong> cacheableMessage,
                                          Cacheable<IMessageChannel, ulong> cacheableChannel,
                                          SocketReaction reaction)
    {
        var message = await cacheableMessage.GetOrDownloadAsync();
        var channel = await cacheableChannel.GetOrDownloadAsync();
        var user = await channel.GetUserAsync(reaction.UserId);

        //Console.WriteLine($"❌ {reaction.UserId} 님이 {reaction.Emote.Name} 리액션을 제거했습니다.");

        // 예시: 특정 이모지 감지
        if (reaction.Emote.Name == "🆗" && gameRegister.msgIdList.Contains(message.Id))
        {
            GameRegisterInfo info = await gameRegister.RemoveUser(reaction.MessageId, reaction.UserId);
            // 정상적으로 추가 완료시 기존 메세지 변경
            if (info != null)
            {
                string users = "";
                foreach (ulong userId in info.users)
                {
                    SocketUser userMention = _client.GetUser(userId);
                    users += $"{userMention.Mention} ";
                }

                await EditGameRegisterMessage(message, info);

            }
            else if (info == null && !user.IsBot)
            {
                await channel.SendMessageAsync($"{user} 님은 참여하실수 없습니다.");
                // 해당 리액션 제거
                await message.RemoveReactionAsync(reaction.Emote, user);
            }
        }
    }

    // json에서 저장된 데이터 기반으로 메세지 수정 
    public async Task EditGameRegisterMessage(IUserMessage msg, GameRegisterInfo info)
    {

        string users = "";
        foreach (ulong userId in info.users)
        {
            SocketUser userMention = _client.GetUser(userId);
            users += $"{userMention.Mention} ";
        }

        Embed embed = new EmbedBuilder()
                    .WithTitle($"{info.game}")
                    .WithDescription($"ID : {info.id}\n모집인원수 : {info.cur}/{info.max}\n시간 : {info.date} {info.time}\n참여인원 : {users}")
                    .WithColor(Color.Blue)
                    .WithFooter(footer => footer.Text = "Powered by Discord.Net")
                    .WithTimestamp(DateTimeOffset.Now)
                    .Build();

        await msg.ModifyAsync(m => { m.Embed = embed; });

    }

}

public class SlashModule : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("hello", "봇이 인사합니다.")]
    public async Task Hello()
    {
        await RespondAsync("부르셨나요?");
    }

    [SlashCommand("info", "봇 정보를 출력합니다.")]
    public async Task Info()
    {
        // 출력값을 임베드박스로 표현현
        var embed = new EmbedBuilder()
            .WithTitle("봇 정보")
            .WithDescription("이것은 예시 봇입니다.")
            .WithColor(Color.Blue)
            .WithFooter(footer => footer.Text = "Powered by Discord.Net")
            .WithTimestamp(DateTimeOffset.Now)
            .Build();

        await RespondAsync(embed: embed);
    }

    [SlashCommand("party", "파티원을 모집합니다.")]
    public async Task Get(string date, string time, string game, int max)
    {
        // ┌────────────────────┬─────────────────────────────────────────────┐
        // │   Slash Command    │            C# (Discord.Net) Type            │
        // ├────────────────────┼─────────────────────────────────────────────┤
        // │ SubCommand         │ N/A (Used to group commands)                │
        // │ SubCommandGroup    │ N/A (Used to group subcommands)             │
        // │ String             │ string                                      │
        // │ Integer            │ int                                         │
        // │ Boolean            │ bool                                        │
        // │ User               │ SocketGuildUser or SocketUser               │
        // │ Role               │ SocketRole                                  │
        // │ Channel            │ SocketChannel                               │
        // │ Mentionable        │ SocketUser, SocketGuildUser, or SocketRole  │
        // │ File               │ IAttachment                                 │
        // └────────────────────┴─────────────────────────────────────────────┘

        /* $"{user.Mention}" > 유저 멘션*/
        var storage = new GameRegisterStorage();
        var user = Context.User;


        // 메세지 ID를 미리 받기 위한 선 입력메세지 
        var embed = new EmbedBuilder()
                .WithTitle($"{game}")
                .WithDescription($"ID : [잠시 후 결정됨]\n모집인원수 : 1/{max}\n시간 : {date} {time}\n 참여인원 : {user.Username}")
                .WithColor(Color.Blue)
                .Build();
        await RespondAsync(embed: embed);

        // 메세지 ID 저장
        var channel = Context.Channel as SocketTextChannel;
        var messages = await channel.GetMessagesAsync(1).FlattenAsync();
        var botMessage = messages.FirstOrDefault(msg => msg.Author.Id == Context.Client.CurrentUser.Id);
        if (botMessage != null)
        {
            // 메시지가 존재하면 이모지 반응 추가
            await botMessage.AddReactionAsync(new Emoji("🆗"));

            ulong messageId = botMessage.Id;
            var msg = await Context.Channel.GetMessageAsync(messageId) as IUserMessage;
            
            // embed 포멧 실제 포멧으로 수정정
            embed = new EmbedBuilder()
                  .WithTitle($"{game}")
                  .WithDescription($"ID : {messageId}\n모집인원수 : 1/{max}\n시간 : {date} {time}\n 참여인원 : {user.Mention}")
                  .WithColor(Color.Blue)
                  .WithFooter(footer => footer.Text = "Powered by Discord.Net")
                  .WithTimestamp(DateTimeOffset.Now)
                  .Build();

            // msg 수정정
            await msg.ModifyAsync(m => { m.Embed = embed; });

            await storage.RegisterSchedule(
                messageId,  // ulong → string
                date,
                time,
                game,
                user.Id,     
                max
            );
            
            
        }


    }
    
    

}


