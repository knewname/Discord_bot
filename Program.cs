using Discord;
using Discord.WebSocket;
using Discord.Interactions;
using System;
using System.Threading.Tasks;
class Program
{
    private DiscordSocketClient? _client;
    private InteractionService? _interactionService;
    public static GameRegisterStorage gameRegisterStorage { get; private set; }
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


        gameRegisterStorage = new GameRegisterStorage();

        string token = "MTM3NzI3NDMzMzU4MzY0MjcyNw.GS_FoI.qV_V8OH9QrKpI3Ebfl_Lk_O-B3fp4hOka6ZIR8";

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
        //ulong guildId = 1377521292194091121;
        //ulong erSerId = 1263418864067149904;


        await _interactionService.AddModulesAsync(System.Reflection.Assembly.GetEntryAssembly(), null);
        // 슬래시 명령어를 "전역" 등록 (→ 모든 서버에서 사용 가능)
        await _interactionService.RegisterCommandsGloballyAsync();
        //await _interactionService.RegisterCommandsToGuildAsync(1263418864067149904);

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

        Console.WriteLine($"{user.Id}");

        if (reaction.Emote.Name == "🆗" && !user.IsBot && gameRegisterStorage.msgIdList.Contains(message.Id))
        {
            GameRegisterInfo info = await gameRegisterStorage.AddUser(reaction.MessageId, reaction.UserId);
            // 정상적으로 추가 완료시 기존 메세지 변경 
            if (info != null)
                await EditGameRegisterMessage(message, info);


            else if (info == null)
            {
                await message.ReplyAsync($"{user} 님은 참여하실수 없습니다.");
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
        if (reaction.Emote.Name == "🆗" && !user.IsBot && gameRegisterStorage.msgIdList.Contains(message.Id))
        {
            GameRegisterInfo info = await gameRegisterStorage.RemoveUser(reaction.MessageId, reaction.UserId);
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
        }
    }

    // json에서 저장된 데이터 기반으로 메세지 수정 
    public async Task EditGameRegisterMessage(IUserMessage msg, GameRegisterInfo info)
    {

        string users = "";
        foreach (ulong userId in info.users)
        {
            SocketUser userMention = _client.GetUser(userId);
            Console.Write($"{userId}");
            users += $"{userMention.Mention} ";
        }

        Embed embed = new EmbedBuilder()
                    .WithTitle($"{info.game}")
                    .WithDescription($"ID : {info.id}\n모집인원수 : {info.cur}/{info.max}\n시간 : {info.date} {info.time}\n참여인원 : {users}")
                    .WithColor(Color.Blue)
                    .WithFooter(footer => footer.Text = "이리악귀들")
                    .WithTimestamp(DateTimeOffset.Now)
                    .Build();

        await msg.ModifyAsync(m => { m.Embed = embed; });

    }

}

public class SlashModule : InteractionModuleBase<SocketInteractionContext>
{
    // [SlashCommand("hello", "봇이 인사합니다.")]
    // public async Task Hello()
    // {
    //     await RespondAsync("부르셨나요?");
    // }

    // [SlashCommand("info", "봇 정보를 출력합니다.")]
    // public async Task Info()
    // {
    //     // 출력값을 임베드박스로 표현현
    //     var embed = new EmbedBuilder()
    //         .WithTitle("봇 정보")
    //         .WithDescription("이것은 예시 봇입니다.")
    //         .WithColor(Color.Blue)
    //         .WithFooter(footer => footer.Text = "Powered by Discord.Net")
    //         .WithTimestamp(DateTimeOffset.Now)
    //         .Build();

    //     await RespondAsync(embed: embed);
    // }

    // 기존 명령어 유지(party를 유지하여 사용자가 혼란오지 않게함)
    [SlashCommand("party", "파티원을 모집합니다.")]
    public async Task party(string date, string time, string game, int max)
    {
        await MakeParty(date, time, game, max);
    }


