using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
                MoveBodies();
                GravitateBodies();
                await _hubContext.Clients.All.GameState(Bodies);
                Thread.Sleep(1000 / 60);
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

        #endregion
    }
}