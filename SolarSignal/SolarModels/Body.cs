using System.Collections.Generic;
using System.Numerics;
using Newtonsoft.Json;

namespace SolarSignal.SolarModels
{
    public class Body
    {
        #region ///  Properties  ///

        public string Name { get; set; }
        public double Mass { get; set; }
        public float Radius { get; set; }

        public Vector2 Position { get; set; }
        public Vector2 Velocity { get; set; }

        //todo:add toggle between controlling angular position and controlling angular acceleration
        //public double AngularVelocity { get; set; }
        public string Color { get; set; }

        //todo:replace this property with local var in orbit method
        [JsonIgnore] public Body ParentBody { get; set; }

        public List<Vector2> FuturePositions { get; set; } = new List<Vector2>();

        #endregion
    }
}