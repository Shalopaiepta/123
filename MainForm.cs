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

            string hint =
                "Объёмная проволочная буква «К»:\n" +
                "Мышь: ЛКМ — вращать, Shift+ЛКМ — переносить, колесо — масштаб\n" +
                "Клавиатура: WSAD/стрелки — перенос, Q/E — поворот Z, +/- — масштаб\n" +
                "TAB — пульсация масштаба, R — сброс";
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
                double scale = 2.0 / Math.Max(1, Math.Min(ClientSize.Width, ClientSize.Height));
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
    }
}