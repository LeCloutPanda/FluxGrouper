using Elements.Core;
using FrooxEngine;
using FrooxEngine.ProtoFlux;
using FrooxEngine.ProtoFlux.Core;
using FrooxEngine.ProtoFlux.Runtimes.Execution.Nodes;
using HarmonyLib;
using ResoniteModLoader;
using System;

public class VideoProxy : ResoniteMod
{
    public enum RegroupType
    {
        LinkNode,
        Local
    }

    public override string Author => "LeCloutPanda";
    public override string Name => "Flux Grouper";
    public override string Version => "1.1.1";
    public override string Link => "https://github.com/LeCloutPanda/FluxGrouper";

    public static ModConfiguration config;
    [AutoRegisterConfigKey] private static ModConfigurationKey<bool> ENABLED = new ModConfigurationKey<bool>("enabledToggle", "Generate option to use recalculate flux groups when using profoflux tool.", () => true);
    [AutoRegisterConfigKey] private static ModConfigurationKey<RegroupType> REGROUPTYPE = new ModConfigurationKey<RegroupType>("regroupType", "The method used to recalculate groups.", () => RegroupType.LinkNode);

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
                        switch (config.GetValue(REGROUPTYPE))
                        {
                            case RegroupType.LinkNode:
                                Slot temp = __instance.Slot.ActiveUserRoot.Slot.AddSlot("FluxGrouper - Temp");
                                temp.PersistentSelf = false;
                                Link link = temp.AttachComponent<Link>();
                                link.A.Target = (INode)____selectedNodes[0].Slot.GetComponentInParents<ProtoFluxNode>();
                                link.RunInUpdates(3, () =>
                                {
                                    if (link != null && !link.IsRemoved)
                                    {
                                        link.A.Target = null;
                                        link.RunInUpdates(3, () =>
                                        {
                                            if (temp != null && !temp.IsRemoved)
                                                temp.Destroy(false);
                                        });
                                    }
                                });
                                break;

                            case RegroupType.Local:
                                __instance.World.ProtoFlux.ScheduleGroupRebuild(____selectedNodes[0].Slot.GetComponentInParents<ProtoFluxNode>().Group);
                                break;
                        }

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