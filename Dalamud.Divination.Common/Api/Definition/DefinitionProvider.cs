﻿using System;
using System.IO;
using System.Threading.Tasks;
using Dalamud.Divination.Common.Api.Logger;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Dalamud.Divination.Common.Api.Definition
{
    public abstract class DefinitionProvider<TContainer> : IDefinitionProvider<TContainer> where TContainer : DefinitionContainer, new()
    {
        protected const string DefaultFilename = "Ephemera.json";

        private TContainer? container;
        private readonly object containerLock = new();
        private readonly Serilog.Core.Logger logger = DivinationLogger.Of("Divination.DefinitionProvider");
        private readonly Task initializationTask;

        protected DefinitionProvider()
        {
            initializationTask = Task.Run(Update);
        }

        public abstract string Filename { get; }

        public TContainer Container
        {
            get
            {
                initializationTask.Wait();

                lock (containerLock)
                {
                    return container ?? throw new AggregateException("定義ファイルの取得に失敗しました。");
                }
            }
        }

        public virtual bool AllowObsoleteDefinitions => false;

        protected abstract JObject? Fetch();

        public void Update()
        {
            var json = Fetch();
            if (json == null)
            {
                return;
            }

            lock (containerLock)
            {
                container = json.ToObject<TContainer>(new JsonSerializer
                {
                    Converters =
                    {
                        new HexStringJsonConverter()
                    }
                });

                var localGameVersion = ReadLocalGameVersion();
                if (localGameVersion != container?.Version)
                {
                    logger.Warning(
                        "ゲームバージョン \"{DefinitionGameVersion}\" はサポートされていません。現在のゲームバージョンは \"{LocalGameVersion}\" です。",
                        container?.Version, localGameVersion);

                    if (!AllowObsoleteDefinitions)
                    {
                        container = new TContainer
                        {
                            Version = container?.Version,
                            Patch = container?.Patch
                        };
                    }
                }
                else
                {
                    container.IsObsolete = false;

                    logger.Information(
                        "パッチ {GamePatch} 向けの定義ファイル \"{DefinitionFilename}\" を読み込みました。現在のゲームバージョンは \"{LocalGameVersion}\" です。",
                        container?.Patch, Filename, localGameVersion);
                }
            }

            logger.Verbose("{DefinitionFilename}\n{DefinitionJson}", Filename, JsonConvert.SerializeObject(json, Formatting.Indented));
        }

        private static string ReadLocalGameVersion()
        {
            // "C:\Program Files (x86)\SquareEnix\FINAL FANTASY XIV - A Realm Reborn\game\ffxivgame.ver"
            var gameVersionPath = Path.Combine(DivinationEnvironment.GameDirectory, "ffxivgame.ver");

            return File.ReadAllText(gameVersionPath).Trim();
        }

        public virtual void Dispose()
        {
            initializationTask.Dispose();
        }
    }
}
