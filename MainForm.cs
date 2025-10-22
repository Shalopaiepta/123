using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace Affine3DWireframe
{
    public class MainForm : Form
    {
        private WireMesh _mesh;
        private Vector3 _position = new Vector3(0, 0, 0);
        private Vector3 _rotation = new Vector3(0, 0, 0); // radians
        private Vector3 _userScale = Vector3.One;
        private Vector3 _animScale = Vector3.One;
        private bool _dragging = false;
        private bool _translating = false;
        private Point _lastMouse;
        private Timer _timer;
        private readonly Stopwatch _stopwatch = new Stopwatch();
        private readonly Random _rng = new Random();
        private bool _jumpActive = false;
        private Vector3 _jumpScale = Vector3.One;
        private double _returnElapsed = 0.0;
        private const double ReturnDuration = 1.0; // сек.
        private const double CameraDistance = 6.0; // смещение по Z для проекции

        public MainForm()
        {
            Text = "Проволочная 3D буква «К». TAB — анимация, R — сброс";
            ClientSize = new Size(1000, 700);
            BackColor = Color.White;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            KeyPreview = true;

            _mesh = WireMesh.CreateLetterK3D(height: 2.0, width: 1.6, stroke: 0.24, depth: 0.6);

            _timer = new Timer();
            _timer.Interval = 16; // ~60 FPS
            _timer.Tick += OnTick;
            _timer.Start();

            _stopwatch.Start();

            MouseDown += OnMouseDown;
            MouseUp += OnMouseUp;
            MouseMove += OnMouseMove;
            MouseWheel += OnMouseWheel;
            KeyDown += OnKeyDown;
            KeyUp += OnKeyUp;
        }

        private void OnTick(object sender, EventArgs e)
        {
            double dt = _stopwatch.Elapsed.TotalSeconds;
            _stopwatch.Restart();

            if (_jumpActive)
            {
                _returnElapsed += dt;
                double t = _returnElapsed / ReturnDuration;
                if (t >= 1.0)
                {
                    _animScale = Vector3.One;
                    _jumpActive = false;
                }
                else
                {
                    double s = t * t * (3.0 - 2.0 * t);
                    _animScale = Vector3.Lerp(_jumpScale, Vector3.One, s);
                }
            }

            Invalidate();
        }

        private double RandScale()
        {
            return 0.6 + _rng.NextDouble() * 1.2;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

            using (var bg = new LinearGradientBrush(ClientRectangle,
                       Color.FromArgb(250, 252, 255),
                       Color.FromArgb(232, 244, 255),
                       90f))
            {
                g.FillRectangle(bg, ClientRectangle);
            }

            int w = ClientSize.Width;
            int h = ClientSize.Height;
            float cx = w * 0.5f;
            float cy = h * 0.5f;

            double fovDeg = 60.0;
            double fovRad = fovDeg * Math.PI / 180.0;
            double focal = (h * 0.5) / Math.Tan(fovRad * 0.5);

            Vector3 totalScale = new Vector3(
                _userScale.X * _animScale.X,
                _userScale.Y * _animScale.Y,
                _userScale.Z * _animScale.Z);

            Matrix4 M = Matrix4.Identity();
            M = Matrix4.Multiply(Matrix4.Scale(totalScale.X, totalScale.Y, totalScale.Z), M);
            M = Matrix4.Multiply(Matrix4.RotationX(_rotation.X), M);
            M = Matrix4.Multiply(Matrix4.RotationY(_rotation.Y), M);
            M = Matrix4.Multiply(Matrix4.RotationZ(_rotation.Z), M);
            M = Matrix4.Multiply(Matrix4.Translation(_position.X, _position.Y, _position.Z), M);

            PointF[] proj = new PointF[_mesh.Vertices.Count];
            bool[] ok = new bool[_mesh.Vertices.Count];

            for (int i = 0; i < _mesh.Vertices.Count; i++)
            {
                Vector3 v = M.TransformPoint(_mesh.Vertices[i]);
                double zc = v.Z + CameraDistance;
                if (zc <= 0.1)
                {
                    ok[i] = false;
                    continue;
                }
                double sx = cx + (v.X * focal / zc);
                double sy = cy - (v.Y * focal / zc);
                proj[i] = new PointF((float)sx, (float)sy);
                ok[i] = true;
            }

            using (var pen = new Pen(Color.FromArgb(40, 60, 90), 2.0f))
            {
                for (int i = 0; i < _mesh.Edges.Count; i++)
                {
                    int a = _mesh.Edges[i].A;
                    int b = _mesh.Edges[i].B;
                    if (a >= 0 && a < proj.Length && b >= 0 && b < proj.Length && ok[a] && ok[b])
                    {
                        g.DrawLine(pen, proj[a], proj[b]);
                    }
                }
            }

            // Draw coordinate axes (coordinate beacon like Minecraft)
            DrawCoordinateAxes(g, cx, cy, focal);

            // Display coordinates in top right corner
            DrawCoordinateDisplay(g);

            string hint =
                "Объёмная проволочная буква «К»:\n" +
                "Мышь: ЛКМ — вращать, Shift+ЛКМ — переносить XY, колесо — масштаб\n" +
                "Клавиатура: WSAD — перенос XY, PageUp/PageDown — перенос Z\n" +
                "Q/E — поворот Z, +/- — масштаб, TAB — анимация, R — сброс";
            using (var br = new SolidBrush(Color.FromArgb(220, Color.Black)))
            using (var font = new Font("Segoe UI", 9f))
            {
                g.DrawString(hint, font, br, 10, 10);
            }
        }

        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            _dragging = true;
            _translating = (ModifierKeys & Keys.Shift) == Keys.Shift;
            _lastMouse = e.Location;
        }

        private void OnMouseUp(object sender, MouseEventArgs e)
        {
            _dragging = false;
            _translating = false;
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (!_dragging) return;

            int dx = e.X - _lastMouse.X;
            int dy = e.Y - _lastMouse.Y;
            _lastMouse = e.Location;

            if (_translating)
            {
                double scale = 8.0 / Math.Max(1, Math.Min(ClientSize.Width, ClientSize.Height));
                _position.X += dx * scale;
                _position.Y -= dy * scale;
            }
            else
            {
                double rotSpeed = Math.PI / 500.0;
                _rotation.Y += dx * rotSpeed;
                _rotation.X += dy * rotSpeed;
            }
        }

        private void OnMouseWheel(object sender, MouseEventArgs e)
        {
            double step = Math.Exp(e.Delta / 120.0 * 0.1);
            _userScale = new Vector3(_userScale.X * step, _userScale.Y * step, _userScale.Z * step);
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            double move = 0.1;
            double rot = Math.PI / 90.0;
            double scl = 1.05;

            if (e.KeyCode == Keys.Left || e.KeyCode == Keys.A) _position.X -= move;
            if (e.KeyCode == Keys.Right || e.KeyCode == Keys.D) _position.X += move;
            if (e.KeyCode == Keys.Up || e.KeyCode == Keys.W) _position.Y += move;
            if (e.KeyCode == Keys.Down || e.KeyCode == Keys.S) _position.Y -= move;
            if (e.KeyCode == Keys.PageUp) _position.Z += move;         // Z вперед (ближе к камере)
            if (e.KeyCode == Keys.PageDown) _position.Z -= move;       // Z назад (дальше от камеры)
            if (e.KeyCode == Keys.Q) _rotation.Z -= rot;
            if (e.KeyCode == Keys.E) _rotation.Z += rot;

            if (e.KeyCode == Keys.Oemplus || e.KeyCode == Keys.Add)
            {
                _userScale = new Vector3(_userScale.X * scl, _userScale.Y * scl, _userScale.Z * scl);
            }
            if (e.KeyCode == Keys.OemMinus || e.KeyCode == Keys.Subtract)
            {
                _userScale = new Vector3(_userScale.X / scl, _userScale.Y / scl, _userScale.Z / scl);
            }

            if (e.KeyCode == Keys.Tab)
            {
                e.SuppressKeyPress = true;
                StartJump();
            }
            if (e.KeyCode == Keys.R)
            {
                ResetState();
            }
        }

        private void OnKeyUp(object sender, KeyEventArgs e)
        {
        }

        private void StartJump()
        {
            _jumpScale = new Vector3(
                RandScale(),
                RandScale(),
                RandScale()
            );
            _animScale = _jumpScale;
            _jumpActive = true;
            _returnElapsed = 0.0;
        }

        private void ResetState()
        {
            _position = new Vector3(0, 0, 0);
            _rotation = new Vector3(0, 0, 0);
            _userScale = Vector3.One;
            _animScale = Vector3.One;
            _jumpActive = false;
            _returnElapsed = 0.0;
        }

        private void DrawCoordinateAxes(Graphics g, float cx, float cy, double focal)
        {
            // Create coordinate axes at origin
            Vector3[] axesPoints = new Vector3[]
            {
                // Origin
                Vector3.Zero,
                // X axis (red)
                new Vector3(1.5, 0, 0),
                // Y axis (green) 
                new Vector3(0, 1.5, 0),
                // Z axis (blue)
                new Vector3(0, 0, 1.5)
            };

            // Apply current transformation to axes
            Vector3 totalScale = new Vector3(
                _userScale.X * _animScale.X,
                _userScale.Y * _animScale.Y,
                _userScale.Z * _animScale.Z);

            Matrix4 M = Matrix4.Identity();
            M = Matrix4.Multiply(Matrix4.Scale(totalScale.X, totalScale.Y, totalScale.Z), M);
            M = Matrix4.Multiply(Matrix4.RotationX(_rotation.X), M);
            M = Matrix4.Multiply(Matrix4.RotationY(_rotation.Y), M);
            M = Matrix4.Multiply(Matrix4.RotationZ(_rotation.Z), M);
            M = Matrix4.Multiply(Matrix4.Translation(_position.X, _position.Y, _position.Z), M);

            // Transform axes points
            PointF[] projAxes = new PointF[4];
            bool[] okAxes = new bool[4];

            for (int i = 0; i < 4; i++)
            {
                Vector3 v = M.TransformPoint(axesPoints[i]);
                double zc = v.Z + CameraDistance;
                if (zc <= 0.1)
                {
                    okAxes[i] = false;
                    continue;
                }
                double sx = cx + (v.X * focal / zc);
                double sy = cy - (v.Y * focal / zc);
                projAxes[i] = new PointF((float)sx, (float)sy);
                okAxes[i] = true;
            }

            if (okAxes[0])
            {
                // Draw X axis (red)
                if (okAxes[1])
                {
                    using (var pen = new Pen(Color.Red, 3.0f))
                    {
                        g.DrawLine(pen, projAxes[0], projAxes[1]);
                        // Draw arrowhead for X
                        DrawArrowhead(g, pen, projAxes[0], projAxes[1]);
                    }
                }

                // Draw Y axis (green)
                if (okAxes[2])
                {
                    using (var pen = new Pen(Color.Green, 3.0f))
                    {
                        g.DrawLine(pen, projAxes[0], projAxes[2]);
                        // Draw arrowhead for Y
                        DrawArrowhead(g, pen, projAxes[0], projAxes[2]);
                    }
                }

                // Draw Z axis (blue)
                if (okAxes[3])
                {
                    using (var pen = new Pen(Color.Blue, 3.0f))
                    {
                        g.DrawLine(pen, projAxes[0], projAxes[3]);
                        // Draw arrowhead for Z
                        DrawArrowhead(g, pen, projAxes[0], projAxes[3]);
                    }
                }
            }
        }

        private void DrawArrowhead(Graphics g, Pen pen, PointF start, PointF end)
        {
            double dx = end.X - start.X;
            double dy = end.Y - start.Y;
            double len = Math.Sqrt(dx * dx + dy * dy);
            if (len < 1e-6) return;

            double arrowLength = 10;
            double arrowAngle = Math.PI / 6; // 30 degrees

            dx /= len;
            dy /= len;

            double x1 = end.X - arrowLength * (dx * Math.Cos(arrowAngle) - dy * Math.Sin(arrowAngle));
            double y1 = end.Y - arrowLength * (dy * Math.Cos(arrowAngle) + dx * Math.Sin(arrowAngle));

            double x2 = end.X - arrowLength * (dx * Math.Cos(-arrowAngle) - dy * Math.Sin(-arrowAngle));
            double y2 = end.Y - arrowLength * (dy * Math.Cos(-arrowAngle) + dx * Math.Sin(-arrowAngle));

            g.DrawLine(pen, end, new PointF((float)x1, (float)y1));
            g.DrawLine(pen, end, new PointF((float)x2, (float)y2));
        }

        private void DrawCoordinateDisplay(Graphics g)
        {
            // Calculate display coordinates based on current transformation state
            Vector3 totalScale = new Vector3(
                _userScale.X * _animScale.X,
                _userScale.Y * _animScale.Y,
                _userScale.Z * _animScale.Z);

            // Convert rotation from radians to degrees for display
            double rotX = _rotation.X * 180.0 / Math.PI;
            double rotY = _rotation.Y * 180.0 / Math.PI;
            double rotZ = _rotation.Z * 180.0 / Math.PI;

            string coordText = String.Format(
                "global position: x{0:+0.00;-0.00;+0.00} y{1:+0.00;-0.00;+0.00} z{2:+0.00;-0.00;+0.00}\n" +
                "local rotation: x{3:+0.0;-0.0;+0.0} y{4:+0.0;-0.0;+0.0} z{5:+0.0;-0.0;+0.0}\n" +
                "scale: x{6:+0.000;-0.000;+0.000} y{7:+0.000;-0.000;+0.000} z{8:+0.000;-0.000;+0.000}",
                _position.X, _position.Y, _position.Z,
                rotX, rotY, rotZ,
                totalScale.X, totalScale.Y, totalScale.Z);

            using (var brush = new SolidBrush(Color.White))
            using (var font = new Font("Consolas", 9f, FontStyle.Regular))
            {
                SizeF textSize = g.MeasureString(coordText, font);
                float x = ClientSize.Width - textSize.Width - 10;
                float y = 10;

                // Draw background
                using (var bgBrush = new SolidBrush(Color.FromArgb(180, Color.Black)))
                {
                    g.FillRectangle(bgBrush, x - 5, y - 2, textSize.Width + 10, textSize.Height + 4);
                }

                g.DrawString(coordText, font, brush, x, y);
            }
        }
    }
}