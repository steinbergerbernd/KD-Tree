using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace Raytracing
{
	public enum Dimension3D { X, Y, Z };

	public class KDNode
	{
		public float SplitValue { get; set; }
		public Dimension3D Dimension { get; set; }
		public List<Vector3> TriangleKeys { get; set; }
		public KDNode Left { get; set; }
		public KDNode Right { get; set; }
	}
}
