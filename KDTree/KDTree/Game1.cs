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

using CameraLib;
using Raytracing.Components;

namespace Raytracing
{
	public struct Triangle
	{
		public Vector3 Point1;
		public Vector3 Point2;
		public Vector3 Point3;
	}

	public struct Line
	{
		public Vector3 Point1;
		public Vector3 Point2;
	}

	public class Game1 : Game
	{
		public Cubes Cubes { get; set; }

		GraphicsDeviceManager graphics;

		CameraHandler cameraHandler;
		ICamera camera;
		KDTree kdTree;

		KeyboardState prevKeyboardState;

		BasicEffect effect;

		Line ray;

		public Game1()
		{
			graphics = new GraphicsDeviceManager(this);
			Content.RootDirectory = "Content";

			camera = new FreeLookCamera(this);
			cameraHandler = new CameraHandler(this);
			Cubes = new Cubes(this);

			Components.Add(camera);
			Components.Add(cameraHandler);
			Components.Add(Cubes);

			Services.AddService(typeof(ICameraService), camera);
		}


		protected override void Initialize()
		{
			base.Initialize();

			camera.Position = new Vector3(100.0f, 100.0f, 100.0f);
			camera.LookAt(Vector3.Zero);

			cameraHandler.Camera = camera;

			Random random = new Random(0);

			for (int i = 0; i < 15; i++)
			{
				Cubes.Add(
					new Vector3((float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble()) * 100.0f,
					new Vector3((float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble()),
					new Vector3((float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble()) * 10.0f,
					true);
			}

			kdTree = new KDTree(this, Cubes.Triangles);
			Components.Add(kdTree);

			GraphicsDevice.RasterizerState = RasterizerState.CullNone;

			effect = new BasicEffect(GraphicsDevice);
			effect.VertexColorEnabled = true;
		}


		protected override void Update(GameTime gameTime)
		{
			if (Keyboard.GetState().IsKeyDown(Keys.Escape))
				Exit();

			if (IsKeyPressed(Keys.Tab))
				ToggleWireframe();

			if (IsKeyPressed(Keys.Enter))
				CastRay();

			if (IsKeyPressed(Keys.Space))
				Cubes.DrawBounds = !Cubes.DrawBounds;
			
			prevKeyboardState = Keyboard.GetState();

			base.Update(gameTime);
		}


		private void CastRay()
		{
			ray.Point1 = camera.Position + camera.Forward * camera.NearPlaneDistance;
			ray.Point2 = camera.Position + camera.Forward * (camera.FarPlaneDistance - camera.NearPlaneDistance);

			kdTree.FindIntersections(ray.Point1, camera.Forward, camera.NearPlaneDistance + camera.FarPlaneDistance);
		}


		protected override void Draw(GameTime gameTime)
		{
			GraphicsDevice.Clear(Color.CornflowerBlue);

			effect.View = camera.View;
			effect.Projection = camera.Projection;
			effect.CurrentTechnique.Passes[0].Apply();

			var v = new VertexPositionColor[2];

			v[0] = new VertexPositionColor() { Position = ray.Point1, Color = Color.Red };
			v[1] = new VertexPositionColor() { Position = ray.Point2, Color = Color.Red };

			GraphicsDevice.DrawUserPrimitives<VertexPositionColor>(PrimitiveType.LineList, v, 0, 1);

			FillMode current = GraphicsDevice.RasterizerState.FillMode;

			GraphicsDevice.RasterizerState = new RasterizerState() { FillMode = FillMode.Solid, CullMode = CullMode.None };

			if (kdTree.IntersectedTriangles != null)
			{
				foreach (Triangle t in kdTree.IntersectedTriangles)
				{
					v = new VertexPositionColor[3];

					v[0].Position = t.Point1;
					v[1].Position = t.Point2;
					v[2].Position = t.Point3;

					for (int i = 0; i < 3; i++)
						v[i].Color = Color.Black;

					GraphicsDevice.DrawUserPrimitives<VertexPositionColor>(PrimitiveType.TriangleList, v, 0, 1);
				}
			}

			GraphicsDevice.RasterizerState = new RasterizerState() { FillMode = current, CullMode = CullMode.None };

			base.Draw(gameTime);
		}


		private bool IsKeyPressed(Keys key)
		{
			return prevKeyboardState.IsKeyUp(key) && Keyboard.GetState().IsKeyDown(key);
		}


		private void ToggleWireframe()
		{
			RasterizerState rasterizerState = new RasterizerState() { CullMode = CullMode.None };

			if (GraphicsDevice.RasterizerState.FillMode == FillMode.Solid)
				rasterizerState.FillMode = FillMode.WireFrame;
			else
				rasterizerState.FillMode = FillMode.Solid;

			GraphicsDevice.RasterizerState = rasterizerState;
		}
	}
}
