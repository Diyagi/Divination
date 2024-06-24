﻿using System;
using System.Runtime.InteropServices;
using Dalamud.Divination.Common.Api.Ui;
using Dalamud.Divination.Common.Api.Ui.Window;
using Dalamud.Game.Text;
using ImGuiNET;

namespace Divination.FaloopIntegration.Config;

public class PluginConfigWindow : ConfigWindow<PluginConfig>
{
    public override void Draw()
    {
        if (ImGui.Begin(Localization.ConfigWindowTitle.Format(FaloopIntegration.Instance.Name), ref IsOpen))
        {
            if (ImGui.BeginTabBar("configuration"))
            {
                if (ImGui.BeginTabItem(Localization.GeneralTab))
                {
                    DrawGeneralTab();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem(Localization.RankSTab))
                {
                    DrawPerRankTab("rank_s", ref Config.RankS);
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem(Localization.FateTab))
                {
                    DrawPerRankTab("fate", ref Config.Fate);
                    ImGui.EndTabItem();
                }

#if DEBUG
                if (ImGui.BeginTabItem("Debug"))
                {
                    DrawDebugTab();
                    ImGui.EndTabItem();
                }
#endif

                ImGui.EndTabBar();
            }

            ImGui.NewLine();

            if (ImGui.Button(Localization.SaveConfigButton))
            {
                IsOpen = false;
                Interface.SavePluginConfig(Config);
                FaloopIntegration.Instance.Connect();
            }

            ImGui.End();
        }
    }

    private void DrawGeneralTab()
    {
        ImGui.Text("Status: connected");

        if (ImGui.CollapsingHeader(Localization.Account))
        {
            ImGui.Indent();

            ImGui.InputText(Localization.AccountUsername, ref Config.FaloopUsername, 32);
            ImGui.InputText(Localization.AccountPassword, ref Config.FaloopPassword, 128, ImGuiInputTextFlags.Password);

            ImGui.Unindent();
        }

        if (ImGuiEx.CheckboxConfig(Localization.EnableActiveMobUi, ref FaloopIntegration.Instance.Config.EnableActiveMobUi))
        {
            FaloopIntegration.Instance.Ui.IsDrawing = FaloopIntegration.Instance.Config.EnableActiveMobUi;
        }
        if (FaloopIntegration.Instance.Config.EnableActiveMobUi)
        {
            ImGuiEx.CheckboxConfig(Localization.HideActiveMobUiInDuty, ref FaloopIntegration.Instance.Config.HideActiveMobUiInDuty);
        }

        ImGui.Checkbox(Localization.EnableSimpleReports, ref FaloopIntegration.Instance.Config.EnableSimpleReports);
    }

    private void DrawPerRankTab(string id, ref PluginConfig.PerRankConfig config)
    {
        ImGui.Combo($"{Localization.ReportChannel}##{id}", ref config.Channel, channelLabels, channelLabels.Length);

        ImGui.Checkbox($"{Localization.EnableSpawnReport}##{id}", ref config.EnableSpawnReport);
        ImGui.SameLine();
        ImGui.Checkbox($"{Localization.EnableDeathReport}##{id}", ref config.EnableDeathReport);

        ImGui.Text(Localization.ReportJurisdiction);
        ImGui.SameLine();
        ImGuiEx.HelpMarker(Localization.ReportJurisdictionDescription);
        ImGui.Indent();

        foreach (var expansion in gameExpansions)
        {
            ref var value = ref CollectionsMarshal.GetValueRefOrAddDefault(config.Jurisdictions, expansion, out _);
            var index = Array.IndexOf(jurisdictions, value);
            if (ImGui.Combo($"{Enum.GetName(expansion)}##{id}", ref index, jurisdictionLabels, jurisdictionLabels.Length))
            {
                value = jurisdictions[index];
            }
        }

        ImGui.Unindent();

        if (ImGui.CollapsingHeader($"{Localization.IgnoreReports}##{id}"))
        {
            ImGui.Indent();

            ImGui.Checkbox($"{Localization.ReportIgnoreInDuty}##{id}", ref config.DisableInDuty);

            ImGui.Checkbox($"{Localization.ReportIgnoreOrphanDeathReport}##{id}", ref config.SkipOrphanReport);
            ImGui.SameLine();
            ImGuiEx.HelpMarker(Localization.ReportIgnoreOrphanDeathReportDescription);

            ImGui.Checkbox($"{Localization.ReportIgnorePendingReport}##{id}", ref config.SkipPendingReport);

            ImGui.Unindent();
        }
    }

    private static void DrawDebugTab()
    {
        if (ImGui.Button("Emit mock payload"))
        {
            FaloopIntegration.Instance.EmitMockData();
        }
    }

    private readonly Jurisdiction[] jurisdictions = Enum.GetValues<Jurisdiction>();
    private readonly string[] jurisdictionLabels = Enum.GetNames<Jurisdiction>();
    private readonly string[] channelLabels = Enum.GetNames<XivChatType>();
    private readonly GameExpansion[] gameExpansions = Enum.GetValues<GameExpansion>();
}
