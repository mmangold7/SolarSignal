using System;
using System.Numerics;

namespace SolarSignal.SolarModels
{
    public class Player : Body
    {
        #region ///  Properties  ///

        public int Angle { get; set; }

        public Vector2 AngleVector => Vector2.Normalize(new Vector2(Convert.ToSingle(Math.Cos(Angle * Math.PI / 180)), Convert.ToSingle(Math.Sin(Angle * Math.PI / 180))));

        public bool FuturesDecremented { get; set; }
        public bool FuturesIncremented { get; set; }

        public string Id { get; set; }
        public Input Input { get; set; }
        public DateTime LastShotTime { get; set; }
        public float ShieldHealth { get; set; } = 100;

        #endregion
    }
}