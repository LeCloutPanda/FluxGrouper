using BepInEx;
using BepInEx.Logging;
using BepInExResoniteShim;
using Elements.Core;
using FrooxEngine;
using FrooxEngine.ProtoFlux;
using FrooxEngine.ProtoFlux.Core;
using FrooxEngine.ProtoFlux.Runtimes.Execution.Nodes;
using HarmonyLib;

namespace FluxGrouper;

[BepInPlugin(GUID, Name, Version)]
public class Patch : BaseResonitePlugin
{
    public const string GUID = "dev.lecloutpanda.fluxgrouper";
    public const string Name = "Flux Grouper";
    public const string Version = "1.1.3";
    public override string Author => "LeCloutPanda";
    public override string Link => "https://github.com/LeCloutPanda/FluxGrouper";

    static ManualLogSource Logger = null!;

    public override void Load()
    {
        Logger = Log;
        HarmonyInstance.PatchAll();
    }

    [HarmonyPatch(typeof(ProtoFluxTool), nameof(ProtoFluxTool.GenerateMenuItems))]
    class ProtoFluxToolPatch
    {
        [HarmonyPostfix]
        private static void Postfix(ProtoFluxTool __instance, InteractionHandler tool, ContextMenu menu, SyncRefList<ProtoFluxNodeVisual> ____selectedNodes)
        {
            if (____selectedNodes.Count > 0 && __instance.Slot.ActiveUser == __instance.LocalUser)
            {
                menu.AddItem("Recalculate Flux Groups(Local)", new Uri("resdb:///05616f9bc2e93d18d9fa3a8b98dbd267bcaa5190bc222e384c743945f37ff315.png"), new colorX?(colorX.Orange)).Button.LocalPressed += (IButton button, ButtonEventData data) =>
                {
                    try
                    {
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
                    }
                    catch (Exception error)
                    {
                        Logger.LogError(error);
                    }
                    finally
                    {
                        ____selectedNodes.Clear();
                        menu.CloseMenu(button, data);
                    }
                };
                
            }
        }
    }
}