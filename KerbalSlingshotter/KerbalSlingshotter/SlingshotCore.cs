using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using Debug = UnityEngine.Debug;
using KSP.IO;
using KSP.UI.Screens;

namespace KerbalSlingshotter
{
    [KSPAddon(KSPAddon.Startup.Flight,false)]
    public class FlightSlingshot : SlingshotCore { }

    [KSPAddon(KSPAddon.Startup.TrackingStation,false)]
    public class TrackingSlingshot : SlingshotCore { }

    public class TimeInfo
    {
        public uint years = 0, days = 0, hours = 0, minutes = 0, seconds = 0;
    }
    public class SlingshotCore : MonoBehaviour
    {
        internal static Texture2D ShipIcon = null;
        internal static Texture2D BodyIcon = null;
        private static Vessel vessel { get { return CurrentVessel(); } }
        protected Rect windowPos = new Rect(50, 100, 300, 400);
        double DesiredTime;
        TimeInfo desiredTimeInfo = new TimeInfo();
        TimeInfo endTimeInfo = null;
        bool startTimeNow = false;
        private static Vessel lastVessel = null;
        public static int HoursPerDay { get { return GameSettings.KERBIN_TIME ? 6 : 24; } }
        public static int DaysPerYear { get { return GameSettings.KERBIN_TIME ? 426 : 365; } }
        private static double UT { get { return Planetarium.GetUniversalTime(); } }
        ApplicationLauncherButton button;
        bool WindowVisible = false;
       // uint years = 0, days = 0, hours = 0, minutes = 0, seconds = 0;

        public void Start()
        {
           /* DesiredTime = UT; */
            if (HighLogic.LoadedScene == GameScenes.TRACKSTATION || HighLogic.LoadedScene == GameScenes.FLIGHT)
                CreateButtonIcon();
            if (ShipIcon == null)
                ShipIcon = GameDatabase.Instance.GetTexture("Squad/PartList/SimpleIcons/RDicon_commandmodules", false);
            if (BodyIcon == null)
                BodyIcon = GameDatabase.Instance.GetTexture("SlingShotter/Textures/body", false);
        }

        private void OnGUI()
        {
            if (WindowVisible)
            {
                GUI.skin = HighLogic.Skin;
                windowPos = GUILayout.Window(1, windowPos, WindowGUI, "SlingShotter | Set Time", GUILayout.MinWidth(300));
                if (HighLogic.LoadedScene == GameScenes.TRACKSTATION || HighLogic.LoadedScene == GameScenes.FLIGHT)
                    DrawIconForAllOrbits();
            }
        }

        void OnDestroy()
        {
            ApplicationLauncher.Instance.RemoveModApplication(button);
        }

        void FixedUpdate()
        {
            // Following needed to adjust values for slider, which can only use float
            // Only do it 1/sec to avoid any unnecessary cpu
            if (Planetarium.GetUniversalTime() > lastTime)
            {
                lastTime = Planetarium.GetUniversalTime() + 1;
                if (vessel != null && vessel.patchedConicSolver.maneuverNodes.Count > 1)
                {
                    div = 1;
                    while (vessel.patchedConicSolver.maneuverNodes.First().UT / div > float.MaxValue ||
                        vessel.patchedConicSolver.maneuverNodes.Last().UT / div > float.MaxValue)
                        div++;

                    if (startTimeNow)
                        sliderBeginTime = (float)Planetarium.GetUniversalTime() + 60;
                    else
                        sliderBeginTime = (float)vessel.patchedConicSolver.maneuverNodes.First().UT / div;

                    if (endTimeInfo == null)
                    {
                        sliderEndtime = (float)vessel.patchedConicSolver.maneuverNodes.Last().UT / div;
                        var a = setTimeSelection(sliderEndtime / div);                    
                    }
                    else
                    {
                        sliderEndtime = (endTimeInfo.years * DaysPerYear * HoursPerDay * 3600 +
                            endTimeInfo.days * HoursPerDay * 3600 +
                            endTimeInfo.hours * 3600 +
                            endTimeInfo.minutes * 60 +
                            endTimeInfo.seconds + (float)Planetarium.GetUniversalTime()) / div;
                    }
                    
                    if (timeSel < sliderBeginTime)
                        timeSel = sliderBeginTime;
                    if (timeSel > sliderEndtime)
                        timeSel = sliderEndtime;
                }
            }
            if (vessel != null && lastVessel != vessel)
            {
                if (vessel.patchedConicSolver.maneuverNodes.Count > 0)
                    desiredTimeInfo = setTimeSelection(vessel.patchedConicSolver.maneuverNodes.First().UT);
                lastVessel = vessel;
            }
        }

