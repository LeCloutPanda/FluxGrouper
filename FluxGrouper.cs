using FrooxEngine.UIX;
using FrooxEngine;
using HarmonyLib;
using ResoniteModLoader;
using System;
using System.Threading.Tasks;
using Elements.Core;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using SkyFrost.Base;
using System.Reflection;
using System.Linq.Expressions;
using FrooxEngine.Store;
using FrooxEngine.ProtoFlux;

public class VideoProxy : ResoniteMod
{

    public enum Resolution
    {
        Q480P,
        Q720P,
        Q1080P,
        Q1440P,
        Q2160P,
        QBest
    }

    public enum ProxyLocation
    {
        Australia,
        NorthAmerica,
        CUSTOM
    }

    public override string Author => "LeCloutPanda";
    public override string Name => "Flux Grouper";
    public override string Version => "1.0.0";
    public override string Link => "https://github.com/LeCloutPanda/FluxGrouper";

    public static ModConfiguration config;
    [AutoRegisterConfigKey] private static ModConfigurationKey<bool> ENABLED = new ModConfigurationKey<bool>("enabledToggle", "Generate option to use recalculate flux groups when using profoflux tool.", () => true);

    public override void OnEngineInit()
    {
        config = GetConfiguration();
        config.Save(true);

        Harmony harmony = new Harmony("dev.lecloutpanda.fluxgrouper");
        harmony.PatchAll();
    }

    [HarmonyPatch(typeof(ProtoFluxTool), nameof(ProtoFluxTool.GenerateMenuItems))]
    class ProtoFluxToolPatch
    {
        [HarmonyPostfix]
        private static void Postfix(ProtoFluxTool __instance, InteractionHandler tool, ContextMenu menu, SyncRefList<ProtoFluxNodeVisual> ____selectedNodes)
        {
            if (config.GetValue(ENABLED) && ____selectedNodes.Count > 0 && __instance.Slot.ActiveUser == __instance.LocalUser)
            {
                menu.AddItem("Recalculate Flux Groups(Local)", new Uri("resdb:///05616f9bc2e93d18d9fa3a8b98dbd267bcaa5190bc222e384c743945f37ff315.png"), new colorX?(colorX.Orange)).Button.LocalPressed += (IButton button, ButtonEventData data) =>
                {
                    try
                    {
                        Msg($"Recalculated Flux Node Groups for {____selectedNodes.Count}");
                        __instance.World.ProtoFlux.ScheduleGroupRebuild(____selectedNodes[0].Slot.GetComponentInParents<ProtoFluxNode>().Group); // ____selectedNodes[0].Slot.GetComponentInParents<ProtoFluxNode>().Group

                        foreach (ProtoFluxNodeVisual selectedNode in ____selectedNodes)
                        {
                            if (selectedNode != null) selectedNode.IsSelected.Value = false;
                        }

                        ____selectedNodes.Clear();
                        menu.CloseMenu(button, data);
                    }
                    catch (Exception error)
                    {
                        Error(error);
                    }
                };
            }
        }
    }
}