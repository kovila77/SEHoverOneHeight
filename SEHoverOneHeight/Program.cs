using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRage;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        const string COCKPIT_NAME = "MainCockpit";
        double eps = 0.001;

        List<IMyTerminalBlock> tmpLst;
        List<IMyThrust> hovers;
        IMyCockpit myCockpit;
        IMyTextSurface lcd;

        //GPS:CenterOfPlanet:217403:238111:-264844:#FF75C9F1:
        Vector3D centerOfPlanet = new Vector3D(217403f, 238111f, -264844f);
        double dCenterHeight;
        double newCenterHeight;
        double dif;

        float minHoversHeight;
        float maxHoversHeight;

        float hoverHeight;
        float newHoverHeight;

        public Program()
        {
            tmpLst = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyCockpit>(tmpLst, (b) => (b.IsSameConstructAs(Me) && b.CustomName.Contains(COCKPIT_NAME)));
            if (tmpLst.Count > 0)
            {
                myCockpit = tmpLst[0] as IMyCockpit;
                lcd = myCockpit.GetSurface(4);
            }
            else
                Echo("No Main Cockpit");

            hovers = new List<IMyThrust>();
            GridTerminalSystem.GetBlocksOfType<IMyThrust>(hovers, (b) =>
            (b.IsSameConstructAs(Me) &&
            (b.Orientation.Forward == Base6Directions.GetOppositeDirection(myCockpit.Orientation.Up))));

            minHoversHeight = hovers[0].GetMinimum<float>("Hover_MinHeight");
            maxHoversHeight = hovers[0].GetMaximum<float>("Hover_MinHeight");

            lcd.WriteText("...");
        }

        public void Save()
        {

        }

        public void Main(string argument, UpdateType updateSource)
        {
            if (updateSource == UpdateType.Update10)
            {
                CompensateHeight();
            }
            else
            {
                if (argument == "Start")
                {
                    Runtime.UpdateFrequency = UpdateFrequency.Update10;
                    dCenterHeight = (myCockpit.GetPosition() - centerOfPlanet).Length();
                    hoverHeight = hovers[0].GetValueFloat("Hover_MinHeight");

                    foreach (IMyThrust thr in hovers)
                    {
                        thr.SetValueBool("Hover_AutoAltitude", true);
                    }
                }
                else if (argument == "Stop")
                {
                    Runtime.UpdateFrequency = UpdateFrequency.None;
                }
            }
        }

        private void CompensateHeight()
        {
            newCenterHeight = (myCockpit.GetPosition() - centerOfPlanet).Length();
            dif = dCenterHeight - newCenterHeight;
            newHoverHeight = hoverHeight + (float)dif;

            if (Math.Abs(dif) > eps)
            {
                SetHoversMinHeight(newHoverHeight);
            }

            lcd.WriteText("Start height: " + dCenterHeight + "\n"
                + "Start hover height: " + hoverHeight + "\n"
                + "Current height: " + newCenterHeight + "\n"
                + "Difference: " + dif + "\n"
                + "Current hover height: " + hovers[0].GetValueFloat("Hover_MinHeight") + "\n"
                + "New hover height: " + newHoverHeight + "\n"
                + "Min Hover_MinHeight: " + minHoversHeight + "\n"
                + "Max Hover_MinHeight: " + maxHoversHeight + "\n"
                );
        }

        public void SetHoversMinHeight(float h)
        {
            h = Math.Max(minHoversHeight, Math.Min(maxHoversHeight, h));

            foreach (IMyTerminalBlock hover in hovers)
            {
                hover.SetValueFloat("Hover_MinHeight", h);
            }
        }
    }
}
