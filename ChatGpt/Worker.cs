using ChatGpt.Models;
using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Message = Telegram.Bot.Types.Message;

namespace ChatGpt;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var botClient = new TelegramBotClient("5807966733:AAHQWANZTzZFXkGEa0zBzVTg-UV6cxzAtko");

        using CancellationTokenSource cts = new();

        // StartReceiving does not block the caller thread. Receiving is done on the ThreadPool.
        ReceiverOptions receiverOptions = new()
        {
            AllowedUpdates = Array.Empty<UpdateType>() // receive all update types
        };

        botClient.StartReceiving(
            updateHandler: HandleUpdateAsync,
            pollingErrorHandler: HandlePollingErrorAsync,
            receiverOptions: receiverOptions,    
            cancellationToken: cts.Token
        );

        var me = await botClient.GetMeAsync();

        Console.WriteLine($"Start listening for @{me.Username}");
        Console.ReadLine();

        // Send cancellation request to stop bot
        cts.Cancel();
    }

    private async Task<string?> ChatGpt(string content)
    {
        var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
        request.Headers.Add("Authorization", "Bearer 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");
        var d = $@"{{""model"":""gpt-3.5-turbo"",""messages"":[{{""role"":""user"",""content"":""{content}""}}]}}";
        var body = new StringContent(d,null, "application/json");
        request.Content = body;
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
       var json =await response.Content.ReadAsStringAsync();

        if (string.IsNullOrWhiteSpace(json))
            return "";

        ChatGptModel? data = JsonConvert.DeserializeObject<ChatGptModel>(json);

        return data?.choices[0]?.message?.content;
    }

    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            // Only process Message updates: https://core.telegram.org/bots/api#message
            if (update.Message is not { } message)
                return;
            // Only process text messages
            if (message.Text is not { } messageText)
                return;

            var chatId = message.Chat.Id;

            Console.WriteLine($"Received a '{messageText}' message in chat {chatId}.");

        var responseChat = await ChatGpt(messageText);

        if (string.IsNullOrWhiteSpace(responseChat)) return;
            // Echo received message text
            Message sentMessage = await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: responseChat,
                replyToMessageId: message.MessageId,
                cancellationToken: cancellationToken);
        }

        Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }
    
}