        static Vessel CurrentVessel()
        {
            if (HighLogic.LoadedScene == GameScenes.FLIGHT && FlightGlobals.ActiveVessel != null)
            {
                return FlightGlobals.ActiveVessel;
            }
            else if (HighLogic.LoadedScene == GameScenes.TRACKSTATION)
            {

                SpaceTracking st = (SpaceTracking)FindObjectOfType(typeof(SpaceTracking));
                if (st.MainCamera.target != null && st.MainCamera.target.type == MapObject.ObjectType.Vessel)
                {
                    return st.MainCamera.target.vessel;
                }
                else
                {
                    return null;
                }
            }
            return null;
        }


        TimeInfo setTimeSelection(double selection)
        {
            TimeInfo t = new TimeInfo();
            selection -= UT;
            t.years = (uint)(selection / DaysPerYear / HoursPerDay / 3600);
            t.days = (uint)((selection / HoursPerDay / 3600) % DaysPerYear);
            t.hours = (uint)((selection / 3600) % HoursPerDay);
            t.minutes = (uint)((selection / 60) % 60);
            t.seconds = (uint)(selection % 60);
            return t;
        }

        Vector2 scrollVector;
        float sliderBeginTime, sliderEndtime;
        float timeSel = 0;
        int div = 1;
        double lastTime = 0;

