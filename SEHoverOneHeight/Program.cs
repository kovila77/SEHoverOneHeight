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
        List<IMyGyro> gyros;
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
        float changedHoverHeight;
        float newHoverHeight;

        bool show = true;
        bool makeWork = false;

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

            if (!bool.TryParse(Storage, out show))
            {
                show = true;
            }

            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }

        public void Save()
        {
            Storage = show.ToString();
        }

        public void Main(string argument, UpdateType updateSource)
        {
            if (updateSource == UpdateType.Update10)
            {
                if (makeWork)
                {
                    CompensateHeight();
                }
                else if (show)
                {
                    Show();
                }
            }
            else
            {
                if (argument == "Up")
                {
                    IncreaseHeversHeight();
                }
                else if (argument == "Down")
                {
                    DecreaseHeversHeight();
                }
                else if (argument == "Start")
                {
                    dCenterHeight = (myCockpit.GetPosition() - centerOfPlanet).Length();
                    changedHoverHeight = hoverHeight = hovers[0].GetValueFloat("Hover_MinHeight");

                    foreach (IMyThrust thr in hovers)
                    {
                        thr.SetValueBool("Hover_AutoAltitude", true);
                    }

                    makeWork = true;
                }
                else if (argument == "Stop")
                {
                    makeWork = false;
                }
                else if (argument == "Show")
                {
                    show = !show;
                }
                //else if (argument == "StartHorizontal")
                //{

                //}
                //else if (argument == "StopHorizontal")
                //{

                //}
            }
        }

        //void Setup()
        //{
        //    var l = new List<IMyTerminalBlock>();

        //    _rc = (IMyRemoteControl)GridTerminalSystem.GetBlockWithName(REMOTE_CONTROL_NAME ?? "");
        //    if (_rc == null)
        //    {
        //        GridTerminalSystem.GetBlocksOfType<IMyRemoteControl>(l, x => x.CubeGrid == Me.CubeGrid);
        //        _rc = (IMyRemoteControl)l[0];
        //    }

        //    GridTerminalSystem.GetBlocksOfType<IMyGyro>(l, x => x.CubeGrid == Me.CubeGrid);
        //    gyros = l.ConvertAll(x => (IMyGyro)x);

        //    if (gyros.Count > LIMIT_GYROS)
        //        gyros.RemoveRange(LIMIT_GYROS, gyros.Count - LIMIT_GYROS);
        //}

        private void IncreaseHeversHeight()
        {
            if (makeWork)
            {
                float dif = 2f;
                if (changedHoverHeight < 5)
                {
                    dif = 0.5f;
                }
                else if (changedHoverHeight < 20)
                {
                    dif = 1f;
                }

                changedHoverHeight = changedHoverHeight + dif;
                SetHoversMinHeight(changedHoverHeight + dif);
            }
            else
            {
                foreach (IMyTerminalBlock hover in hovers)
                {
                    hover.ApplyAction("MinAlt_Increase");
                }
            }
        }

        private void DecreaseHeversHeight()
        {
            if (makeWork)
            {
                float dif = 2f;
                if (changedHoverHeight < 5)
                {
                    dif = 0.5f;
                }
                else if (changedHoverHeight < 20)
                {
                    dif = 1f;
                }

                changedHoverHeight = changedHoverHeight - dif;
                SetHoversMinHeight(changedHoverHeight - dif);
            }
            else
            {
                foreach (IMyTerminalBlock hover in hovers)
                {
                    hover.ApplyAction("MinAlt_Decrease");
                }
            }
        }

        private void CompensateHeight()
        {
            newCenterHeight = (myCockpit.GetPosition() - centerOfPlanet).Length();
            dif = dCenterHeight - newCenterHeight;
            newHoverHeight = changedHoverHeight + (float)dif;

            if (Math.Abs(dif) > eps)
            {
                SetHoversMinHeight(newHoverHeight);
            }

            Show();
        }

        private void SetHoversMinHeight(float h)
        {
            h = Math.Max(minHoversHeight, Math.Min(maxHoversHeight, h));

            foreach (IMyTerminalBlock hover in hovers)
            {
                hover.SetValueFloat("Hover_MinHeight", h);
            }
        }

        private void Show()
        {
            lcd.WriteText("Start hover height:    " + hoverHeight + "\n" +
                          "Changed start hov. h.: " + changedHoverHeight + "\n" +
                          "Current hover0 h.:     " + hovers[0].GetValueFloat("Hover_MinHeight") + "\n" +
                          "New comput. hover h.:  " + newHoverHeight + "\n" +
                          "---\n" +
                          "Start planet height:   " + dCenterHeight + "\n" +
                          "Current planet height: " + newCenterHeight + "\n" +
                          "Difference abs. h.:    " + dif + "\n" +
                          "Min Hover_MinHeight:   " + minHoversHeight + "\n" +
                          "Max Hover_MinHeight:   " + maxHoversHeight + "\n"
                );
        }
    }
}
