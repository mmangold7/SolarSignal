using System;
using System.Numerics;

namespace SolarSignal.SolarModels
{
    public class Player : Body
    {
        #region ///  Properties  ///

        public Vector2 AngleVector => new Vector2(Convert.ToSingle(Math.Cos(Angle * Math.PI / 180))
            , Convert.ToSingle(Math.Sin(Angle * Math.PI / 180)));

        public string Id { get; set; }
        public int Angle { get; set; }
        public bool DownPressed { get; set; }
        public bool LeftMousePressed { get; set; }
        public bool LeftPressed { get; set; }
        public bool RightMousePressed { get; set; }
        public bool RightPressed { get; set; }
        public bool UpPressed { get; set; }
        public bool FuturesIncremented { get; set; }
        public bool FuturesDecremented { get; set; }

        #endregion
    }
}