        private void WindowGUI(int windowID)
        {
            GUIStyle textFieldLabel = new GUIStyle(GUI.skin.textField);
            GUIStyle mySty = new GUIStyle(GUI.skin.button);
            mySty.normal.textColor = mySty.focused.textColor = Color.white;
            mySty.hover.textColor = mySty.active.textColor = Color.yellow;
            mySty.onNormal.textColor = mySty.onFocused.textColor = mySty.onHover.textColor = mySty.onActive.textColor = Color.green;
            mySty.padding = new RectOffset(8, 8, 8, 8);
            GUILayout.BeginVertical();
           

            GUIStyle MyScrollView = new GUIStyle(HighLogic.Skin.scrollView);
            var scrollbar_stlye = new GUIStyle(MyScrollView);
            scrollbar_stlye.padding = new RectOffset(3, 3, 3, 3);
            scrollbar_stlye.border = new RectOffset(3, 3, 3, 3);
            scrollbar_stlye.margin = new RectOffset(1, 1, 1, 1);
            scrollbar_stlye.overflow = new RectOffset(1, 1, 1, 1);

            

            if (vessel.patchedConicSolver.maneuverNodes.Any())
            {
                GUILayout.BeginHorizontal();
                
                GUILayout.Space(5);
                scrollVector = GUILayout.BeginScrollView(scrollVector, scrollbar_stlye); //, GUILayout.Width(266), GUILayout.Height(225));
                foreach (var t1 in vessel.patchedConicSolver.maneuverNodes)
                {
                    TimeInfo t = setTimeSelection(t1.UT);

                    GUILayout.BeginHorizontal();
                    string l = t.years.ToString() + "y, " +
                        t.days.ToString() + "d, " +
                        t.hours.ToString() + "h, " +
                        t.minutes.ToString() + "m, " +
                        t.seconds.ToString() + "s";
                    if (GUILayout.Button(l))
                    {
                        desiredTimeInfo = setTimeSelection(t1.UT);
                        startTimeNow = false;
                        timeSel = (float)t1.UT / div;
                    }
#if false
                    GUILayout.Label("y", GUILayout.ExpandWidth(false));
                    GUILayout.Label(t.years.ToString(), textFieldLabel, GUILayout.Width(40));
                    GUILayout.Label("d", GUILayout.ExpandWidth(false));
                    GUILayout.TextField(t.days.ToString(), textFieldLabel, GUILayout.Width(40));
                    GUILayout.Label("h", GUILayout.ExpandWidth(false));
                    GUILayout.TextField(t.hours.ToString(), textFieldLabel, GUILayout.Width(40));
                    GUILayout.Label("m", GUILayout.ExpandWidth(false));
                    GUILayout.TextField(t.minutes.ToString(), textFieldLabel, GUILayout.Width(40));
                    GUILayout.Label("s", GUILayout.ExpandWidth(false));
                    GUILayout.TextField(t.seconds.ToString(), textFieldLabel, GUILayout.Width(40));
#endif
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndScrollView();
                GUILayout.EndHorizontal();
            }
           

            // need to add 0.25 here to deal with floating point roundoff.
            // setTimeSelection(DesiredTime + 0.25);
            GUILayout.Label("Desired Time:", GUILayout.ExpandWidth(true));
            GUILayout.BeginHorizontal();
            GUILayout.Label("y", GUILayout.ExpandWidth(false));
            desiredTimeInfo.years = uint.Parse(GUILayout.TextField(desiredTimeInfo.years.ToString(), GUILayout.Width(40)));
            GUILayout.Label("d", GUILayout.ExpandWidth(false));
            desiredTimeInfo.days = uint.Parse(GUILayout.TextField(desiredTimeInfo.days.ToString(), GUILayout.Width(40)));
            GUILayout.Label("h", GUILayout.ExpandWidth(false));
            desiredTimeInfo.hours = uint.Parse(GUILayout.TextField(desiredTimeInfo.hours.ToString(), GUILayout.Width(40)));
            GUILayout.Label("m", GUILayout.ExpandWidth(false));
            desiredTimeInfo.minutes = uint.Parse(GUILayout.TextField(desiredTimeInfo.minutes.ToString(), GUILayout.Width(40)));
            GUILayout.Label("s", GUILayout.ExpandWidth(false));
            desiredTimeInfo.seconds = uint.Parse(GUILayout.TextField(desiredTimeInfo.seconds.ToString(), GUILayout.Width(40)));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            var newTimeSel = GUILayout.HorizontalSlider(timeSel, sliderBeginTime, sliderEndtime);
            if (newTimeSel != timeSel)
            {
                desiredTimeInfo = setTimeSelection(newTimeSel * div);
                timeSel = newTimeSel;
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Start Time Now"))
            {
                startTimeNow = true;
            }
            
            if (GUILayout.Button(endTimeInfo == null? "Set End Time":"Unset End Time") && vessel.patchedConicSolver.maneuverNodes.Any())
            {
                if (endTimeInfo == null)
                {
                    endTimeInfo = desiredTimeInfo;
                }
                else
                    endTimeInfo = null;
            }
            GUILayout.EndHorizontal();
            if (GUILayout.Button("Next Node") && vessel.patchedConicSolver.maneuverNodes.Any())
            {
                startTimeNow = false;
                endTimeInfo = null;
                desiredTimeInfo = setTimeSelection(vessel.patchedConicSolver.maneuverNodes.First().UT);
                GUI.changed = false;
                timeSel = (float)vessel.patchedConicSolver.maneuverNodes.First().UT / div;
            }
            if (GUILayout.Button("Last Node") && vessel.patchedConicSolver.maneuverNodes.Any())
            {
                startTimeNow = false;
                endTimeInfo = null;
                desiredTimeInfo = setTimeSelection(vessel.patchedConicSolver.maneuverNodes.Last().UT);
                GUI.changed = false;
                timeSel = (float)vessel.patchedConicSolver.maneuverNodes.Last().UT / div;
            }

            GUILayout.EndVertical();

            DesiredTime = UT + desiredTimeInfo.years * DaysPerYear * HoursPerDay * 3600.0 +
                desiredTimeInfo.days * HoursPerDay * 3600.0 +
                desiredTimeInfo.hours * 3600.0 +
                desiredTimeInfo.minutes * 60.0 + desiredTimeInfo.seconds;

            GUI.DragWindow();
        }

        private void drawGUI()
        {
           


            
        }

        void DrawPatchIcons(Orbit o)
        {
            while (o != null)
            {
                if (o.ContainsUT(DesiredTime))
                    DrawIcon(o.getPositionAtUT(DesiredTime), ShipIcon);
                o = o.nextPatch;
            }
        }

        void DrawNodeOrbits()
        {
            Orbit o = vessel.orbit;
            foreach (ManeuverNode node in vessel.patchedConicSolver.maneuverNodes)
            {
                if (node != null && node.nextPatch != null)
                {
                    DrawPatchIcons(node.nextPatch);
                }
            }
        }

        void DrawBodyOrbits()
        {
           foreach (CelestialBody body in FlightGlobals.Bodies)
            {

                if (body != null && body.orbit != null)
                {

                    DrawIcon(body.getPositionAtUT(DesiredTime), BodyIcon);

                }
            }
        }

        void DrawIconForAllOrbits()
        {
            if (vessel != null)
            {
                DrawPatchIcons(vessel.orbit); // Icons for all patches of actual vessel orbit
                DrawNodeOrbits(); // Icon(s) for wherever we fall in any maneuver node created orbits
            }
            DrawBodyOrbits(); // Icons for all the celestial bodies
        }

        void DrawIcon(Vector3d position,Texture2D icon)
        {
            GUIStyle styleWarpToButton = new GUIStyle();
            styleWarpToButton.fixedWidth = 32;
            styleWarpToButton.fixedHeight = 32;
            styleWarpToButton.normal.background = icon;

            Vector3d screenPosNode = PlanetariumCamera.Camera.WorldToScreenPoint(ScaledSpace.LocalToScaledSpace(position));

            Rect rectNodeButton = new Rect((Int32)screenPosNode.x-16, (Int32)(Screen.height - screenPosNode.y)-16, 32, 32);
            GUI.Button(rectNodeButton, "", styleWarpToButton);
        }

        private void CreateButtonIcon()
        {
            button = ApplicationLauncher.Instance.AddModApplication(
                () =>
                {
                    if (vessel != null) WindowVisible = true;
                },
                () => WindowVisible = false,
                null,
                null,
                null,
                null,
                ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.MAPVIEW | ApplicationLauncher.AppScenes.TRACKSTATION,
                GameDatabase.Instance.GetTexture("SlingShotter/Textures/icon", false)
                );
        }

    }
}
