using System;
using System.IO;
using System.Text.Json;
using BattleShipBrain;
using BattleShipConsoleUI;
using DAL;

namespace BattleShipConsoleApp
{
    internal class Program
    {
        private static string? _basePath;
        private static readonly GameConfig Config = new();
        private static string? _loadedGameConfigName = "";
        private static int _loadedGameConfigId = 0;

        private static void Main(string[] args)
        {
            Console.WriteLine("Hello Battleship!");
            _basePath = args.Length == 1 ? args[0] : Directory.GetCurrentDirectory();

            SaveConfig("default", Config);

            DirectoryInfo di = new(@$"{_basePath + Path.DirectorySeparatorChar + "Configs"}");
            FileInfo[] files = di.GetFiles("*.json");

            Console.Clear();
            Console.WriteLine("=========| Load Configuration |=========\n");
            Console.WriteLine("Available Configurations !\n");
            Console.WriteLine("=========| Load Local Configuration |=========");
            Console.WriteLine();
            foreach (FileInfo file in files) Console.WriteLine(file.Name.Split(".")[0]);
            Console.WriteLine();
            Console.WriteLine("=========| Load DB Configuration |=========");
            Console.WriteLine();
            using var db = new ApplicationDbContext();
            foreach (var dbConfig in db.GameConfigSaves) Console.WriteLine(dbConfig!.ConfigName);
            Console.WriteLine();
            Console.WriteLine("=============================================");
            var inputDataStr = "";
            var configDataTypeStr = "";
            while (configDataTypeStr == "" || inputDataStr == "")
            {
                Console.Write("Db or Local: ");
                configDataTypeStr = Console.ReadLine()?.Trim().ToUpper();
                Console.Write("Your choice(R to Load Default): ");
                inputDataStr = Console.ReadLine()?.Trim();
                Console.WriteLine();
            }

            EDataLocationType configDataLocationType = EDataLocationType.NotDefined;
            if (configDataTypeStr == "DB") configDataLocationType = EDataLocationType.DataBase;
            if (configDataTypeStr == "LOCAL") configDataLocationType = EDataLocationType.Local;
            StartProgram(inputDataStr!, configDataLocationType);
        }

        private static string GetFileNameConfig(string configName)
        {
            DirectoryInfo di = new(@$"{_basePath + Path.DirectorySeparatorChar + "Configs"}");
            FileInfo[] files = di.GetFiles("*.json");
            string fileNameConfig;

            foreach (FileInfo file in files)
            {
                if (file.Name.Split(".")[0].ToUpper() == configName.ToUpper())
                {
                    fileNameConfig = _basePath + Path.DirectorySeparatorChar + "Configs" + Path.DirectorySeparatorChar +
                                     $"{configName}.json";
                    return fileNameConfig;
                }
            }

            fileNameConfig = _basePath + Path.DirectorySeparatorChar + "Configs" + Path.DirectorySeparatorChar +
                             $"{configName}.json";
            return fileNameConfig;
        }

        private static string GetConfJsonStr(GameConfig config)
        {
            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            var confJsonStr = JsonSerializer.Serialize(config, jsonOptions);

            return confJsonStr;
        }

        private static void SaveConfig(string configName, GameConfig config)
        {
            var fileNameStandardConfig = GetFileNameConfig(configName);

            var confJsonStr = GetConfJsonStr(config);

            Console.WriteLine($"{configName} conf is in: " + fileNameStandardConfig);
            if (!File.Exists(fileNameStandardConfig))
            {
                Console.WriteLine($"Saving {configName} config!");
                File.WriteAllText(fileNameStandardConfig, confJsonStr);
            }
        }

        private static GameConfig LoadNewConfig(string configName, EDataLocationType loadFromDataLocationType)
        {
            GameConfig config = new();
            var fileNameStandardConfig = GetFileNameConfig(configName);
            using var db = new ApplicationDbContext();
            if (loadFromDataLocationType is EDataLocationType.Local)
            {
                if (File.Exists(fileNameStandardConfig))
                {
                    Console.WriteLine("Loading config...");
                    _loadedGameConfigName = fileNameStandardConfig;
                    var confText = File.ReadAllText(fileNameStandardConfig);
                    foreach (var dbConfig in db.GameConfigSaves)
                    {
                        if (string.Equals(dbConfig!.ConfigName, configName, StringComparison.CurrentCultureIgnoreCase))
                        {
                            _loadedGameConfigName = dbConfig.ConfigName;
                            _loadedGameConfigId = dbConfig.GameConfigSavedId;
                        }
                    }
                    config = JsonSerializer.Deserialize<GameConfig>(confText) ?? throw new InvalidOperationException();
                }
            }
            if (loadFromDataLocationType is EDataLocationType.DataBase)
            {
                foreach (var dbConfig in db.GameConfigSaves)
                {
                    if (string.Equals(dbConfig!.ConfigName, configName, StringComparison.CurrentCultureIgnoreCase))
                    {
                        _loadedGameConfigName = dbConfig.ConfigName;
                        _loadedGameConfigId = dbConfig.GameConfigSavedId;
                        config = JsonSerializer.Deserialize<GameConfig>(dbConfig.GameConfigJsnString) ?? throw new InvalidOperationException();
                        return config;
                    }
                }
            }

            
            return config;
        }

        private static void StartProgram(string config, EDataLocationType locationType)
        {
            BsBrain brain = new(LoadNewConfig(config, locationType), _basePath!);
            if (_loadedGameConfigName is null || _loadedGameConfigName.Length < 1)
            {
                _loadedGameConfigName = "Default";
            }
            BsConsoleUi console = new(brain, _loadedGameConfigName, locationType, _loadedGameConfigId);
            console.DrawConsoleUi();
            // console.DrawUi("main");
        }
    }
}