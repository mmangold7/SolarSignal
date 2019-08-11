namespace SolarSignal.SolarModels
{
    public class Player : Body
    {
        #region ///  Properties  ///

        public string Id { get; set; }
        public int Angle { get; set; }
        public bool DownPressed { get; set; }
        public bool LeftMousePressed { get; set; }
        public bool LeftPressed { get; set; }
        public bool RightMousePressed { get; set; }
        public bool RightPressed { get; set; }
        public bool UpPressed { get; set; }

        #endregion
    }
}