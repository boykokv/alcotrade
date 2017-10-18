using System;
using System.Text;
using CommandLine;
using CommandLine.Text;

namespace BB.Common.Migrations.Utils
{
    public class DbManagerOptions
    {
        [Option('u', "upgrade", HelpText = "Обновление БД")]
        public bool Upgrade { get; set; }

        [Option('d', "downgrade", HelpText = "Откат БД")]
        public bool Downgrade { get; set; }

        [Option('c', "check", HelpText = "Проверка БД")]
        public bool Check { get; set; }

        [Option('r', "recreate", HelpText = "Удаление и обновление БД")]
        public bool Recreate { get; set; }

        [Option("drop", HelpText = "Удаление БД")]
        public bool DropDatabase { get; set; }

        [Option('v', "version", HelpText = "Версия базы данных", Required = false)]
        public string Version { get; set; }

        [Option('m', "migration", HelpText = "Наименование отдельной миграции", Required = false)]
        public string Migration { get; set; }

        [Option('s', "seed", HelpText = "Вставка данных")]
        public bool SeedEnabled { get; set; }

        [Option('t', "seedForTest", HelpText = "Вставка тестовых данных")]
        public bool SeedForTestEnabled { get; set; }

        [Option('b', "backup", HelpText = "Создание бэкапа данных БД")]
        public bool Backup { get; set; }

        [Option('i', "info", HelpText = "Показать возможные миграции и информацию о базе")]
        public bool ShowAll { get; set; }

        [Option("restorebackup", HelpText = "Восстановление данных БД из бэкапа")]
        public bool RestoreBackup { get; set; }

        [Option("backup_dir_path", HelpText = "Путь к директории где должен храниться бэкап данных БД")]
        public string BackupDirPath { get; set; }

        [Option("wait", HelpText = "Доп. параметр - перед выходом из приложения ожидаем нажатия любой клавиши от пользователя", Required = false)]
        public bool Wait { get; set; }

        [Option("cmdtimeout", HelpText = "Доп. параметр - таймаут в секундах перед завершением попытки выполнить команду (пока только для операций бэкапа/восстановления из бэкапа)", Required = false)]
        public int? CommandTimeoutInSeconds { get; set; }

        /// <summary>
        /// Проверяет что только одна операция выбрана
        /// </summary>
        public virtual bool IsOneActionSelected
        {
            get
            {
                return ((Upgrade ? 1 : 0) + (Downgrade ? 1 : 0) +
                    (Check ? 1 : 0) + (Recreate ? 1 : 0) + (Backup ? 1 : 0) + (ShowAll ? 1 : 0) + (RestoreBackup ? 1 : 0) + (DropDatabase ? 1 : 0) == 1);
            }
        }

        [HelpOption]
        public string GetUsage()
        {
            string name = System.IO.Path.GetFileName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            var helpMessage = new StringBuilder();

            helpMessage.Append(HelpText
                .AutoBuild(this, current => HelpText.DefaultParsingErrorsHandler(this, current)));

            helpMessage.AppendFormat("Примеры использования: {0}{0}", Environment.NewLine);

            helpMessage.AppendFormat("{1} -u  -обновление к последней версии {0}{0}", Environment.NewLine, name);

            helpMessage.AppendFormat("{1} -u -v 1 -обновление к определенной версии версии {0}{0}", Environment.NewLine, name);

            helpMessage.AppendFormat("{1} -u -m 1.5.091600_Schedule -обновление к определенной миграции {0}{0}", Environment.NewLine, name);

            helpMessage.AppendFormat("{1} -d -откат к чистой БД {0}{0}", Environment.NewLine, name);

            helpMessage.AppendFormat("{1} -d -v 1 -откат к определенной версии {0}{0}", Environment.NewLine, name);

            helpMessage.AppendFormat("{1} -d -m 1.5.091600_Schedule -откат к определенной миграции {0}{0}", Environment.NewLine, name);

            helpMessage.AppendFormat("{1} -r -пересоздание БД к последней миграции {0}{0}", Environment.NewLine, name);

            helpMessage.AppendFormat("{1} --drop -удаление БД {0}{0}", Environment.NewLine, name);

            helpMessage.AppendFormat("{1} -r -v 1 -пересоздание БД к определенной версии {0}{0}", Environment.NewLine, name);

            helpMessage.AppendFormat("{1} -r -m 1.5.091600_Schedule -пересоздание БД к определенной миграции {0}{0}", Environment.NewLine, name);

            helpMessage.AppendFormat("{1} -c -проверка всех миграций {0}{0}", Environment.NewLine, name);

            helpMessage.AppendFormat("{1} -c -v 1 -проверка наличия миграций определенной версии {0}{0}", Environment.NewLine, name);

            helpMessage.AppendFormat("{1} -c -m 1.5.091600_Schedule -проверка наличия определенной миграций {0}{0}", Environment.NewLine, name);

            helpMessage.AppendFormat("{1} -b --backup_dir_path d:\\db_backups\\ -Создание бэкапа данных БД в указанную директорию {0}{0}", Environment.NewLine, name);

            helpMessage.AppendFormat("{1} --restorebackup --backup_dir_path d:\\db_backups\\ -Восстановление данных БД из бэкапа в указанной директории {0}{0}", Environment.NewLine, name);

            helpMessage.AppendFormat("{1} --wait -Доп. параметр - перед выходом из приложения ожидаем нажатия любой клавиши от пользователя {0}{0}", Environment.NewLine, name);

            return helpMessage.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string GetLogMessage()
        {
            var stringBuilder = new StringBuilder("Операция: ");

            if (Upgrade)
                stringBuilder.AppendFormat("Обновление БД");

            if (Downgrade)
                stringBuilder.AppendFormat("Откат БД");

            if (Check)
                stringBuilder.AppendFormat("Проверка миграций примененных к БД");

            if (Recreate)
                stringBuilder.AppendFormat("Пересоздание БД");

            if (Backup)
                stringBuilder.AppendFormat("Создание бэкапа данных БД");

            if (RestoreBackup)
                stringBuilder.AppendFormat("Восстановление данных БД из бэкапа");

            if (!string.IsNullOrWhiteSpace(Version) || !string.IsNullOrWhiteSpace(Migration))
            {
                stringBuilder.Append(Environment.NewLine);

                stringBuilder.AppendFormat("Параметры : ");

                if (!string.IsNullOrWhiteSpace(Version)) stringBuilder.AppendFormat("Версия = {0} ", Version);

                if (!string.IsNullOrWhiteSpace(Migration)) stringBuilder.AppendFormat("Название миграции = {0} ", Migration);
            }

            return stringBuilder.ToString();
        }
    }
}
