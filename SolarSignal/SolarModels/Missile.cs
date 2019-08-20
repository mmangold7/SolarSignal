using System;

namespace SolarSignal.SolarModels
{
    public class Missile : Body
    {
        #region ///  Properties  ///

        public DateTime CreatedAt { get; set; }

        public float Damage { get; set; }

        public float InitialSpeed { get; set; }

        #endregion
    }
}