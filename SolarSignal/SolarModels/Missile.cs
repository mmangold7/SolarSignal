using System;
using System.Collections.Generic;
using System.Numerics;
using Newtonsoft.Json;

namespace SolarSignal.SolarModels
{
    public class Missile : Body
    {
        #region ///  Properties  ///

        public float Damage { get; set; }

        public float InitialSpeed { get; set; }

        public DateTime CreatedAt { get; set; }

        #endregion
    }
}