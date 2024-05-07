﻿using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Divination.Common.Api.Chat;
using Dalamud.Divination.Common.Api.Dalamud;
using Dalamud.Divination.Common.Api.Ui.Window;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Plugin.Services;
using Divination.FaloopIntegration.Config;
using Divination.FaloopIntegration.Ipc;
using ImGuiNET;

namespace Divination.FaloopIntegration.Ui;

public class ActiveMobUi : IWindow, IDisposable
{
    private readonly ConcurrentDictionary<string, MobSpawnEvent> mobs = new();
    private readonly Task cleanupTask;
    private readonly CancellationTokenSource cancellation = new();
    private readonly AetheryteLinkInChatIpc ipc;
    private readonly IChatClient chatClient;
    private readonly IGameGui gameGui;
    private readonly PluginConfig config;
    private readonly ICondition condition;

    public ActiveMobUi(AetheryteLinkInChatIpc ipc, IChatClient chatClient, IGameGui gameGui, PluginConfig config, ICondition condition)
    {
        this.ipc = ipc;
        this.chatClient = chatClient;
        this.gameGui = gameGui;
        this.config = config;
        this.condition = condition;
        cleanupTask = new Task(CleanUp);
        cleanupTask.Start();
    }

    private bool isDrawing = true;
    public bool IsDrawing
    {
        get => isDrawing && !mobs.IsEmpty && (!config.HideActiveMobUiInDuty || !Utils.IsInDuty(condition));
        set => isDrawing = value;
    }

    public void Draw()
    {
        if (!IsDrawing)
        {
            return;
        }

        if (ImGui.Begin(Localization.ActiveMob, ImGuiWindowFlags.AlwaysAutoResize))
        {
            if (ImGui.BeginTable("active_mobs", 3, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.SizingFixedFit))
            {
                ImGui.TableSetupColumn(Localization.TableHeaderMob);
                ImGui.TableSetupColumn(Localization.TableHeaderTime);
                ImGui.TableSetupColumn(string.Empty);
                ImGui.TableHeadersRow();

                foreach (var mob in mobs.Values.OrderBy(x => x.SpawnedAt))
                {
                    ImGui.TableNextRow();
                    DrawRow(mob);
                }

                ImGui.EndTable();
            }

            ImGui.End();
        }
    }

    private void DrawRow(MobSpawnEvent mob)
    {
        ImGui.TableNextColumn();
        var mobText = $"{Utils.GetRankIconChar(mob.Rank).ToIconChar()} {mob.Mob.Singular.RawString} {SeIconChar.CrossWorld.ToIconString()} {mob.World.Name.RawString}";
        ImGui.Text(mobText);

        ImGui.TableNextColumn();
        var span = DateTime.UtcNow - mob.SpawnedAt;
        ImGui.Text(span.ToString("mm\\:ss"));

        ImGui.TableNextColumn();
        if (ImGui.Button($"{Localization.TableButtonTeleport}##{mob.Id}"))
        {
            if (ipc.Teleport(mob.TerritoryTypeId, mob.TerritoryType.Map.Row, mob.Coordinates ?? default, mob.WorldId))
            {
                chatClient.Print(Localization.TeleportingMessage.Format(mobText));
            }
            else
            {
                DalamudLog.Log.Warning("Failed to teleport: {Event}", mob);
            }
        }
        if (mob.Coordinates.HasValue)
        {
            ImGui.SameLine();
            if (ImGui.Button($"{Localization.TableButtonOpenMap}##{mob.Id}"))
            {
                var mapLink = new MapLinkPayload(mob.TerritoryTypeId, mob.Map.RowId, mob.Coordinates.Value.X, mob.Coordinates.Value.Y);
                gameGui.OpenMapWithMapLink(mapLink);
            }
        }
    }

    public void OnMobSpawn(MobSpawnEvent ev)
    {
        mobs[ev.Id] = ev;
    }

    public void OnMobDeath(MobDeathEvent ev)
    {
        mobs.TryRemove(ev.Id, out _);
    }

    private async void CleanUp()
    {
        while (!cancellation.IsCancellationRequested)
        {
            foreach (var mob in mobs.Values.Where(x => DateTime.UtcNow - x.SpawnedAt > GetMaxAge(x)))
            {
                mobs.TryRemove(mob.Id, out _);
            }

            await Task.Delay(10 * 1000);
        }
    }

    private static TimeSpan GetMaxAge(MobSpawnEvent ev)
    {
        return ev.Rank switch
        {
            MobRank.S => TimeSpan.FromMinutes(10),
            MobRank.SS => TimeSpan.FromMinutes(10),
            MobRank.FATE => TimeSpan.FromMinutes(30),
            _ => throw new ArgumentOutOfRangeException(nameof(ev.Rank), ev.Rank, null),
        };
    }

    public void Dispose()
    {
        cancellation.Cancel();
    }
}
