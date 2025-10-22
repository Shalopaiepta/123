using System;
using System.Collections.Generic;

namespace Affine3DWireframe
{
    public struct Vector3
    {
        public double X, Y, Z;
        public Vector3(double x, double y, double z) { X = x; Y = y; Z = z; }
        public static Vector3 operator +(Vector3 a, Vector3 b) => new Vector3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static Vector3 operator -(Vector3 a, Vector3 b) => new Vector3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static Vector3 operator *(Vector3 a, double s) => new Vector3(a.X * s, a.Y * s, a.Z * s);
        public static Vector3 Lerp(Vector3 a, Vector3 b, double t) =>
            new Vector3(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t, a.Z + (b.Z - a.Z) * t);
        public static readonly Vector3 One = new Vector3(1, 1, 1);
        public static readonly Vector3 Zero = new Vector3(0, 0, 0);
    }

    public struct Matrix4
    {
        public double M00, M01, M02, M03;
        public double M10, M11, M12, M13;
        public double M20, M21, M22, M23;
        public double M30, M31, M32, M33;

        public static Matrix4 Identity()
        {
            Matrix4 m = new Matrix4();
            m.M00 = 1; m.M11 = 1; m.M22 = 1; m.M33 = 1;
            return m;
        }

        public static Matrix4 Translation(double tx, double ty, double tz)
        {
            Matrix4 m = Identity();
            m.M03 = tx;
            m.M13 = ty;
            m.M23 = tz;
            return m;
        }

        public static Matrix4 Scale(double sx, double sy, double sz)
        {
            Matrix4 m = Identity();
            m.M00 = sx;
            m.M11 = sy;
            m.M22 = sz;
            return m;
        }

        public static Matrix4 RotationX(double radians)
        {
            double c = Math.Cos(radians);
            double s = Math.Sin(radians);
            Matrix4 m = Identity();
            m.M11 = c; m.M12 = -s;
            m.M21 = s; m.M22 = c;
            return m;
        }

        public static Matrix4 RotationY(double radians)
        {
            double c = Math.Cos(radians);
            double s = Math.Sin(radians);
            Matrix4 m = Identity();
            m.M00 = c; m.M02 = s;
            m.M20 = -s; m.M22 = c;
            return m;
        }

        public static Matrix4 RotationZ(double radians)
        {
            double c = Math.Cos(radians);
            double s = Math.Sin(radians);
            Matrix4 m = Identity();
            m.M00 = c; m.M01 = -s;
            m.M10 = s; m.M11 = c;
            return m;
        }

        public static Matrix4 Multiply(Matrix4 a, Matrix4 b)
        {
            Matrix4 r = new Matrix4();
            r.M00 = a.M00 * b.M00 + a.M01 * b.M10 + a.M02 * b.M20 + a.M03 * b.M30;
            r.M01 = a.M00 * b.M01 + a.M01 * b.M11 + a.M02 * b.M21 + a.M03 * b.M31;
            r.M02 = a.M00 * b.M02 + a.M01 * b.M12 + a.M02 * b.M22 + a.M03 * b.M32;
            r.M03 = a.M00 * b.M03 + a.M01 * b.M13 + a.M02 * b.M23 + a.M03 * b.M33;

            r.M10 = a.M10 * b.M00 + a.M11 * b.M10 + a.M12 * b.M20 + a.M13 * b.M30;
            r.M11 = a.M10 * b.M01 + a.M11 * b.M11 + a.M12 * b.M21 + a.M13 * b.M31;
            r.M12 = a.M10 * b.M02 + a.M11 * b.M12 + a.M12 * b.M22 + a.M13 * b.M32;
            r.M13 = a.M10 * b.M03 + a.M11 * b.M13 + a.M12 * b.M23 + a.M13 * b.M33;

            r.M20 = a.M20 * b.M00 + a.M21 * b.M10 + a.M22 * b.M20 + a.M23 * b.M30;
            r.M21 = a.M20 * b.M01 + a.M21 * b.M11 + a.M22 * b.M21 + a.M23 * b.M31;
            r.M22 = a.M20 * b.M02 + a.M21 * b.M12 + a.M22 * b.M22 + a.M23 * b.M32;
            r.M23 = a.M20 * b.M03 + a.M21 * b.M13 + a.M22 * b.M23 + a.M23 * b.M33;

            r.M30 = a.M30 * b.M00 + a.M31 * b.M10 + a.M32 * b.M20 + a.M33 * b.M30;
            r.M31 = a.M30 * b.M01 + a.M31 * b.M11 + a.M32 * b.M21 + a.M33 * b.M31;
            r.M32 = a.M30 * b.M02 + a.M31 * b.M12 + a.M32 * b.M22 + a.M33 * b.M32;
            r.M33 = a.M30 * b.M03 + a.M31 * b.M13 + a.M32 * b.M23 + a.M33 * b.M33;
            return r;
        }

