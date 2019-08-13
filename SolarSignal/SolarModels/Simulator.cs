using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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

        public int FuturePositionsCount { get; set; } = 200;
        public bool ShouldCalculateFuturePaths { get; set; } = false;

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

        public void CreatePlayerWithId(string id)
        {
            Bodies.Add(new Player
            {
                Id = id,
                Color = "purple",
                Mass = 1,
                Radius = 10,
                Position = new Vector2(200, 200)
            });
        }

        public void DestroyPlayerWithId(string id)
        {
            Bodies.Remove(Players.Single(p => p.Id == id));
        }

        #endregion

        #region ///  Methods  ///

        public void Pause()
        {
            _paused = true;
        }

        private readonly int FuturesCountIncrementSize = 50;

        public void IncreaseFuturesCalculations()
        {
            FuturePositionsCount += FuturesCountIncrementSize;
        }

        public void DecreaseFuturesCalculations()
        {
            FuturePositionsCount -= FuturesCountIncrementSize;
            if (FuturePositionsCount < 0) FuturePositionsCount = 0;
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
                if (Bodies == null) break;
                foreach (var body in GetGravitatableBodies()) UpdateBodyPosition(body);
                if (ShouldCalculateFuturePaths && FuturePositionsCount > 0 && !_alreadyCalculatedPaths)
                    CalculateFuturePositions();
                await _hubContext.Clients.All.GameState(Bodies);
                foreach (var player in Players) ClearInputs(player);
                await Task.Delay(1000 / 60);
            }
        }

        private void UpdateBodyPosition(Body body)
        {
            GravitateBody(body);
            MoveBody(body);
        }

        private void MoveBody(Body body)
        {
            body.Position += body.Velocity;
        }

        private void GravitateBody(Body body)
        {
            foreach (var otherBody in Bodies.Where(b => b != body))
            {
                var displacement = otherBody.Position - body.Position;
                var rSquared = displacement.LengthSquared();

                var theta = Math.Atan2(displacement.Y, displacement.X);

                //var deltaV = BigG * otherBody.Mass / rSquared;

                var bodyXDeltaV = BigG * otherBody.Mass / rSquared * Math.Cos(theta);
                var bodyYDeltaV = BigG * otherBody.Mass / rSquared * Math.Sin(theta);

                body.Velocity += new Vector2(Convert.ToSingle(bodyXDeltaV), Convert.ToSingle(bodyYDeltaV));
            }
        }

        private bool _alreadyCalculatedPaths;

        //turn the code that updates position into a position-updater function that can be used in the simulation to get each future position vector
        private void CalculateFuturePositions()
        {
            var originalPositions = new Dictionary<Body, Vector2>();
            var originalVelocities = new Dictionary<Body, Vector2>();

            foreach (var body in Bodies)
            {
                originalPositions.Add(body, body.Position);
                originalVelocities.Add(body, body.Velocity);
                body.FuturePositions = new List<Vector2>();
            }

            for (var i = 0; i < FuturePositionsCount; i++)
                foreach (var body in Bodies)
                {
                    UpdateBodyPosition(body);
                    body.FuturePositions.Add(body.Position);
                }

            foreach (var body in Bodies) body.Position = originalPositions[body];
            foreach (var body in Bodies) body.Velocity = originalVelocities[body];

            _calculatedAtLeastOneFuture = true;
        }

        private void AssignCircularOrbitVelocity(Body orbiter, Body parentBody)
        {
            var orbitRadius = Vector2.Distance(orbiter.Position, parentBody.Position);

            var accelerationOfGravity = BigG * parentBody.Mass / Math.Pow(orbitRadius, 2);

            var parentReferenceFrameOrbitingXVelocity =
                Convert.ToSingle(Math.Sqrt(orbitRadius * accelerationOfGravity));

            orbiter.Velocity = new Vector2(parentReferenceFrameOrbitingXVelocity, 0);

            if (parentBody.ParentBody == null) return;

            AssignCircularOrbitVelocity(orbiter, parentBody.ParentBody);
        }

        public Body CreateCircularOrbiterOf(Body parentBody, float orbitRadius, double mass, double radius,
            string color, string name)
        {
            var orbiter = new Body
            {
                Name = name,
                Mass = mass,
                Radius = radius,
                Color = color,
                Position = new Vector2(parentBody.Position.X, parentBody.Position.Y + orbitRadius),
                Velocity = new Vector2(0, 0)
            };

            AssignCircularOrbitVelocity(orbiter, parentBody);

            Bodies.Add(orbiter);

            orbiter.ParentBody = parentBody;

            return orbiter;
        }

        private readonly bool _shouldGravitatePlayers = true;
        private bool _calculatedAtLeastOneFuture;

        private IEnumerable<Body> GetGravitatableBodies()
        {
            if (_shouldGravitatePlayers) return Bodies;

            return Bodies.Where(b => b.GetType() != typeof(Player));
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

            if (Players.TrueForAll(
                    p => !p.DownPressed && !p.UpPressed && !p.FuturesIncremented && !p.FuturesDecremented) &&
                _calculatedAtLeastOneFuture)
                _alreadyCalculatedPaths = true;
            else
                _alreadyCalculatedPaths = false;

            foreach (var player in Players)
            {
                player.Angle -= Convert.ToInt32(player.LeftPressed) * 5;
                player.Angle += Convert.ToInt32(player.RightPressed) * 5;

                if (player.Angle > 360) player.Angle -= 360;

                if (player.Angle < 0) player.Angle += 360;

                var upPressed = player.UpPressed;
                var downPressed = player.DownPressed;

                if (upPressed && downPressed)
                {
                    ClearInputs(player);
                    return;
                }

                var scaleMagnitude = 5 / 30f;
                var deltaV = Vector2.Multiply(player.AngleVector, scaleMagnitude);

                if (upPressed)
                    player.Velocity += deltaV;
                else if (downPressed) player.Velocity -= deltaV;
            }
        }

        #endregion
    }
}