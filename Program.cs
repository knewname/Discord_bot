using Discord;
using Discord.WebSocket;
using Discord.Interactions;
using System;
using System.Threading.Tasks;
using DotNetEnv;
using Microsoft.VisualBasic;


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
        await gameRegisterStorage.InitScheduleList(); // ← 중요!


        DotNetEnv.Env.Load(); // .env 파일 로드

        string token = Environment.GetEnvironmentVariable("DISCORD_TOKEN");

        if (string.IsNullOrWhiteSpace(token))
        {
            Console.WriteLine("❌ 토큰이 비어 있습니다!");
        }


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


        //AddRoleAsync(ulong, RequestOptions) 
        var message = await cacheableMessage.GetOrDownloadAsync();
        var channel = await cacheableChannel.GetOrDownloadAsync();
        var user = await channel.GetUserAsync(reaction.UserId);



        // 서버(Guild) ID 가져오기
        var guildId = (channel as SocketGuildChannel)?.Guild.Id;
        ulong serverId = 0;
        if (guildId.HasValue)
        {
            serverId = guildId.Value;
        }
        else
        {
            await message.ReplyAsync("서버 ID를 가져오지 못했습니다.");
        }

        Console.WriteLine($"{serverId}");


        // 파티 인원 추가
        if (reaction.Emote.Name == "🆗" && !user.IsBot && gameRegisterStorage.msgIdList.Contains(message.Id) && serverId != 0)
        {
            registerUserAdd(message, user, reaction, serverId);
        }
    }

    

    private async Task OnReactionRemovedAsync(Cacheable<IUserMessage, ulong> cacheableMessage,
                                          Cacheable<IMessageChannel, ulong> cacheableChannel,
                                          SocketReaction reaction)
    {
        var message = await cacheableMessage.GetOrDownloadAsync();
        var channel = await cacheableChannel.GetOrDownloadAsync();
        var user = await channel.GetUserAsync(reaction.UserId);

        // 서버(Guild) ID 가져오기
        var guildId = (channel as SocketGuildChannel)?.Guild.Id;
        ulong serverId = 0;
        if (guildId.HasValue)
        {
            serverId = guildId.Value;
        }
        else
        {
            await message.ReplyAsync("서버 ID를 가져오지 못했습니다.");
        }


        // 파티 인원 취소
        if (reaction.Emote.Name == "🆗"
            && !user.IsBot
            && gameRegisterStorage.msgIdList.Contains(message.Id)
            && serverId != 0)
        {
            registerUserRemove(message, reaction, serverId);
        }
    }


    // 파티 인원 추가 함수
    private async void registerUserAdd(IUserMessage message, IUser user, SocketReaction reaction, ulong serverId)
    {

        GameRegisterInfo info = gameRegisterStorage.SearchGameSchedule(reaction.MessageId);

        // 리액션 리스트에 있는 인원들로 참가자 파악
        var addEmoji = new Emoji("🆗");
        var userList = await message.GetReactionUsersAsync(addEmoji, info.max).FlattenAsync();

        info = await gameRegisterStorage.AddUser(reaction.MessageId, userList);



        // 정상적으로 추가 완료시 기존 메세지 변경 
        if (info != null)
            await EditGameRegisterMessage(message, info, serverId);

        else if (info == null)
        {
            await message.ReplyAsync($"{user} 님은 참여하실수 없습니다.");
            // 해당 리액션 제거
            await message.RemoveReactionAsync(reaction.Emote, user);
        }

    }

    // 파티 인원 취소 함수
    private async void registerUserRemove(IUserMessage message, SocketReaction reaction, ulong serverId)
    {

        GameRegisterInfo info = gameRegisterStorage.SearchGameSchedule(reaction.MessageId);

        // 리액션 리스트에 있는 인원들로 참가자 파악
        var addEmoji = new Emoji("🆗");
        var userList = await message.GetReactionUsersAsync(addEmoji, info.max).FlattenAsync();

        info = await gameRegisterStorage.RemoveUser(reaction.MessageId, userList);

        // 정상적으로 추가 완료시 기존 메세지 변경
        if (info != null)
            await EditGameRegisterMessage(message, info, serverId);

    }

    // json에서 저장된 데이터 기반으로 메세지 수정 
    public async Task EditGameRegisterMessage(IUserMessage msg, GameRegisterInfo info)
    {
        string users = "";
        foreach (ulong userId in info.users)
        {
            SocketUser userMention = _client.GetUser(userId);
            Console.WriteLine($"{userId} + {userMention.Id}");
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
    
    public async Task EditGameRegisterMessage(IUserMessage msg, GameRegisterInfo info, ulong servetId)
    {
        SocketGuild guild = _client.GetGuild(servetId);
        Program.gameRegisterStorage.EditGameRegisterMessage(msg, info, guild);
    }

    

}

public class SlashModule : InteractionModuleBase<SocketInteractionContext>
{

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

        if (max > 1)
        {
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
        else
            await RespondAsync("1명 이하는 설정하실수 없습니다.", ephemeral: true);



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
    public async Task EditParty(string id, string? date = null, string? time = null, string? game = null, int? max = null)
    {
        ulong msgId = ulong.Parse(id);
        var gameRegisterStorage = Program.gameRegisterStorage;
        GameRegisterInfo? gameRegisterInfo = gameRegisterStorage.SearchGameSchedule(msgId);

        if (gameRegisterInfo != null)
        {
            if (date != null)
                gameRegisterInfo.date = date;
            if (time != null)
                gameRegisterInfo.time = time;

            if (game != null)
                gameRegisterInfo.game = game;

            if (max != null)
            {
                if (max <= 1)
                    await RespondAsync("1명 이하는 설정하실수 없습니다.", ephemeral: true);
                else if (max < gameRegisterInfo.cur)
                    await RespondAsync("현재 참여인원보다 적은 수입니다.", ephemeral: true);
                else
                    gameRegisterInfo.max = (int)max;
            }

            await gameRegisterStorage.SaveAsync();


            // 메세지 ID 저장
            var msg = await Context.Channel.GetMessageAsync(msgId) as IUserMessage;
            var guild = (msg.Channel as SocketGuildChannel)?.Guild;         // 현재 채널로 서버 guild값을 가져옴
            await gameRegisterStorage.EditGameRegisterMessage(msg, gameRegisterInfo, guild);


            await RespondAsync("수정 완료 하였습니다.", ephemeral: true);
        }
        else
        {
            await RespondAsync("해당 ID는 존재하지 않습니다.", ephemeral: true);
        }

    }



    // [SlashCommand("역할부여등록", "역할부여할 메세지, 반응, 역할을 등록합니다.")]
    // public async Task RegRole(string msgId, SocketRole role, string emoji)
    // {
    // }
    
    

}