        public Vector3 TransformPoint(Vector3 v)
        {
            double x = v.X, y = v.Y, z = v.Z, w = 1.0;
            double tx = M00 * x + M01 * y + M02 * z + M03 * w;
            double ty = M10 * x + M11 * y + M12 * z + M13 * w;
            double tz = M20 * x + M21 * y + M22 * z + M23 * w;
            double tw = M30 * x + M31 * y + M32 * z + M33 * w;
            if (tw != 0.0) { tx /= tw; ty /= tw; tz /= tw; }
            return new Vector3(tx, ty, tz);
        }
    }

    public struct Edge
    {
        public int A;
        public int B;
        public Edge(int a, int b) { A = a; B = b; }
    }

    public class WireMesh
    {
        public List<Vector3> Vertices = new List<Vector3>();
        public List<Edge> Edges = new List<Edge>();

        public static WireMesh CreateCube(double size)
        {
            double s = size * 0.5;
            Vector3[] v = new Vector3[]
            {
                new Vector3(-s, -s, -s),
                new Vector3( s, -s, -s),
                new Vector3( s,  s, -s),
                new Vector3(-s,  s, -s),
                new Vector3(-s, -s,  s),
                new Vector3( s, -s,  s),
                new Vector3( s,  s,  s),
                new Vector3(-s,  s,  s),
            };
            int[,] e = new int[,]
            {
                {0,1},{1,2},{2,3},{3,0},
                {4,5},{5,6},{6,7},{7,4},
                {0,4},{1,5},{2,6},{3,7}
            };
            WireMesh m = new WireMesh();
            for (int i = 0; i < v.Length; i++) m.Vertices.Add(v[i]);
            for (int i = 0; i < e.GetLength(0); i++) m.Edges.Add(new Edge(e[i, 0], e[i, 1]));
            Vector3 c = m.ComputeCentroid();
            for (int i = 0; i < m.Vertices.Count; i++)
                m.Vertices[i] = m.Vertices[i] - c;
            return m;
        }

        public static WireMesh CreateLetterK(double height = 4.0, double width = 3.0)
        {
            double h = height;
            double w = width;
            double xLeft = -w * 0.4;
            double xRight = w * 0.5;
            double yTop = h * 0.5;
            double yBot = -h * 0.5;
            double yMid = 0.0;

            var m = new WireMesh();
            m.Vertices.Add(new Vector3(xLeft, yBot, 0));
            m.Vertices.Add(new Vector3(xLeft, yTop, 0));
            m.Vertices.Add(new Vector3(xLeft, yMid, 0));
            m.Vertices.Add(new Vector3(xRight, yTop, 0));
            m.Vertices.Add(new Vector3(xRight, yBot, 0));

            m.Edges.Add(new Edge(0, 1));
            m.Edges.Add(new Edge(2, 3));
            m.Edges.Add(new Edge(2, 4));

            Vector3 c = m.ComputeCentroid();
            for (int i = 0; i < m.Vertices.Count; i++)
                m.Vertices[i] = m.Vertices[i] - c;
            return m;
        }

