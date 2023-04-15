using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot;
using System.Security.Policy;
using Telegram.Bot.Types.Payments;
using System.Collections;
using Telegram.Bot.Types.Enums;
using System.Diagnostics;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Args;
using Telegram.Bot.Exceptions;
using System.Globalization;
namespace ChatGPT
{
    class Program
    {
        static ITelegramBotClient bot = new TelegramBotClient("YOU_TOKEN_FROM_TG_BOT");
        public static async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken cancellationToken)
        {
            bool tonometr = false;

            string endpoint = "https://api.openai.com/v1/chat/completions";
            
            List<Message> messages = new List<Message>();
            string apiKey = "YOU_TOKEN_FROM_OPEN_AI";
            var httpClient = new HttpClient();
            // устанавливаем отправляемый в запросе токен
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
            // вывод данных в консоль
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(update));
            var button1 = new KeyboardButton("отчет")
            {
                Text = "Запустить чат"
            };
            var keyboard = new ReplyKeyboardMarkup(new[] { new[] { button1 } })
            {
                ResizeKeyboard = true, // optional: set this property to true to resize the keyboard when the user sends a message
                OneTimeKeyboard = true // optional: set this property to true to hide the keyboard after the user sends a message
            };
            if (update.Type == Telegram.Bot.Types.Enums.UpdateType.Message)
            {
                var message = update.Message;
                if (update.Message.Type == MessageType.Text)
                {
                    //ГЛАВНАЯ СТРАНИЦА
                    if (message.Text.ToLower() == "/start")
                    {
                        await bot.SendTextMessageAsync(message.Chat, "Добро пожаловать в бот ChatGPT!\r 👋" + "\n" + "\n" +
                            "🤖 Я ваш проводник в мир ChatGPT" + "\n" + "\n" + "🔗 Ссылка на бота: https://t.me/chat4bot", replyMarkup: keyboard);
                        return;
                    }

                }
                if (update.Type == UpdateType.Message)
                {
                    if (update.Message.Type == MessageType.Text)
                    {

                        var text = update.Message.Text;
                        var id = update.Message.Chat.Id;
                        var username = update.Message.Chat.Username;
                        Console.WriteLine($"{id} | {text} | {username}");

                        if( text != null){
                            tonometr = true;
                        }
                        while (tonometr)
                        {
                            // ввод сообщения пользователя

                            var content = text;

                            // если введенное сообщение имеет длину меньше 1 символа
                            // то выходим из цикла и завершаем программу
                            if (content is not { Length: > 0 }) break;
                            // формируем отправляемое сообщение
                            var message1 = new Message() { Role = "user", Content = content };
                            // добавляем сообщение в список сообщений
                            messages.Add(message1);

                            // формируем отправляемые данные
                            var requestData = new Request()
                            {
                                ModelId = "gpt-3.5-turbo",
                                Messages = messages
                            };
                            // отправляем запрос
                            using var response = await httpClient.PostAsJsonAsync(endpoint, requestData);

                            // если произошла ошибка, выводим сообщение об ошибке на консоль
                            if (!response.IsSuccessStatusCode)
                            {
                                Console.WriteLine($"{(int)response.StatusCode} {response.StatusCode}");
                                break;
                            }
                            // получаем данные ответа
                            ResponseData? responseData = await response.Content.ReadFromJsonAsync<ResponseData>();

                            var choices = responseData?.Choices ?? new List<Choice>();
                            if (choices.Count == 0)
                            {
                                Console.WriteLine("No choices were returned by the API");
                                continue;
                            }
                            var choice = choices[0];
                            var responseMessage = choice.Message;
                            // добавляем полученное сообщение в список сообщений
                            messages.Add(responseMessage);
                            var responseText = responseMessage.Content.Trim();
                            await bot.SendTextMessageAsync(message.Chat, responseText);
                            Console.WriteLine(responseText.ToString());
                            tonometr = false;
                        }

                    }
                }
            }
        }
        // класс сообщения
        class Message
        {
            [JsonPropertyName("role")]
            public string Role { get; set; } = "";
            [JsonPropertyName("content")]
            public string Content { get; set; } = "";
        }
        class Request
        {
            [JsonPropertyName("model")]
            public string ModelId { get; set; } = "";
            [JsonPropertyName("messages")]
            public List<Message> Messages { get; set; } = new();
        }

        class ResponseData
        {
            [JsonPropertyName("id")]
            public string Id { get; set; } = "";
            [JsonPropertyName("object")]
            public string Object { get; set; } = "";
            [JsonPropertyName("created")]
            public ulong Created { get; set; }
            [JsonPropertyName("choices")]
            public List<Choice> Choices { get; set; } = new();
            [JsonPropertyName("usage")]
            public Usage Usage { get; set; } = new();
        }

        class Choice
        {
            [JsonPropertyName("index")]
            public int Index { get; set; }
            [JsonPropertyName("message")]
            public Message Message { get; set; } = new();
            [JsonPropertyName("finish_reason")]
            public string FinishReason { get; set; } = "";
        }

        class Usage
        {
            [JsonPropertyName("prompt_tokens")]
            public int PromptTokens { get; set; }
            [JsonPropertyName("completion_tokens")]
            public int CompletionTokens { get; set; }
            [JsonPropertyName("total_tokens")]
            public int TotalTokens { get; set; }
        }
        public static async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            // Некоторые действия
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(exception));
        }


        static void Main(string[] args)
        {

            Console.WriteLine("Запущен бот " + bot.GetMeAsync().Result.FirstName);
            var receiverMessage = new ReceiverOptions
            {
                AllowedUpdates = new UpdateType[] {
                    UpdateType.Message
                }
            };
            var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = { }, // receive all update types
            };
            bot.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                receiverOptions,
                cancellationToken
            );

            Console.ReadLine();
        }
    }
}

