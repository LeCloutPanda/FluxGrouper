using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.NET.Common;
using BepInExResoniteShim;
using Elements.Core;
using FrooxEngine;
using FrooxEngine.ProtoFlux;
using FrooxEngine.ProtoFlux.Core;
using FrooxEngine.ProtoFlux.Runtimes.Execution.Nodes;
using HarmonyLib;

namespace FluxGrouper;

[ResonitePlugin(PluginMetadata.GUID, PluginMetadata.NAME, PluginMetadata.VERSION, PluginMetadata.AUTHORS, PluginMetadata.REPOSITORY_URL)]
[BepInDependency(BepInExResoniteShim.PluginMetadata.GUID, BepInDependency.DependencyFlags.HardDependency)]
public class Patch : BasePlugin
{
    static ManualLogSource Logger = null!;
	private static ConfigEntry<bool> ENABLED;

	public override void Load()
	{
		ENABLED = Config.Bind("General", "Create Context Menu Item", true, "Create Context Menu item for fixing flux groupings");

        Logger = Log;
        HarmonyInstance.PatchAll();
    }

    [HarmonyPatch(typeof(ProtoFluxTool), nameof(ProtoFluxTool.GenerateMenuItems))]
    class ProtoFluxToolPatch
    {
        [HarmonyPostfix]
        private static void Postfix(ProtoFluxTool __instance, InteractionHandler tool, ContextMenu menu, SyncRefList<ProtoFluxNodeVisual> ____selectedNodes)
        {
            if (!ENABLED.Value) return;

            if (____selectedNodes.Count > 0 && __instance.Slot.ActiveUser == __instance.LocalUser)
            {
                menu.AddItem("Recalculate Flux Groups", new Uri("resdb:///05616f9bc2e93d18d9fa3a8b98dbd267bcaa5190bc222e384c743945f37ff315.png"), new colorX?(colorX.Orange)).Button.LocalPressed += (IButton button, ButtonEventData data) =>
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
                        // Add this delay so we don't nuke the temp slot before we actually do what we want
                        __instance.RunInUpdates(6, () =>
                        {
                            __instance.Slot.ActiveUserRoot.Slot.FindChild("FluxGrouper - Temp")?.Destroy();
                            foreach (ProtoFluxNodeVisual selectedNode in ____selectedNodes)
                            {
                                if (selectedNode != null)
                                {
                                    selectedNode.IsSelected.Value = false;
                                }
                            }
                            ____selectedNodes.Clear();
                            menu.CloseMenu(button, data);
                        });
                    }
                };

            }
        }
    }
}