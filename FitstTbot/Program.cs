using Microsoft.AspNetCore.Components.Forms;
using Newtonsoft.Json;
using Telegram.Bot.Polling;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using File = System.IO.File;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Microsoft.VisualBasic;
using Telegram.Bot.Types.ReplyMarkups;
using Newtonsoft.Json.Linq;

namespace FitstTbot
{
    internal class Program
    {
        private static string TOKEN = @"7156760094:AAHyH0jUPALdwCaR3DLRcG7IsC0zntEiXrI";
       

        //начало работы
        static void Main()
        {
            Console.WriteLine("Hello, World!");
            TBot.Start(TOKEN);
            Console.ReadLine();
        }
    }

    internal class TBot
    {
        public static ITelegramBotClient bot; 
        private static State State;

        #region Hide
        public static void Start(string token)
        {
            var path = Environment.CurrentDirectory + @"\state.json";
            var jsstr = File.ReadAllText(path);
            State = JsonConvert.DeserializeObject<State>(jsstr) ?? new State();
            bot = new TelegramBotClient(token);
            var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = { },
            };

            bot.StartReceiving(
                (botClient, update, cancellationToken1) => HandleUpdateAsync(botClient, update, cancellationToken1),
                HandleErrorAsync, receiverOptions, cancellationToken);

        }
        private static Task HandleErrorAsync(ITelegramBotClient botClient, Exception ex, HandleErrorSource errorSource, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
        private static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            try
            {
                await Task.Run(() =>
                {
                    ParseTextMessage(update);
                    ParseCallbackData(update);



                });
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        private static void ParseCallbackData(Update update)
        {
            var query = update.CallbackQuery;

            if(query == null) return;

            var data = query.Data;
            if(data == null) return;

            var chatId = new ChatId(query.Message.Chat.Id);
            var user = State.Users.FirstOrDefault(x => x.Id == chatId.Identifier) ?? new TUser();

            user.Id = chatId.Identifier ?? -1;
            NewCallData(user, data);
        }
        private static void ParseTextMessage(Update update)
        {
            //check
            var message = update.Message;
            if (message == null) return;

            //check
            var text = message.Text;
            if (text == null) return;

            var chatId = new ChatId(message.Chat.Id);
            var user = State.Users.FirstOrDefault(x => x.Id == chatId.Identifier) ?? new TUser();

            user.Id = chatId.Identifier ?? -1;

            user.FirstName = message.Chat.FirstName ?? "Anon";
            user.LastName = message.Chat.LastName ?? "";

            NewMessage(user, text);
        }

        #endregion

        private static void NewCallData(TUser user, string data)
        {
            if (data == "pro")
            {
                if (user.Balance > 5)
                {
                    user.Role = Role.Pro;
                    user.ProStart = DateTime.Now;
                    State.Save();
                }
            }
        }
        private static void NewMessage(TUser user, string message)
        {
            string? text = null;
            ReplyKeyboardMarkup? keyboard = null;
            InlineKeyboardMarkup? inline = null;
            #region Sign Up

            if (message == "/start" && user.Role == Role.New)
            {
                State.NewUser(user);
                keyboard = new ReplyKeyboardMarkup(new[] { new KeyboardButton[] { "Инструкция", "Я уже знаю" } }) { ResizeKeyboard = true };

                text = $"Привет! Я — VPN-бот." +
                       $"\n\n" +
                       $"Я рад, что вы решили воспользоваться моими услугами. Я помогу вам подключиться к выбранному VPN-серверу и получить доступ к интернету через защищённое соединение." +
                       $"\n\n" +
                       $"Чтобы начать работу со мной, просто выберите сервер из списка доступных и нажмите кнопку подключения. После этого вы сможете пользоваться интернетом без ограничений и с полной конфиденциальностью." +
                       $"\n\n" +
                       $"Если у вас возникнут вопросы или проблемы, пожалуйста, не стесняйтесь обращаться ко мне за помощью. Я всегда готов помочь!";
            }

            if (message == "Инструкция")
            {
                keyboard = new ReplyKeyboardMarkup(new[] { new KeyboardButton[] { "Я прочитал" } }) { ResizeKeyboard = true };

                text = $"Инструкция: ссылка";
            }

            if (message == "Я уже знаю" || message == "Я прочитал")
            {
                keyboard = new ReplyKeyboardMarkup(new[] { new KeyboardButton[] { "Аккаунт", "Управление" }, new KeyboardButton[] { "Поддержка", "Инструкция" } }) { ResizeKeyboard = true };

                text = $"Главное меню";

                user.Role = Role.Standart;
            }


            #endregion

            #region Menu
            if (message == "Аккаунт" && user.Role != Role.New)
            {
                inline = new InlineKeyboardMarkup(new[]
                {
                    new [] // first row
                    {
                        InlineKeyboardButton.WithCallbackData("Подписка Pro", "pro"),
                    }
                });

                text = $"Имя: {user.FirstName}\n" +
                       $"Баланс: {user.Balance:0.00} RUB\n" +
                       $"Подписка: {user.Role:G}";

                bot.SendMessage(user.Id, text, replyMarkup: inline);
                return;
            }

            if (message == "Управление" && user.Role != Role.New)
            {
                inline = new InlineKeyboardMarkup(new[]
                {
                    new [] // first row
                    {
                        InlineKeyboardButton.WithCallbackData("Включить Pro", "pro"),
                    }
                });
                var minutes = user.BalanceToTime();
                var days = (minutes / 60) / 24;
                var hour = (days % 1) * 24;
                var minute = (hour % 1) * 60;


                if (user.Role == Role.Pro)
                {
                    text = $"Тариф: <b>{user.Role:G}</b>\n" +
                           $"Баланс: {user.Balance:0.00} RUB\n\nХватит на <b>{(int)days}D {(int)hour}H {(int)minute}m</b>";
                }
                else
                {
                    text = $"Тариф: {user.Role:G}\n" +
                           $"Баланс: {user.Balance:0.00} RUB\n" +
                           $"\n<b>Включите Pro, чтобы протестировать работу VPN</b>";
                }

                

                bot.SendMessage(user.Id, text, replyMarkup: inline, parseMode: ParseMode.Html);
                return;
            }

            if (message == "Инструкция" && user.Role != Role.New)
            {
                inline = new InlineKeyboardMarkup(new[]
                {
                    new [] // first row
                    {
                        InlineKeyboardButton.WithCallbackData("IOS", "ios"),
                    },
                    new [] // first row
                    {
                        InlineKeyboardButton.WithCallbackData("Android", "and"),
                    },
                    new [] // first row
                    {
                        InlineKeyboardButton.WithCallbackData("Windows", "wnd"),
                    },
                    new [] // first row
                    {
                        InlineKeyboardButton.WithCallbackData("MacOS", "mac"),
                    }
                });


                text = "Выбирете устройство:";



                bot.SendMessage(user.Id, text, replyMarkup: inline);
                return;
            }
            #endregion

            #region end

            if (text == null)
            {
                bot.SendMessage(user.Id, "Раздел в разработке\n\n" +
                                         "Coming soon...", replyMarkup: keyboard); return;
            }
            bot.SendMessage(user.Id, text, replyMarkup: keyboard);
            #endregion
        }
    }

    internal class State
    {
        public List<TUser> Users = new List<TUser>();


        public void NewUser(TUser user)
        {
            if (!Users.Any(x => x.Id == user.Id))
            {
                Users.Add(user);
            }
        }

        public void Save()
        {
            File.WriteAllText(Environment.CurrentDirectory + @"\state.json", JsonConvert.SerializeObject(this));
        }
    }

    public class TUser : User
    {
        public Role Role = Role.New;
        public double Balance = 30;
        public DateTime? ProStart;
        public double BalanceToTime(double price = 10)
        {
            if (Role == Role.Pro && ProStart != null)
            {
                var endMinutes = Balance / (10.0 / (24.0 * 60.0));
                var date = ProStart.Value.AddMinutes(endMinutes);
                var minutes = date - DateTime.Now;

                var cost = (DateTime.Now - ProStart).Value.TotalMinutes;
                Balance -= cost * (10.0 / (24.0 * 60.0));
    
                return minutes.TotalMinutes;

            }

            return Balance / (30.0 / 24.0 / 60.0);
        }
    }

    public enum Role
    {
        New = 0,
        Standart = 1,
        Pro = 2,
    }
} 
