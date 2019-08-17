using System;
using System.Numerics;

namespace SolarSignal.SolarModels
{
    public class Player : Body
    {
        #region ///  Properties  ///

        public Vector2 AngleVector => Vector2.Normalize(new Vector2(Convert.ToSingle(Math.Cos(Angle * Math.PI / 180))
            , Convert.ToSingle(Math.Sin(Angle * Math.PI / 180))));

        public string Id { get; set; }
        public int Angle { get; set; }
        public bool FuturesIncremented { get; set; }
        public bool FuturesDecremented { get; set; }
        public Input Input { get; set; }
        public DateTime LastShotTime { get; set; }

        #endregion
    }
}