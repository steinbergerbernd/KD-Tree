using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

using Waypoint = System.Tuple<Microsoft.Xna.Framework.Vector3, Microsoft.Xna.Framework.Quaternion, float>;

namespace CameraLib
{
	public class CameraTracker : GameComponent
	{
		public ICamera Camera { get; set; }
		public List<Waypoint> Waypoints { get; private set; }

		public float Tension { get; set; }
		public float Bias { get; set; }
		public float Continuity { get; set; }

		float current;
		float total;

		public CameraTracker(Game game, ICamera camera) : base(game)
		{
			Camera = camera;

			Tension = 0;
			Bias = 0;
			Continuity = 0;

			Waypoints = new List<Waypoint>();

			float time = 0.0f;
			float delta = 3.0f;

			Waypoints.Add(CreateWaypoint(new Vector3(10.0f, 5.0f, 10.0f), new Vector3(0.0f, 0.0f, 0.0f), time += delta));
			Waypoints.Add(CreateWaypoint(new Vector3(10.0f, 3.0f, -10.0f), new Vector3(0.0f, 3.0f, 0.0f), time += delta));
			Waypoints.Add(CreateWaypoint(new Vector3(-10.0f, 5.0f, -10.0f), new Vector3(0.0f, 0.0f, 0.0f), time += delta));
			Waypoints.Add(CreateWaypoint(new Vector3(-10.0f, 3.0f, 10.0f), new Vector3(0.0f, 3.0f, 0.0f), time += delta));
			Waypoints.Add(CreateWaypoint(new Vector3(-15.0f, 10.0f, 15.0f), new Vector3(5.0f, 0.0f, 0.0f), time += delta));
			Waypoints.Add(CreateWaypoint(new Vector3(-15.0f, 10.0f, -15.0f), new Vector3(0.0f, 0.0f, 5.0f), time += delta));
			Waypoints.Add(CreateWaypoint(new Vector3(1.0f, 5.0f, -1.0f), new Vector3(0.0f, 0.0f, 0.0f), time += delta));
			Waypoints.Add(CreateWaypoint(new Vector3(15.0f, 10.0f, 15.0f), new Vector3(0.0f, 0.0f, 0.0f), time += delta));
			Waypoints.Add(CreateWaypoint(new Vector3(10.0f, 5.0f, 0.0f), new Vector3(0.0f, 0.0f, 0.0f), time += delta));
			Waypoints.Add(CreateWaypoint(new Vector3(0.0f, 0.2f, 0.0f), new Vector3(-1.0f, 0.2f, 0.0f), time += delta));
			Waypoints.Add(CreateWaypoint(new Vector3(-10.0f, 5.0f, 0.0f), new Vector3(0.0f, 0.0f, 0.0f), time += delta));
		}


		public override void Initialize()
		{
			base.Initialize();
		}


		public override void Update(GameTime gameTime)
		{
			current += (float)gameTime.ElapsedGameTime.TotalSeconds;

			total = Waypoints.Last().Item3;

			if (current > total)
				current = current % total;

			int index = GetWaypointIndex(current);

			Waypoint last = GetWaypoint(index-1);
			Waypoint begin = GetWaypoint(index);
			Waypoint end = GetWaypoint(index+1);

			Vector3 beginTangent = GetTangent(index, Tension, Bias, Continuity);
			Vector3 endTangent = GetTangent(index+1, Tension, Bias, -Continuity);

			float t0 = last.Item3;
			float t1 = begin.Item3;

			if (t0 > t1)
				t0 -= total;

			float amount = (current - t0) / (t1 - t0);

			Camera.Position = Vector3.Hermite(begin.Item1, beginTangent, end.Item1, endTangent, amount);
			Camera.Look(Vector3.Transform(Vector3.Forward, Quaternion.Slerp(begin.Item2, end.Item2, amount)));

			//Camera.LookAt(Vector3.Up);

			base.Update(gameTime);
		}


		private Waypoint CreateWaypoint(Vector3 position, Vector3 target, float time)
		{
			Matrix rotation = Matrix.CreateWorld(Vector3.Zero, Vector3.Normalize(target - position), Vector3.Up);
			Quaternion quaternion = Quaternion.CreateFromRotationMatrix(rotation);

			return new Waypoint(position, quaternion, time);
		}


		private int GetWaypointIndex(float t)
		{
			int index = -1;

			for (int i = 0; index == -1 && i < Waypoints.Count; i++)
				if (Waypoints[i].Item3 >= t)
					index = i;

			return index;
		}


		private Waypoint GetWaypoint(int index)
		{
			if (index >= Waypoints.Count)
				index -= Waypoints.Count;
			else if (index < 0)
				index += Waypoints.Count;

			return Waypoints[index];
		}


		private Vector3 GetTangent(int index, float t, float b, float c)
		{
			Vector3 pLast = GetWaypoint(index - 1).Item1;
			Vector3 pCurrent = GetWaypoint(index).Item1;
			Vector3 pNext = GetWaypoint(index + 1).Item1;

			return ((1 - t) * (1 + b) * (1 + c) / 2) * (pCurrent - pLast) + ((1 - t) * (1 - b) * (1 - c) / 2) * (pNext - pCurrent);
		}
	}
}
