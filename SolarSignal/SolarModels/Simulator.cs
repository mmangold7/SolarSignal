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
        private const int Fps = 90;

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

        public bool IsPaused { get; private set; }

        public List<Body> Bodies;

        public int AmountOfFuturePositionsToGenerate { get; set; } = 200;

        public bool ShouldCalculateFuturePaths { get; set; } = false;

        public List<Player> Players => Bodies.OfType<Player>().ToList();

        public List<Missile> Missiles => Bodies.OfType<Missile>().ToList();

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
                if (Bodies == null) break;
                HandlePlayerInput();
                foreach (var body in GetBodiesToGravitate()) UpdateBodyPosition(body);
                foreach (var player in Players) HandleDamage(player);
                if (AmountOfFuturePositionsToGenerate > 0 && !_alreadyCalculatedPaths && ShouldCalculateFuturePaths)
                    CalculateFuturePositions();
                else
                    foreach (var body in GetBodiesToGravitate())
                        body.FuturePositions = null;
                RemoveOldMissiles();
                await _hubContext.Clients.All.GameState(Bodies, _alreadyCalculatedPaths);
                foreach (var player in Players) ClearInputs(player);
                await Task.Delay(1000 / Fps);
            }
        }

        private void RemoveOldMissiles()
        {
            Bodies.RemoveAll(b => b is Missile m && DateTime.Now - m.CreatedAt > TimeSpan.FromSeconds(3));
        }

        private void HandlePlayerInput()
        {
            if (Players == null) return;

            //If there is no player input, there is no need to re-calculate futures since they are still determined correct
            if (Players.TrueForAll(
                    p => !p.Input.DownPressed && !p.Input.UpPressed && !p.FuturesIncremented &&
                         !p.FuturesDecremented && !p.Input.ShootPressed) &&
                _calculatedAtLeastOneFuture)
                _alreadyCalculatedPaths = true;
            else
                _alreadyCalculatedPaths = false;

            foreach (var player in Players)
            {
                //ship rotation
                player.Angle -= Convert.ToInt32(player.Input.LeftPressed) * 5;
                player.Angle += Convert.ToInt32(player.Input.RightPressed) * 5;

                if (player.Angle > 360) player.Angle -= 360;
                if (player.Angle < 0) player.Angle += 360;

                //ship acceleration
                var scaleMagnitude =
                    3 / 30f * (Convert.ToInt32(player.Input.UpPressed) - Convert.ToInt32(player.Input.DownPressed));
                var deltaV = player.AngleVector * scaleMagnitude;

                //todo:limit player deltav based on their speed
                //if player is going too fast and trying to go faster, project their deltav onto the vectors perpendicular to player.Velocity
                //if (player.Velocity.Length() > MaxSpeed && Vector2.Dot(deltaV, Vector2.Normalize(player.Velocity)) > 0)
                //{
                //    //var perpendicularVector = Math.Atan2(player.Velocity.Y - player.AngleVector.Y,
                //    //                              player.Velocity.X - player.AngleVector.X) >
                //    //                          0
                //    //    ? new Vector2(-player.Velocity.X, player.Velocity.Y)
                //    //    : new Vector2(player.Velocity.X, -player.Velocity.Y);

                //    //var unitPerpendicularVector = Vector2.Normalize(perpendicularVector);

                //    //deltaV = Vector2.Multiply(Vector2.Dot(deltaV, unitPerpendicularVector), unitPerpendicularVector);
                //}

                player.Velocity += deltaV;

                //ship weapons
                if (player.Input.ShootPressed && DateTime.Now - player.LastShotTime > TimeSpan.FromMilliseconds(100))
                {
                    var shotTime = DateTime.Now;

                    var newMissile = new Missile
                    {
                        ParentBody = player,
                        Damage = 10,
                        Mass = 10f,
                        Radius = 2,
                        Color = "orange",
                        Position = player.Position + (2 * player.Radius + 2f) * player.AngleVector,
                        Velocity = player.AngleVector * 3 + player.Velocity,
                        CreatedAt = shotTime,
                        Name = $"Missile {shotTime.Ticks}"
                    };

                    Bodies.Add(newMissile);

                    player.LastShotTime = shotTime;

                    player.Velocity -= newMissile.Velocity * newMissile.Mass / player.Mass;
                }
            }
        }

        //private const float MaxSpeed = 5f;

        private void ClearInputs(Player player)
        {
            player.Input.LeftPressed = false;
            player.Input.RightPressed = false;
            player.Input.UpPressed = false;
            player.Input.DownPressed = false;
            player.Input.ShootPressed = false;
            player.FuturesIncremented = false;
            player.FuturesDecremented = false;
        }

        private void UpdateBodyPosition(Body body)
        {
            HandleCollisions(body);
            MoveBody(body);
            GravitateBody(body);
        }

        private void HandleDamage(Player player)
        {
            var damageEligibleMissiles = Missiles.Where(m =>
                Vector2.Distance(player.Position, m.Position) < player.Radius * 2 + m.Radius && m.ParentBody != player);
            foreach (var damageEligibleMissile in damageEligibleMissiles)
            {
                player.ShieldHealth -= damageEligibleMissile.Damage;
                Bodies.Remove(damageEligibleMissile);
            }

            if (player.ShieldHealth <= 0)
            {
                player.Position = GetSuitableStartPosition();
                player.ShieldHealth = 100;
                player.Velocity = new Vector2(0, 0);
                player.Angle = 0;
            }
        }

        private void HandleCollisions(Body body)
        {
            //don't collide
            var collisionEligibleBodies = Bodies.Where(otherBody =>
                otherBody != body && //bodies with themselves
                (otherBody.ParentBody != body && otherBody is Missile ||
                 body.ParentBody != otherBody && body is Missile ||
                 !(body is Missile) && !(otherBody is Missile)) && //players with their own bullets
                !(body is Missile && otherBody is Missile)).ToList(); //bullets with other bullets

            foreach (var otherBody in collisionEligibleBodies)
            {
                //players radii is their shield radius
                var displacement = otherBody.Position - body.Position;
                var distance = displacement.Length();
                var bodyRadius = body is Player player ? player.Radius * 2 : body.Radius;
                var otherBodyRadius = otherBody is Player otherPlayer ? otherPlayer.Radius * 2 : otherBody.Radius;
                var sumOfRadii = bodyRadius + otherBodyRadius;

                //if they are in colliding range
                if (distance < sumOfRadii && !body.CollidingWithBodies.Contains(otherBody))
                {
                    var initialMomentum = body.Mass * body.Velocity + otherBody.Mass * otherBody.Velocity;
                    var totalMass = body.Mass + otherBody.Mass;

                    var bodyFinalVelocity =
                        (initialMomentum +
                         otherBody.Mass * (otherBody.Velocity - body.Velocity)) / totalMass;
                    var otherBodyFinalVelocity =
                        (initialMomentum +
                         body.Mass * (body.Velocity - otherBody.Velocity)) / totalMass;

                    body.Velocity = bodyFinalVelocity * Vector2.Normalize(body.Position - otherBody.Position);
                    otherBody.Velocity = otherBodyFinalVelocity * Vector2.Normalize(otherBody.Position - body.Position);

                    body.CollidingWithBodies.Add(otherBody);
                    otherBody.CollidingWithBodies.Add(body);
                }

                if (body.CollidingWithBodies.Contains(otherBody) && distance > sumOfRadii)
                {
                    body.CollidingWithBodies.Remove(otherBody);
                    otherBody.CollidingWithBodies.Remove(body);
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
                var acceleration = Vector2.Normalize(displacement) *
                                   Convert.ToSingle(BigG * otherBody.Mass / rSquared);
                body.Velocity += acceleration;
            }
        }

        private void CalculateFuturePositions()
        {
            var originalPositions = new Dictionary<Body, Vector2>();
            var originalVelocities = new Dictionary<Body, Vector2>();

            var bodiesToMakeFuturesFor = Bodies.Except(Missiles).ToList();

            foreach (var body in bodiesToMakeFuturesFor)
            {
                originalPositions.Add(body, body.Position);
                originalVelocities.Add(body, body.Velocity);
                body.FuturePositions = new List<Vector2>();
            }

            for (var i = 0; i < AmountOfFuturePositionsToGenerate; i++)
                foreach (var body in bodiesToMakeFuturesFor)
                {
                    UpdateBodyPosition(body);
                    body.FuturePositions.Add(body.Position);
                }

            foreach (var body in bodiesToMakeFuturesFor)
            {
                body.Position = originalPositions[body];
                body.Velocity = originalVelocities[body];
            }

            _calculatedAtLeastOneFuture = true;
        }

        public Body CreateCircularOrbiterOf(Body parentBody, float orbitRadius, float mass, float radius,
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

        private Vector2 GetRandomVector()
        {
            var randomGenerator = new Random();
            return new Vector2(randomGenerator.Next(-200, 200), randomGenerator.Next(-200, 200));
        }

        private Vector2 GetSuitableStartPosition()
        {
            var startPosition = GetRandomVector();

            while (Bodies.Any(b => (b.Position - startPosition).Length() < 20 + b.Radius))
                startPosition = GetRandomVector();

            return startPosition;
        }

        public void CreatePlayerWithId(string id, string rgbColor)
        {
            Bodies.Add(new Player
            {
                Id = id,
                Name = id,
                Color = rgbColor,
                Mass = 250,
                Radius = 10,
                Position = GetSuitableStartPosition(),
                Input = new Input
                {
                    UpPressed = false,
                    DownPressed = false,
                    LeftPressed = false,
                    RightPressed = false,
                    ShootPressed = false
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