        public static WireMesh CreateLetterK3D(double height = 2.0, double width = 1.6, double stroke = 0.24, double depth = 0.6)
        {
            double h = height;
            double w = width;
            double xLeft = -w * 0.4;
            double xRight = w * 0.5;
            double yTop = h * 0.5;
            double yBot = -h * 0.5;
            double yMid = 0.0;

            var m = new WireMesh();

            Vector3 p0 = new Vector3(xLeft, yBot, 0);
            Vector3 p1 = new Vector3(xLeft, yTop, 0);
            Vector3 p2 = new Vector3(xLeft, yMid, 0);
            Vector3 p3 = new Vector3(xRight, yTop, 0);
            Vector3 p4 = new Vector3(xRight, yBot, 0);

            m.AppendBar(p0, p1, stroke, depth);
            m.AppendBar(p2, p3, stroke, depth);
            m.AppendBar(p2, p4, stroke, depth);

            Vector3 c = m.ComputeCentroid();
            for (int i = 0; i < m.Vertices.Count; i++)
                m.Vertices[i] = m.Vertices[i] - c;

            return m;
        }

        private int AddVertex(Vector3 v)
        {
            Vertices.Add(v);
            return Vertices.Count - 1;
        }

        private void AppendBar(Vector3 p, Vector3 q, double stroke, double depth)
        {
            double dx = q.X - p.X;
            double dy = q.Y - p.Y;
            double len = Math.Sqrt(dx * dx + dy * dy);
            if (len < 1e-6) return;

            double nx = -dy / len;
            double ny = dx / len;

            double r = stroke * 0.5;
            double hd = depth * 0.5;

            Vector3 pL = new Vector3(p.X + nx * r, p.Y + ny * r, 0);
            Vector3 pR = new Vector3(p.X - nx * r, p.Y - ny * r, 0);
            Vector3 qL = new Vector3(q.X + nx * r, q.Y + ny * r, 0);
            Vector3 qR = new Vector3(q.X - nx * r, q.Y - ny * r, 0);

            int v0 = AddVertex(new Vector3(pL.X, pL.Y, -hd));
            int v1 = AddVertex(new Vector3(pR.X, pR.Y, -hd));
            int v2 = AddVertex(new Vector3(qR.X, qR.Y, -hd));
            int v3 = AddVertex(new Vector3(qL.X, qL.Y, -hd));

            int u0 = AddVertex(new Vector3(pL.X, pL.Y, +hd));
            int u1 = AddVertex(new Vector3(pR.X, pR.Y, +hd));
            int u2 = AddVertex(new Vector3(qR.X, qR.Y, +hd));
            int u3 = AddVertex(new Vector3(qL.X, qL.Y, +hd));

            Edges.Add(new Edge(v0, v1));
            Edges.Add(new Edge(v1, v2));
            Edges.Add(new Edge(v2, v3));
            Edges.Add(new Edge(v3, v0));

            Edges.Add(new Edge(u0, u1));
            Edges.Add(new Edge(u1, u2));
            Edges.Add(new Edge(u2, u3));
            Edges.Add(new Edge(u3, u0));

            Edges.Add(new Edge(v0, u0));
            Edges.Add(new Edge(v1, u1));
            Edges.Add(new Edge(v2, u2));
            Edges.Add(new Edge(v3, u3));
        }

        public Vector3 ComputeCentroid()
        {
            if (Vertices.Count == 0) return Vector3.Zero;
            Vector3 sum = Vector3.Zero;
            for (int i = 0; i < Vertices.Count; i++)
                sum = sum + Vertices[i];
            return new Vector3(sum.X / Vertices.Count, sum.Y / Vertices.Count, sum.Z / Vertices.Count);
        }
    }
}