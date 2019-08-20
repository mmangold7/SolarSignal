using System.Collections.Generic;
using System.Numerics;
using Newtonsoft.Json;

namespace SolarSignal.SolarModels
{
    public class Body
    {
        #region ///  Properties  ///

        [JsonIgnore] public List<Body> CollidingWithBodies { get; set; } = new List<Body>();

        //todo:add toggle between controlling angular position and controlling angular acceleration
        //public double AngularVelocity { get; set; }
        public string Color { get; set; }

        public List<Vector2> FuturePositions { get; set; } = new List<Vector2>();
        public float Mass { get; set; }

        public string Name { get; set; }

        //todo:replace this property with local var in orbit method
        [JsonIgnore] public Body ParentBody { get; set; }

        public Vector2 Position { get; set; }
        public float Radius { get; set; }
        public Vector2 Velocity { get; set; }

        #endregion
    }
}