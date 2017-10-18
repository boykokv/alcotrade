namespace BB.Common.Migrations
{
    public class Constants
    {
        public const string NotUniqMigration =
            "Найдено более одной миграции. Необходимо уточнить параметры.{0}Возможные варианты:{0}{1}";

        public const string MigrationNotFound = "Не найдено миграции, удовлетворяющей условию {0}";

        public const string NotUniqMigrationInDb =
            "Найдено более одной миграции в базе данных. Необходимо уточнить параметры.{0}Возможные варианты:{0}{1}";

        public const string MigrationNotFoundInDb = "В базе данных не найдено миграции, удовлетворяющей условию {0}";
    }
}