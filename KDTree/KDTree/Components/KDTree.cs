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

namespace Raytracing.Components
{
	public class KDTree : DrawableGameComponent
	{
		public List<Triangle> IntersectedTriangles { get; set; }

		Dictionary<Vector3, Triangle> triangleList;
		KDNode root;
		BasicEffect effect; 

		public KDTree(Game game, Dictionary<Vector3, Triangle> triangleList) : base(game)
		{
			this.triangleList = triangleList;

			root = BuildKdTree(triangleList.Keys.ToList(), new List<Vector3>());

			Initialize();
			effect = new BasicEffect(GraphicsDevice);
		}


		public override void Draw(GameTime gameTime)
		{
			ICameraService camera = (ICameraService)Game.Services.GetService(typeof(ICameraService));

			effect.View = camera.View;
			effect.Projection = camera.Projection;
			effect.CurrentTechnique.Passes[0].Apply();

			//DrawTree(root);

			base.Draw(gameTime);
		}


		private void DrawTree(KDNode node)
		{
			if (node == null)
				return;

			DrawTree(node.Left);
			DrawTree(node.Right);

			if (node.TriangleKeys == null)
				return;

			foreach (Vector3 key in node.TriangleKeys)
			{
				Triangle t = triangleList[key];
				var v = new VertexPositionColor[3];

				v[0].Position = t.Point1;
				v[1].Position = t.Point2;
				v[2].Position = t.Point3;

				for (int i = 0; i < 3; i++)
					v[i].Color = Color.Black;

				GraphicsDevice.DrawUserPrimitives<VertexPositionColor>(PrimitiveType.TriangleList, v, 0, 1);
			}
		}


		public void FindIntersections(Vector3 start, Vector3 direction, float tMax)
		{
			IntersectedTriangles = new List<Triangle>();
			VisitNodes(root, start, Vector3.Normalize(direction), tMax);
		}

		private bool IsInsideTriangle(Triangle triangle, Vector3 point)
		{
			Vector3 v0 = triangle.Point3 - triangle.Point1;
			Vector3 v1 = triangle.Point2 - triangle.Point1;
			Vector3 v2 = point - triangle.Point1;

			float dot00 = Vector3.Dot(v0, v0);
			float dot01 = Vector3.Dot(v0, v1);
			float dot02 = Vector3.Dot(v0, v2);
			float dot11 = Vector3.Dot(v1, v1);
			float dot12 = Vector3.Dot(v1, v2);

			float invDenom = 1.0f / (dot00 * dot11 - dot01 * dot01);
			float u = (dot11 * dot02 - dot01 * dot12) * invDenom;
			float v = (dot00 * dot12 - dot01 * dot02) * invDenom;

			return u >= 0.0f && v >= 0.0f && (float)(u + v) <= 1.0f;
		}


		private void VisitNodes(KDNode node, Vector3 position, Vector3 viewVector, float tMax)
		{
			if (node == null)
				return;

			if (node.TriangleKeys != null)
			{
				foreach (Vector3 p in node.TriangleKeys)
				{
					Triangle t = triangleList[p];

					Vector3 n = Vector3.Normalize(Vector3.Cross(t.Point2 - t.Point1, t.Point3 - t.Point1));

					if (Vector3.Dot(n, viewVector) == 0.0f)
						continue;

					float u = Vector3.Dot((t.Point1 - position), n) / Vector3.Dot(n, viewVector);

					Vector3 i = position + viewVector * u;

					if (IsInsideTriangle(t, position + viewVector * u))
						IntersectedTriangles.Add(t);
				}
			}

			float value = 0.0f;
			float pos = 0.0f;

			switch (node.Dimension)
			{
				case Dimension3D.X:
					value = viewVector.X;
					pos = position.X;
					break;
				case Dimension3D.Y:
					value = viewVector.Y;
					pos = position.Y;
					break;
				case Dimension3D.Z:
					value = viewVector.Z;
					pos = position.Z;
					break;
			}

			bool left = pos < node.SplitValue;

			if (value == 0.0f)
				VisitNodes(left ? node.Left : node.Right, position, viewVector, tMax);
			else
			{
				float t = (node.SplitValue - pos) / value;

				if (0.0f <= t && t < tMax)
				{
					VisitNodes(left ? node.Left : node.Right, position, viewVector, t);
					VisitNodes(left ? node.Right : node.Left, position + viewVector * t, viewVector, tMax - t);
				}
				else
				{
					VisitNodes(left ? node.Left : node.Right, position, viewVector, tMax);
				}
			}
		}


