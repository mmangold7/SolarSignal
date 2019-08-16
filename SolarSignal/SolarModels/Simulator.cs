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

        private const double BigG = .0002;
        private const int Fps = 240;

        #endregion

        #region ///  Constructors  ///

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

        #endregion

        #region ///  Fields  ///

        private readonly IHubContext<SolarHub, ISolarHub> _hubContext;
        private readonly int FuturesCountIncrementSize = 100;
        private readonly bool _shouldGravitatePlayers = true;
        private bool _alreadyCalculatedPaths;
        private bool _calculatedAtLeastOneFuture;

        #endregion

        #region ///  Properties  ///

        public bool IsPaused { get; private set; } = true;

        public List<Body> Bodies;

        public int AmountOfFuturePositionsToGenerate { get; set; } = 200;

        public bool ShouldCalculateFuturePaths { get; set; } = false;

        public List<Player> Players => Bodies.OfType<Player>().ToList();

        #endregion

        #region ///  Methods  ///

        private IEnumerable<Body> GetBodiesToGravitate()
        {
            if (_shouldGravitatePlayers) return Bodies;

            return Bodies.Where(b => b.GetType() != typeof(Player));
        }

        public async void Simulate()
        {
            while (!IsPaused)
            {
                HandlePlayerInput();
                if (Bodies == null) break;
                foreach (var body in GetBodiesToGravitate()) UpdateBodyPosition(body);
                if (AmountOfFuturePositionsToGenerate > 0 && !_alreadyCalculatedPaths)
                    CalculateFuturePositions();
                else
                    foreach (var body in GetBodiesToGravitate())
                        body.FuturePositions = null;
                await _hubContext.Clients.All.GameState(Bodies, _alreadyCalculatedPaths);
                foreach (var player in Players) ClearInputs(player);
                await Task.Delay(1000 / Fps);
            }
        }

        private void HandlePlayerInput()
        {
            if (Players == null) return;

            if (Players.TrueForAll(
                    p => !p.Input.DownPressed && !p.Input.UpPressed && !p.FuturesIncremented &&
                         !p.FuturesDecremented) &&
                _calculatedAtLeastOneFuture)
                _alreadyCalculatedPaths = true;
            else
                _alreadyCalculatedPaths = false;

            foreach (var player in Players)
            {
                player.Angle -= Convert.ToInt32(player.Input.LeftPressed) * 3;
                player.Angle += Convert.ToInt32(player.Input.RightPressed) * 3;

                if (player.Angle > 360) player.Angle -= 360;
                if (player.Angle < 0) player.Angle += 360;

                var scaleMagnitude =
                    1 / 30f * (Convert.ToInt32(player.Input.UpPressed) - Convert.ToInt32(player.Input.DownPressed));
                var deltaV = Vector2.Multiply(player.AngleVector, scaleMagnitude);

                player.Velocity += deltaV;
            }
        }

        private void ClearInputs(Player player)
        {
            player.Input.LeftPressed = false;
            player.Input.RightPressed = false;
            player.Input.UpPressed = false;
            player.Input.DownPressed = false;
            player.FuturesIncremented = false;
            player.FuturesDecremented = false;
        }

        private void UpdateBodyPosition(Body body)
        {
            //HandleCollisions(body);
            MoveBody(body);
            GravitateBody(body);
        }

        private void HandleCollisions(Body body)
        {
            foreach (var otherBody in Bodies.Where(b => b != body))
            {
                var displacement = otherBody.Position - body.Position;
                var sumOfRadii = body.Radius + otherBody.Radius;

                if (displacement.Length() < sumOfRadii)
                {
                     var positionOffsetHalf = Vector2.Multiply(Vector2.Normalize(displacement),
                        0.5f * Convert.ToSingle(sumOfRadii - displacement.Length()));
                     otherBody.Position += positionOffsetHalf;
                     body.Position -= positionOffsetHalf;
                    var bodyFinalVelocity =
                        Vector2.Multiply(Convert.ToSingle((body.Mass - otherBody.Mass) / (body.Mass + otherBody.Mass)),
                            body.Velocity) +
                        Vector2.Multiply(Convert.ToSingle(2 * otherBody.Mass / (body.Mass + otherBody.Mass)),
                            otherBody.Velocity);
                    var otherBodyVelocityFinal =
                        Vector2.Multiply(Convert.ToSingle(2 * body.Mass / (body.Mass + otherBody.Mass)),
                            body.Velocity) + Vector2.Multiply(
                            Convert.ToSingle((otherBody.Mass - body.Mass) / (body.Mass + otherBody.Mass)),
                            otherBody.Velocity);
                    body.Velocity = bodyFinalVelocity;
                    otherBody.Velocity = otherBodyVelocityFinal;
                }
                else
                {

                }
            }
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
                var acceleration = Vector2.Multiply(Vector2.Normalize(displacement),
                    Convert.ToSingle(BigG * otherBody.Mass / rSquared));
                body.Velocity += acceleration;
            }
        }

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

            for (var i = 0; i < AmountOfFuturePositionsToGenerate; i++)
                foreach (var body in Bodies)
                {
                    UpdateBodyPosition(body);
                    body.FuturePositions.Add(body.Position);
                }

            foreach (var body in Bodies)
            {
                body.Position = originalPositions[body];
                body.Velocity = originalVelocities[body];
            }

            _calculatedAtLeastOneFuture = true;
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

            orbiter.ParentBody = parentBody;

            Bodies.Add(orbiter);

            return orbiter;
        }

        private void AssignCircularOrbitVelocity(Body orbiter, Body parentBody)
        {
            var orbitRadius = Vector2.Distance(orbiter.Position, parentBody.Position);

            var accelerationOfGravity = BigG * parentBody.Mass / Math.Pow(orbitRadius, 2);

            var parentReferenceFrameOrbitingXVelocity =
                Convert.ToSingle(Math.Sqrt(orbitRadius * accelerationOfGravity));

            orbiter.Velocity += new Vector2(parentReferenceFrameOrbitingXVelocity, 0);

            if (parentBody.ParentBody == null) return;

            AssignCircularOrbitVelocity(orbiter, parentBody.ParentBody);
        }

        public void CreatePlayerWithId(string id, string rgbColor)
        {
            Bodies.Add(new Player
            {
                Id = id,
                Name = id,
                Color = rgbColor,
                Mass = 1,
                Radius = 10,
                Position = new Vector2(250, 250),
                Input = new Input
                {
                    UpPressed = false,
                    DownPressed = false,
                    LeftPressed = false,
                    RightPressed = false
                }
            });
        }

        public void DestroyPlayerWithId(string id)
        {
            Bodies.Remove(Players.Single(p => p.Id == id));
        }

        public void Pause()
        {
            IsPaused = true;
        }

        public void Resume()
        {
            IsPaused = false;
            Simulate();
        }

        public void IncreaseFuturesCalculations()
        {
            AmountOfFuturePositionsToGenerate += FuturesCountIncrementSize;
        }

        public void DecreaseFuturesCalculations()
        {
            AmountOfFuturePositionsToGenerate -= FuturesCountIncrementSize;
            if (AmountOfFuturePositionsToGenerate < 0) AmountOfFuturePositionsToGenerate = 0;
        }

        #endregion
    }
}