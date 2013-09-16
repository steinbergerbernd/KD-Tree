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
using Vertex = Microsoft.Xna.Framework.Graphics.VertexPositionNormalTexture;


namespace Raytracing.Components
{
	public class Cubes : DrawableGameComponent
	{
		public Dictionary<Vector3, Triangle> Triangles { get;  private set; }
		public bool DrawBounds { get; set; }

		Vertex[] vertices;
		BasicEffect effect;
		ICameraService camera;
		int[] indicesTriangles;
		int[] indicesLines;

		Triangle[] triangles;
		Matrix[] sides;

		List<Cube> cubes;

		struct Cube
		{
			public Matrix World;
			public Vector3 Color;
			public bool Fill;
		}

		public Cubes(Game game) : base(game)
		{
			cubes = new List<Cube>();
			Triangles = new Dictionary<Vector3, Triangle>();
		}


		public override void Initialize()
		{
			base.Initialize();

			Vector3[] positions = new Vector3[] 
			{
				new Vector3(0.0f, 0.0f, 0.0f),
				new Vector3(0.0f, 1.0f, 0.0f),
				new Vector3(1.0f, 1.0f, 0.0f),
				new Vector3(1.0f, 0.0f, 0.0f)
			};

			triangles = new Triangle[]
			{
				new Triangle() { Point1 = positions[0], Point2 = positions[1], Point3 = positions[3] },
				new Triangle() { Point1 = positions[1], Point2 = positions[2], Point3 = positions[3] }
			};

			vertices = new Vertex[4];

			for (int i = 0; i < 4; i++)
			{
				vertices[i].Position = positions[i];
				vertices[i].Normal = Vector3.Forward;
			}

			indicesTriangles = new int[4] { 0, 1, 3, 2 };
			indicesLines = new int[] { 0, 1, 2, 3, 0 };

			sides = new Matrix[6];

			for (int i = 0; i < 6; i++)
			{
				sides[i] = Matrix.CreateTranslation(Vector3.One * -0.5f);

				if (i < 4)
					sides[i] *= Matrix.CreateRotationY(i * MathHelper.PiOver2);
				else if (i == 4)
					sides[i] *= Matrix.CreateRotationX(MathHelper.PiOver2);
				else
					sides[i] *= Matrix.CreateRotationX(-MathHelper.PiOver2);

				sides[i] *= Matrix.CreateTranslation(Vector3.One * 0.5f);
			}

			effect = new BasicEffect(GraphicsDevice);
			effect.LightingEnabled = true;

			camera = (ICameraService)Game.Services.GetService(typeof(ICameraService));
		}


		public override void Update(GameTime gameTime)
		{
			base.Update(gameTime);
		}


		public override void Draw(GameTime gameTime)
		{
			effect.View = camera.View;
			effect.Projection = camera.Projection;

			foreach (EffectPass pass in effect.CurrentTechnique.Passes)
			{
				foreach (Cube cube in cubes)
				{
					effect.DiffuseColor = cube.Color;
					effect.DirectionalLight0.Direction = Vector3.Normalize(cube.World.Translation - camera.Position);

					if (GraphicsDevice.RasterizerState.FillMode == FillMode.WireFrame || !cube.Fill)
						effect.AmbientLightColor = cube.Color;
					else
						effect.AmbientLightColor = Vector3.Zero;

					for (int i = 0; i < 6; i++)
					{
						effect.World = sides[i] * cube.World;

						pass.Apply();

						if (cube.Fill)
							GraphicsDevice.DrawUserIndexedPrimitives<Vertex>(PrimitiveType.TriangleStrip, vertices, 0, vertices.Length, indicesTriangles, 0, 2);
						else if (DrawBounds)
							GraphicsDevice.DrawUserIndexedPrimitives<Vertex>(PrimitiveType.LineStrip, vertices, 0, vertices.Length, indicesLines, 0, 4);
					}
				}
			}

			base.Draw(gameTime);
		}


		public void Add(Vector3 position, Vector3 color, Vector3 size, bool fill)
		{
			Matrix world = Matrix.CreateScale(size) * Matrix.CreateTranslation(position);

			var newTriangles = new Dictionary<Vector3, Triangle>();

			if (fill)
			{
				for (int i = 0; i < 6; i++)
				{
					for (int j = 0; j < 2; j++)
					{
						Triangle t = TransformTriangle(triangles[j], sides[i] * world);
						Vector3 key = GetCentroid(t);

						if (Triangles.ContainsKey(key))
							return;

						newTriangles.Add(key, t);
					}
				}
			}

			foreach (KeyValuePair<Vector3, Triangle> kvp in newTriangles)
				Triangles.Add(kvp.Key, kvp.Value);

			cubes.Add(new Cube() { World = world, Color = color, Fill = fill });
		}


		private Vector3 GetCentroid(Triangle t)
		{
			float x = (t.Point1.X + t.Point2.X + t.Point3.X) / 3.0f;
			float y = (t.Point1.Y + t.Point2.Y + t.Point3.Y) / 3.0f;
			float z = (t.Point1.Z + t.Point2.Z + t.Point3.Z) / 3.0f;

			return new Vector3(x, y, z);
		}


		private Triangle TransformTriangle(Triangle triangle, Matrix matrix)
		{
			triangle.Point1 = Vector3.Transform(triangle.Point1, matrix);
			triangle.Point2 = Vector3.Transform(triangle.Point2, matrix);
			triangle.Point3 = Vector3.Transform(triangle.Point3, matrix);

			return triangle;
		}
	}
}
