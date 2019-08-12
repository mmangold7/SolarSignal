using System.Drawing;
using Newtonsoft.Json;

namespace SolarSignal.SolarModels
{
    public class Body
    {
        #region ///  Properties  ///

        public string Name { get; set; }
        public double Mass { get; set; }
        public double Radius { get; set; }
        public double XPosition { get; set; }
        public double XVelocity { get; set; }
        public double YPosition { get; set; }
        public double YVelocity { get; set; }
        //todo:add toggle between controlling angular position and controlling angular acceleration
        //public double AngularVelocity { get; set; }
        public string Color { get; set; }
        [JsonIgnore]
        public Body ParentBody { get; set; }

        #endregion
    }
}