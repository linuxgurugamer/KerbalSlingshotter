using UnityEngine;
using ToolbarControl_NS;

namespace KerbalSlingshotter
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class RegisterToolbar : MonoBehaviour
    {
        void Start()
        {
            ToolbarControl.RegisterMod(SlingshotCore.MODID, SlingshotCore.MODNAME);
        }
    }
}