    [SlashCommand("파티모집", "파티원을 모집합니다.")]
    public async Task MakeParty(string date, string time, string game, int max)
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

        // 싱글톤으로 선언된 gameRegisterStorage 데이터를 가져옴
        var gameRegisterStorage = Program.gameRegisterStorage;
        // 명령어를 작성한 유저 데이터를 가져옴
        var user = Context.User;


        // 메세지 ID를 미리 받기 위한 선 입력메세지 
        // 게임 스케줄에 대한 고유값으로 메세지 ID값을 받기 때문에 메세지를 작성함으로 해당 ID값이 필요
        var embed = new EmbedBuilder()
                .WithTitle($"{game}")
                .WithDescription($"ID : [잠시 후 결정됨]\n모집인원수 : 1/{max}\n시간 : {date} {time}\n 참여인원 : {user.Username}")
                .WithColor(Color.Blue)
                .Build();
        await RespondAsync(embed: embed);

        // ID를 받기 위한 작성한 메세지에 대한 정보 가져오기
        var channel = Context.Channel as SocketTextChannel;
        var messages = await channel.GetMessagesAsync(1).FlattenAsync();
        var botMessage = messages.FirstOrDefault(msg => msg.Author.Id == Context.Client.CurrentUser.Id);
        if (botMessage != null)
        {
            // 메시지가 존재하면 이모지 반응 추가
            await botMessage.AddReactionAsync(new Emoji("🆗"));

            // 메세지 ID 저장
            ulong messageId = botMessage.Id;
            var msg = await Context.Channel.GetMessageAsync(messageId) as IUserMessage;

            // 스케쥴 고유값을 list에 add
            gameRegisterStorage.msgIdList.Add(messageId);

            // embed 포멧 실제 포멧으로 수정정
            embed = new EmbedBuilder()
                  .WithTitle($"{game}")
                  .WithDescription($"ID : {messageId}\n모집인원수 : 1/{max}\n시간 : {date} {time}\n 참여인원 : {user.Mention}")
                  .WithColor(Color.Blue)
                  .WithFooter(footer => footer.Text = "이리악귀들")
                  .WithTimestamp(DateTimeOffset.Now)
                  .Build();

            // 실제 포멧으로 수정한 데이터로 수정 
            await msg.ModifyAsync(m => { m.Embed = embed; });

            // 예약된 스케줄을 저장
            await gameRegisterStorage.RegisterSchedule(
                messageId,
                date,
                time,
                game,
                user.Id,
                max
            );


        }


    }


    [SlashCommand("파티삭제", "파티모집을 삭제합니다")]
    public async Task RemoveParty(string id)
    {
        ulong msgId = ulong.Parse(id);
        var gameRegisterStorage = Program.gameRegisterStorage;
        int errorCode = await gameRegisterStorage.RemoveSchedule(msgId, Context.User.Id);
        if (errorCode == 0)
        {
            var msg = await Context.Channel.GetMessageAsync(msgId) as IUserMessage;
            await msg.DeleteAsync();
            await RespondAsync("정상적으로 삭제되었습니다.", ephemeral: true);
        }
        else if (errorCode == 1)
            await RespondAsync("ID값은 19자리의 숫자값이여야합니다.", ephemeral: true);
        else if (errorCode == 2)
            await RespondAsync("해당 ID값을 찾을수 없습니다.", ephemeral: true);
        else if (errorCode == 3)
            await RespondAsync("등록자만이 삭제할수 있습니다.", ephemeral: true);


    }



    [SlashCommand("파티수정", "파티의 정보를 수정합니다.")]
    public async Task EditParty(string id, string date = null, string time = null, string game = null, int? max = null)
    {
        ulong msgId = ulong.Parse(id);
        var gameRegisterStorage = Program.gameRegisterStorage;
        GameRegisterInfo gameRegisterInfo = gameRegisterStorage.SearchGameSchedule(msgId);
        if (date != null)
        {
            
        }

        
    }
    
    

}


