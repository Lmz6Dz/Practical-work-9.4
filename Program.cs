using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;

namespace Practical_work_9._4
{
    internal class Program
    {
        static readonly string token = System.IO.File.ReadAllText(@"token");
        private static readonly TelegramBotClient bot = new(token);

        public static async Task HandleUpdatesAsync(ITelegramBotClient bot, Update update, CancellationToken cancellationToken)
        {
            if (update.Message != null)
            {
                switch (update.Message.Type)
                {
                    case MessageType.Document:
                        {
                            string newname = await DownLoad(update.Message.Document.FileId, update.Message.Document.FileName, update.Message.Chat.Id);
                            await bot.SendTextMessageAsync(update.Message.Chat.Id, $"файл {update.Message.Document.FileName} загружен под именем {newname}");
                            break;
                        }
                    case MessageType.Audio:
                        {
                            string newname = await DownLoad(update.Message.Audio.FileId, update.Message.Audio.FileName, update.Message.Chat.Id);
                            await bot.SendTextMessageAsync(update.Message.Chat.Id, $"файл {update.Message.Audio.FileName} загружен под именем {newname}");
                            break;
                        }
                    case MessageType.Voice:
                        {
                            string newname = await DownLoad(update.Message.Voice.FileId, $"{update.Message.Voice.FileId}.ogg", update.Message.Chat.Id);
                            await bot.SendTextMessageAsync(update.Message.Chat.Id, $" Загружено голосовое сообщение {update.Message.Voice.FileSize} байт под именем {newname}");
                            break;
                        }
                    case MessageType.Photo:
                        {
                            string newname = await DownLoad(update.Message.Photo.Last().FileId, $"{update.Message.Photo.Last().FileId}.jpg", update.Message.Chat.Id);
                            await bot.SendTextMessageAsync(update.Message.Chat.Id, $"Загружено изображение {update.Message.Photo.Last().FileSize} байт под именем {newname}");
                            break;
                        }
                    case MessageType.Text:
                        {
                            await HandleMessage(bot, update.Message, update.Message.Chat.Id);
                            break;
                        }
                    default: return;
                }

                Console.WriteLine($"Received a '{update.Message.Type}' message in chat {update.Message.Chat.Id}.");

                return;
            }
        }

        public static async Task HandleMessage(ITelegramBotClient bot, Message message, long directory)
        {
            switch (message.Text)
            {
                case "/start":
                    {
                        await bot.SendTextMessageAsync(message.Chat.Id, "Здравия, вас приветствует бот для закачки файлов. " +
                            "Отправьте документы, фото, аудио, голосовое сообщение для сохранения на диск. " +
                            "Для просмотра и скачивания введите команду /download");
                        break;
                    }
                case "/download":
                    {
                        if (Directory.Exists($"{directory}"))
                        {
                            await bot.SendTextMessageAsync(message.Chat.Id, "Список ранее загруженных файлов: ");

                            var file = Directory.GetFiles($"{directory}").Select(fn => Path.GetFileName(fn)).ToArray();

                            for (int i = 0; i < file.Length; i++)
                            {
                                string pathfull = Path.Combine(Directory.GetCurrentDirectory(), $"{directory}", file[i]);

                                using FileStream stream = System.IO.File.Open(pathfull, FileMode.Open);

                                await bot.SendDocumentAsync(message.Chat.Id, new InputOnlineFile(stream, file[i]));
                            }
                        }
                        else
                            await bot.SendTextMessageAsync(message.Chat.Id, "Файлы не были отправлены, нечего загружать");
                        break;
                    }
                default:
                    {
                        await bot.SendTextMessageAsync(message.Chat.Id, $"Вы сказали:\n{message.Text}");
                        break;
                    }
            }
        }

        public static Task HandleErrorAsync(ITelegramBotClient client, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Ошибка телеграм АПИ:\n{apiRequestException.ErrorCode}\n{apiRequestException.Message}",
                _ => exception.ToString()
            };
            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }

        public static async Task<string> DownLoad(string fileId, string path, long directory)
        {
            if (!Directory.Exists($"{directory}")) Directory.CreateDirectory($"{directory}");

            string fileName = path;

            //сохранение копии
            if (System.IO.File.Exists($"{directory}/{fileName}"))
            {
                int n = 1;
                while (System.IO.File.Exists($"{directory}/{fileName}"))
                {
                    fileName = Path.Combine(
                        Path.GetDirectoryName($"{directory}"),
                        Path.GetFileNameWithoutExtension(path) + " (" + n.ToString() + ")" + Path.GetExtension(path));
                    n++;
                }
            }

            FileStream fs = new($"{directory}/" + fileName, FileMode.Create);

            await bot.DownloadFileAsync(bot.GetFileAsync(fileId).Result.FilePath, fs);

            fs.Close();

            fs.Dispose();

            return fileName;
        }

        public static Task Main()
        {
            var cts = new CancellationTokenSource();

            var receiverOptions = new Telegram.Bot.Polling.ReceiverOptions
            {
                AllowedUpdates = { }
            };

            bot.StartReceiving(
                HandleUpdatesAsync,
                HandleErrorAsync,
                receiverOptions,
                cancellationToken: cts.Token);

            Console.WriteLine("Запущен бот " + bot.GetMeAsync().Result.Username);

            Console.ReadLine();

            cts.Cancel();

            return Task.CompletedTask;
        }
    }
}