		private KDNode BuildKdTree(List<Vector3> pointList, List<Vector3> overlapping)
		{
			Vector3[] extremePoints = CalculateExtremePoints(pointList.ToArray(), 0, pointList.Count - 1);

			float lengthX = extremePoints[1].X - extremePoints[0].X;
			float lengthY = extremePoints[3].Y - extremePoints[2].Y;
			float lengthZ = extremePoints[5].Z - extremePoints[4].Z;

			((Game1)Game).Cubes.Add(
				new Vector3(extremePoints[0].X, extremePoints[2].Y, extremePoints[4].Z),
				Vector3.Zero,
				new Vector3(lengthX, lengthY, lengthZ),
				false);

			KDNode node = new KDNode();

			float median;

			if (lengthX > lengthY && lengthX > lengthZ)
			{
				median = MedianHoare(pointList.Select(tl => tl.X).ToArray());
				node.Dimension = Dimension3D.X;
			}
			else if (lengthY >= lengthX && lengthY > lengthZ)
			{
				median = MedianHoare(pointList.Select(tl => tl.Y).ToArray());
				node.Dimension = Dimension3D.Y;
			}
			else
			{
				median = MedianHoare(pointList.Select(tl => tl.Z).ToArray());
				node.Dimension = Dimension3D.Z;
			}

			node.SplitValue = median;

			float value, tPoint1Value, tPoint2Value, tPoint3Value;

			List<Vector3> overlappingLeft = new List<Vector3>(), overlappingRight = new List<Vector3>();
			List<Vector3> leftPointList = new List<Vector3>(), rightPointList = new List<Vector3>();

			foreach (Vector3 point in pointList)
			{
				GetPointValues(node.Dimension, point, out value, out tPoint1Value, out tPoint2Value, out tPoint3Value);

				if (value < median)
				{
					leftPointList.Add(point);

					if (tPoint1Value >= median || tPoint2Value >= median || tPoint3Value >= median)
						overlappingRight.Add(point);
				}
				else
				{
					rightPointList.Add(point);

					if (tPoint1Value <= median || tPoint2Value <= median || tPoint3Value <= median)
						overlappingLeft.Add(point);
				}
			}

			foreach (Vector3 p in overlapping)
			{
				GetPointValues(node.Dimension, p, out value, out tPoint1Value, out tPoint2Value, out tPoint3Value);

				if (tPoint1Value <= median || tPoint2Value <= median || tPoint3Value <= median)
					overlappingLeft.Add(p);

				if (tPoint1Value >= median || tPoint2Value >= median || tPoint3Value >= median)
					overlappingRight.Add(p);
			}

			if (leftPointList.Count > 12)
				node.Left = BuildKdTree(leftPointList, overlappingLeft);
			else
			{
				node.Left = null;
				node.TriangleKeys = new List<Vector3>(overlappingLeft.Union(leftPointList));
			}

			if (rightPointList.Count > 12)
				node.Right = BuildKdTree(rightPointList, overlappingRight);
			else
			{
				node.Right = null;

				if (node.TriangleKeys == null)
					node.TriangleKeys = new List<Vector3>();

				node.TriangleKeys.AddRange(new List<Vector3>(overlappingRight.Union(rightPointList)));
			}

			return node;
		}


		private void GetPointValues(Dimension3D dimension, Vector3 point, out float value, out float tPoint1Value, out float tPoint2Value, out float tPoint3Value)
		{
			Triangle triangle = triangleList[point];

			switch (dimension)
			{
				case Dimension3D.X:
					value = point.X;
					tPoint1Value = triangle.Point1.X;
					tPoint2Value = triangle.Point2.X;
					tPoint3Value = triangle.Point3.X;
					break;
				case Dimension3D.Y:
					value = point.Y;
					tPoint1Value = triangle.Point1.Y;
					tPoint2Value = triangle.Point2.Y;
					tPoint3Value = triangle.Point3.Y;
					break;
				case Dimension3D.Z:
					value = point.Z;
					tPoint1Value = triangle.Point1.Z;
					tPoint2Value = triangle.Point2.Z;
					tPoint3Value = triangle.Point3.Z;
					break;
				default:
					value = 0;
					tPoint1Value = 0;
					tPoint2Value = 0;
					tPoint3Value = 0;
					break;
			}
		}


		private Vector3[] CalculateExtremePoints(Vector3[] points, int begin, int end)
		{
			Vector3[] p0, p1, returnPoints;

			if (end - begin > 1)
			{
				p0 = CalculateExtremePoints(points, begin, begin + (end - begin) / 2);
				p1 = CalculateExtremePoints(points, begin + (end - begin) / 2 + 1, end);
			}
			else
			{
				p0 = new Vector3[] { points[begin], points[begin], points[begin], points[begin], points[begin], points[begin] };
				p1 = new Vector3[] { points[end], points[end], points[end], points[end], points[end], points[end] };
			}

			returnPoints = new Vector3[6];
			returnPoints[0] = (p0[0].X < p1[0].X) ? p0[0] : p1[0];
			returnPoints[1] = (p0[1].X > p1[1].X) ? p0[1] : p1[1];
			returnPoints[2] = (p0[2].Y < p1[2].Y) ? p0[2] : p1[2];
			returnPoints[3] = (p0[3].Y > p1[3].Y) ? p0[3] : p1[3];
			returnPoints[4] = (p0[4].Z < p1[4].Z) ? p0[4] : p1[4];
			returnPoints[5] = (p0[5].Z > p1[5].Z) ? p0[5] : p1[5];

			return returnPoints;
		}

		private float MedianHoare(float[] array)
		{
			int middle = GetMedianIndex(array.Length);
			float median = Hoare(array, middle);

			if (triangleList.Count % 2 == 0)
				median = (median + Hoare(array, middle + 1)) / 2;

			return median;
		}

		//Mediansuche nach Hoare
		private float Hoare(float[] array, int index)
		{
			int left = 0, right = array.Length - 1;
			int i, j;

			while (left < right)
			{
				i = left;
				j = right;

				partition(array, ref i, ref j, index);

				if (j < index)
					left = i;

				if (index < i)
					right = j;
			}

			return array[index];
		}

		private void partition(float[] array, ref int i, ref int j, int p)
		{
			float pivot = array[p];

			while (i <= j)
			{
				while (array[i] < pivot)
					++i;

				while (pivot < array[j])
					--j;

				if (i <= j)
					swap(array, i++, j--);
			}
		}

		private void swap(float[] array, int i, int j)
		{
			float tmp = array[i];
			array[i] = array[j];
			array[j] = tmp;
		}

		private int GetMedianIndex(int n)
		{
			return (n % 2 == 0) ? n / 2 - 1 : (n - 1) / 2;
		}
	}
}
