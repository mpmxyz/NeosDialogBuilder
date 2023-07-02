using BaseX;
using FrooxEngine;
using FrooxEngine.UIX;
using HarmonyLib;
using NeosModLoader;

namespace NeosDialogBuilder
{
    public class NeosDialogBuilderMod : NeosMod
    {
        internal const string TITLE_SECRET_EDIT_PANEL = "Edit...";
        internal const string LABEL_SECRET_EDIT = "Edit";
        internal const string LABEL_USERSPACE_DIALOG_CLOSE = "OK";
        internal const string SECRET_TEXT_PATTERN = "*";
        internal const float CONFIG_PANEL_HEIGHT = 0.25f;
        internal const float USERSPACE_PANEL_HEIGHT = 0.15f;
        internal const float SPACING = 4f;
        internal const float BUTTON_HEIGHT = 24f;
        internal const float ERROR_HEIGHT = 8f;
        internal static readonly float2 CONFIG_CANVAS_SIZE = new float2(200f, 108f);
        internal static readonly float2 USERSPACE_CANVAS_SIZE = new float2(200f, 52f*2);//TODO: userspace should become "normal" dialog

        public override string Name => "NeosDialogBuilder";
        public override string Author => "mpmxyz";
        public override string Version => "1.0.0";
        public override string Link => "https://github.com/mpmxyz/NeosDialogBuilder/";

        public override void OnEngineInit()
        {
            Harmony harmony = new Harmony("com.github.mpmxyz.neosdialogbuilder");
            harmony.PatchAll();
        }

        [HarmonyPatch(typeof(DevToolTip), "OnPrimaryPress")]
        class TestPatch
        {
            static bool Prefix(DevToolTip __instance)
            {
                World world = __instance.World;
                new DialogBuilder<TestDialog>()
                .BuildWindow(
                    "Test",
                    world,
                    world.LocalUserViewPosition,
                    world.LocalUserViewRotation,
                    world.LocalUserViewScale,
                    new TestDialog());
                return false;
            }
        }
    }
}