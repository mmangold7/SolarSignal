using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using SolarSignal.Hubs;

namespace SolarSignal.SolarModels
{
    public class Simulator
    {
        #region ///  Constants  ///

        private const double BigG = .001;

        #endregion

        #region ///  Fields  ///

        public List<Body> Bodies;

        private readonly Random _playerIdGenerator = new Random();

        public List<Player> Players => Bodies.OfType<Player>().ToList();

        private bool _paused;

        private readonly IHubContext<SolarHub, ISolarHub> _hubContext;

        public Simulator(IServiceProvider serviceProvider)
        {
            _hubContext = serviceProvider.GetService<IHubContext<SolarHub, ISolarHub>>();

            Bodies = new List<Body>();
        }

        public Simulator(IHubContext<SolarHub, ISolarHub> context, IEnumerable<Body> bodies)
        {
            _hubContext = context;

            Bodies = bodies.ToList();
        }

        public int CreatePlayerAndReturnId()
        {
            var newId = _playerIdGenerator.Next(0, int.MaxValue);

            Bodies.Add(new Player
            {
                Id = newId,
                Color = "purple",
                Mass = 1,
                Radius = 10,
                XPosition = 100,
                YPosition = 100
            });

            return newId;
        }

        #endregion

        #region ///  Methods  ///

        public void Pause()
        {
            _paused = true;
        }

        public void Resume()
        {
            _paused = false;
            Simulate();
        }

        public async void Simulate()
        {
            while (!_paused)
            {
                HandlePlayerInput();
                MoveBodies();
                GravitateBodies();
                await _hubContext.Clients.All.GameState(Bodies);
                await Task.Delay(1000 / 60);
            }
        }

        private void AssignCircularOrbitVelocity(Body orbiter, Body parentBody)
        {
            var orbitRadius = Math.Sqrt(Math.Pow(parentBody.XPosition - orbiter.XPosition, 2) +
                                        Math.Pow(parentBody.YPosition - orbiter.YPosition, 2));

            var accelerationOfGravity = BigG * parentBody.Mass / Math.Pow(orbitRadius, 2);

            var parentReferenceFrameOrbitingXVelocity = Math.Sqrt(orbitRadius * accelerationOfGravity);

            orbiter.XVelocity += parentReferenceFrameOrbitingXVelocity;

            if (parentBody.ParentBody == null) return;

            AssignCircularOrbitVelocity(orbiter, parentBody.ParentBody);
        }

        public Body CreateCircularOrbiterOf(Body parentBody, double orbitRadius, double mass, double radius,
            string color, string name)
        {
            var orbiter = new Body
            {
                Name = name,
                Mass = mass,
                Radius = radius,
                Color = color,
                XPosition = parentBody.XPosition,
                YPosition = parentBody.YPosition + orbitRadius,
                XVelocity = 0
            };

            AssignCircularOrbitVelocity(orbiter, parentBody);

            Bodies.Add(orbiter);

            orbiter.ParentBody = parentBody;

            return orbiter;
        }

        private bool _shouldGravitatePlayers = false;

        private IEnumerable<Body> GetGravitatableBodies()
        {
            if (_shouldGravitatePlayers)
            {
                return Bodies;
            }

            return Bodies.Where(b => b.GetType() != typeof(Player));
        }

        private void GravitateBodies()
        {
            if (Bodies == null) return;

            foreach (var body in Bodies)
            foreach (var otherBody in Bodies.Where(b => b != body))
            {
                var xDisplacement = otherBody.XPosition - body.XPosition;
                var yDisplacement = otherBody.YPosition - body.YPosition;

                var rSquared = Math.Pow(xDisplacement, 2) + Math.Pow(yDisplacement, 2);
                var theta = Math.Atan2(yDisplacement, xDisplacement);

                var bodyXDeltaV = BigG * otherBody.Mass / rSquared * Math.Cos(theta);
                var bodyYDeltaV = BigG * otherBody.Mass / rSquared * Math.Sin(theta);

                body.XVelocity += bodyXDeltaV;
                body.YVelocity += bodyYDeltaV;
            }
        }

        private void MoveBodies()
        {
            if (Bodies == null) return;

            foreach (var body in Bodies)
            {
                body.XPosition += body.XVelocity;
                body.YPosition += body.YVelocity;
            }
        }

        private void ClearInputs(Player player)
        {
            player.LeftPressed = false;
            player.RightPressed = false;
            player.UpPressed = false;
            player.DownPressed = false;
        }

        private void HandlePlayerInput()
        {
            if (Players == null) return;

            foreach (var player in Players)
            {
                player.Angle -= Convert.ToInt32(player.LeftPressed)*2;
                player.Angle += Convert.ToInt32(player.RightPressed)*2;

                if (player.Angle > 360) player.Angle -= 360;

                if (player.Angle < 0) player.Angle += 360;

                var upPressed = player.UpPressed;
                var downPressed = player.DownPressed;

                if (upPressed && downPressed)
                {
                    ClearInputs(player);
                    return;
                }

                var scaleMagnitude = 2 / 30.0;

                var xUnitVector = Math.Cos(player.Angle * Math.PI / 180);
                var yUnitVector = Math.Sin(player.Angle * Math.PI / 180);

                if (upPressed)
                {
                    player.XVelocity += scaleMagnitude * xUnitVector;
                    player.YVelocity += scaleMagnitude * yUnitVector;
                }
                else if (downPressed)
                {
                    player.XVelocity -= scaleMagnitude * xUnitVector;
                    player.YVelocity -= scaleMagnitude * yUnitVector;
                }

                ClearInputs(player);
            }
        }

        #endregion
    }
}