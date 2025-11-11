using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Linq;

namespace AimTrainer
{
    public partial class Form1 : Form
    {
        private int score = 0;
        private int lives = 3;
        private bool gameRunning = false;
        private Button targetButton;
        private Random random = new Random();
        private List<ResultEntry> results = new List<ResultEntry>();
        private Timer disappearTimer = new Timer();
        private Timer progressBarUpdateTimer = new Timer();

        private DateTime actionStartTime;
        private long virtualElapsedTimeMs = 0;
        private bool isHolding = false;

        // Constantes del juego
        private const int SCORE_THRESHOLD_DOUBLE_ZONE = 15;
        private const int ACTION_TIME_LIMIT_MS = 5000;
        private const int HOLD_TIME_MS = 1000;
        private const int CLICK_TIMER_INTERVAL = 600;
        private const int FINAL_SCORE_MULTIPLIER = 250;

        // Constantes para Dodge Zone
        private const int ACTION_TIME_LIMIT_DODGE_MS = 4000;
        private const double DODGE_SPEED_MULTIPLIER = 2.0;
        private const int DODGE_CLICK_TIME_MS = 2500;

        // Colores
        private static readonly Color LIFE_COLOR = Color.LimeGreen;
        private static readonly Color FAIL_COLOR = Color.DimGray;
        private static readonly Color RED_ZONE_COLOR = Color.FromArgb(200, 255, 0, 0); // Zona Roja Sólida

        private enum TargetType
        {
            SingleClick,
            DoubleClick,
            HoldClick,
            NoClick,
            DodgeZone,
            DodgeClick
        }

        private enum VerticalZone { None, Left, Right }
        private enum HorizontalZone { None, Top, Bottom }

        private TargetType currentTargetType;
        private TargetType lastTargetType = TargetType.NoClick;
        private int clickCount = 0;
        private Timer clickTimer = new Timer { Interval = 600 };

        private VerticalZone currentVerticalZone = VerticalZone.None;
        private HorizontalZone currentHorizontalZone = HorizontalZone.None;
        private List<Rectangle> dodgeZoneAreas = new List<Rectangle>();

        public Form1()
        {
            InitializeComponent();
            this.DoubleBuffered = true;

            targetButton = new Button
            {
                Text = "Click",
                Size = new Size(60, 60),
                Visible = false
            };

            targetButton.MouseDown += TargetButton_MouseDown;
            targetButton.MouseUp += TargetButton_MouseUp;

            disappearTimer.Interval = ACTION_TIME_LIMIT_MS;
            disappearTimer.Tick += DisappearTimer_Tick;

            progressBarUpdateTimer.Interval = 50;
            progressBarUpdateTimer.Tick += ProgressBarUpdateTimer_Tick;

            clickTimer.Interval = CLICK_TIMER_INTERVAL;
            clickTimer.Tick += ClickCountTimer_Tick;

            this.MouseDown += Form1_MouseDown;
            this.MouseMove += Form1_MouseMove;

            this.Controls.Add(targetButton);

            progressBarTimer.Minimum = 0;
            progressBarTimer.Maximum = 100;
            progressBarTimer.Value = 100;
            progressBarTimer.Step = 1;

            // Configurar el evento Paint para dibujar los PictureBox como círculos
            pbLife1.Paint += PictureBox_PaintAsCircle;
            pbLife2.Paint += PictureBox_PaintAsCircle;
            pbLife3.Paint += PictureBox_PaintAsCircle;

            UpdateLifeIndicators();
        }

        public class ResultEntry
        {
            public int Score { get; set; }
            public string ActionType { get; set; }
            public TimeSpan TimeTaken { get; set; }
            public DateTime Timestamp { get; set; }
        }

        private void PictureBox_PaintAsCircle(object sender, PaintEventArgs e)
        {
            PictureBox pb = sender as PictureBox;
            if (pb == null) return;

            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using (SolidBrush brush = new SolidBrush(pb.BackColor))
            {
                e.Graphics.FillEllipse(brush, 0, 0, pb.Width - 1, pb.Height - 1);
            }
        }

        private void UpdateLifeIndicators()
        {
            pbLife1.BackColor = lives >= 1 ? LIFE_COLOR : FAIL_COLOR;
            pbLife2.BackColor = lives >= 2 ? LIFE_COLOR : FAIL_COLOR;
            pbLife3.BackColor = lives >= 3 ? LIFE_COLOR : FAIL_COLOR;

            pbLife1.Invalidate();
            pbLife2.Invalidate();
            pbLife3.Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (gameRunning && (currentTargetType == TargetType.DodgeZone || currentTargetType == TargetType.DodgeClick))
            {
                // Zona de color sólido
                using (Brush brush = new SolidBrush(RED_ZONE_COLOR))
                {
                    foreach (var area in dodgeZoneAreas)
                    {
                        e.Graphics.FillRectangle(brush, area);
                    }
                }

                // Texto de instrucción: "SALIR DE LO ROJO"
                string instruction = $"SALIR DE LO ROJO";
                using (Font font = new Font(FontFamily.GenericSansSerif, 24, FontStyle.Bold))
                // Color del texto: Rojo Oscuro
                using (Brush textBrush = new SolidBrush(Color.DarkRed))
                {
                    var format = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    };
                    e.Graphics.DrawString(instruction, font, textBrush, this.ClientRectangle, format);
                }
            }
        }

        private void GameOver()
        {
            gameRunning = false;
            targetButton.Visible = false;
            disappearTimer.Stop();
            progressBarUpdateTimer.Stop();
            clickTimer.Stop();

            long finalScore = (long)score * FINAL_SCORE_MULTIPLIER;

            MessageBox.Show($"¡Juego Terminado! Has fallado {3 - lives} veces.\n" +
                            $"Aciertos: {score}\n" +
                            $"Puntuación Final: {finalScore}", "GAME OVER", MessageBoxButtons.OK, MessageBoxIcon.Information);

            btnStart.Text = "Iniciar";
            lives = 3;
            score = 0;
            UpdateLifeIndicators();
            lblScore.Text = $"Puntuación: 0 (Última: {finalScore})";
        }


        private void btnStart_Click(object sender, EventArgs e)
        {
            if (gameRunning)
            {
                gameRunning = false;
                GameOver();
            }
            else
            {
                gameRunning = true;
                score = 0;
                lives = 3;
                lblScore.Text = "Puntuación: 0";
                results.Clear();
                UpdateLifeIndicators();
                SpawnTarget();
            }
        }

        private void SpawnTarget()
        {
            if (!gameRunning) return;

            disappearTimer.Stop();
            progressBarUpdateTimer.Stop();
            progressBarTimer.Value = 100;
            virtualElapsedTimeMs = 0;
            currentVerticalZone = VerticalZone.None;
            currentHorizontalZone = HorizontalZone.None;
            dodgeZoneAreas.Clear();
            this.Invalidate();

            clickTimer.Stop();
            clickCount = 0;
            isHolding = false;
            targetButton.Visible = true;

            // Lógica para seleccionar el tipo de desafío
            TargetType newTargetType;
            int maxTargetTypeIndex = Enum.GetValues(typeof(TargetType)).Cast<int>().Max();
            int minTargetTypeIndex = 0;

            if (score < 5)
            {
                maxTargetTypeIndex = (int)TargetType.NoClick;
            }
            else
            {
                maxTargetTypeIndex = Enum.GetValues(typeof(TargetType)).Cast<int>().Max();
            }

            do
            {
                int randomValue = random.Next(minTargetTypeIndex, maxTargetTypeIndex + 1);
                newTargetType = (TargetType)randomValue;
            }
            while (newTargetType == lastTargetType);

            currentTargetType = newTargetType;
            lastTargetType = currentTargetType;

            int targetSize = targetButton.Width;
            int offset = btnStart.Bottom + 10;
            int clientWidth = this.ClientSize.Width;
            int clientHeight = this.ClientSize.Height;
            int drawableHeight = clientHeight - offset;

            // LÓGICA DE ZONAS (DODGEZONE y DODGECLICK)
            if (currentTargetType == TargetType.DodgeZone || currentTargetType == TargetType.DodgeClick)
            {
                targetButton.Visible = (currentTargetType == TargetType.DodgeClick);

                // 1. Determinar Zonas (Doble Zona si score >= 15)
                if (score >= SCORE_THRESHOLD_DOUBLE_ZONE && random.Next(0, 3) < 2)
                {
                    currentVerticalZone = (VerticalZone)random.Next(1, 3);
                    currentHorizontalZone = (HorizontalZone)random.Next(1, 3);
                }
                else
                {
                    if (random.Next(0, 2) == 0)
                    {
                        currentVerticalZone = (VerticalZone)random.Next(1, 3);
                        currentHorizontalZone = HorizontalZone.None;
                    }
                    else
                    {
                        currentVerticalZone = VerticalZone.None;
                        currentHorizontalZone = (HorizontalZone)random.Next(1, 3);
                    }
                }

                if (currentTargetType == TargetType.DodgeZone && currentVerticalZone == VerticalZone.None && currentHorizontalZone == HorizontalZone.None)
                {
                    currentVerticalZone = (VerticalZone)random.Next(1, 3);
                }

                // 2. Calcular Áreas 
                if (currentVerticalZone == VerticalZone.Left)
                    dodgeZoneAreas.Add(new Rectangle(0, offset, clientWidth / 2, drawableHeight));
                else if (currentVerticalZone == VerticalZone.Right)
                    dodgeZoneAreas.Add(new Rectangle(clientWidth / 2, offset, clientWidth / 2, drawableHeight));

                if (currentHorizontalZone == HorizontalZone.Top)
                    dodgeZoneAreas.Add(new Rectangle(0, offset, clientWidth, drawableHeight / 2));
                else if (currentHorizontalZone == HorizontalZone.Bottom)
                    dodgeZoneAreas.Add(new Rectangle(0, offset + drawableHeight / 2, clientWidth, drawableHeight / 2));

                this.Invalidate();

                // 3. Ajustar Timer para DodgeZone/DodgeClick
                if (currentTargetType == TargetType.DodgeZone)
                {
                    disappearTimer.Interval = ACTION_TIME_LIMIT_DODGE_MS;
                }
                else if (currentTargetType == TargetType.DodgeClick)
                {
                    disappearTimer.Interval = DODGE_CLICK_TIME_MS;
                    targetButton.Text = "CLICK RÁPIDO";
                    targetButton.BackColor = Color.Yellow;

                    Point safeLocation = FindSafeSpawnLocation(targetSize, offset);
                    targetButton.Location = safeLocation;
                }
            }
            // LÓGICA DE CLICKS / NO-CLICK (EXISTENTE)
            else
            {
                disappearTimer.Interval = ACTION_TIME_LIMIT_MS;

                int minX = 10;
                int minY = offset;
                int maxX = clientWidth - targetSize - 10;
                int maxY = clientHeight - targetSize - 10;

                if (maxX <= minX || maxY <= minY) return;

                targetButton.Location = new Point(
                    random.Next(minX, maxX),
                    random.Next(minY, maxY)
                );

                switch (currentTargetType)
                {
                    case TargetType.SingleClick:
                        targetButton.Text = "1 CLICK";
                        targetButton.BackColor = Color.LightGreen;
                        break;
                    case TargetType.DoubleClick:
                        targetButton.Text = "2 CLICKS";
                        targetButton.BackColor = Color.LightBlue;
                        break;
                    case TargetType.HoldClick:
                        targetButton.Text = $"MANTENER {HOLD_TIME_MS / 1000}s";
                        targetButton.BackColor = Color.Orange;
                        break;
                    case TargetType.NoClick:
                        targetButton.Text = $"NO CLICK";
                        targetButton.BackColor = Color.LightCoral;
                        break;
                }
            }

            actionStartTime = DateTime.Now;
            disappearTimer.Start();
            progressBarUpdateTimer.Start();
        }

        private Point FindSafeSpawnLocation(int targetSize, int offset)
        {
            int attempts = 0;
            Rectangle targetRect;
            int clientWidth = this.ClientSize.Width;
            int clientHeight = this.ClientSize.Height;
            int minX = 10;
            int minY = offset;
            int maxX = clientWidth - targetSize - 10;
            int maxY = clientHeight - targetSize - 10;

            if (maxX <= minX || maxY <= minY) return new Point(minX, minY);

            do
            {
                int x = random.Next(minX, maxX);
                int y = random.Next(minY, maxY);
                targetRect = new Rectangle(x, y, targetSize, targetSize);

                bool intersects = false;
                foreach (var area in dodgeZoneAreas)
                {
                    if (area.IntersectsWith(targetRect))
                    {
                        intersects = true;
                        break;
                    }
                }

                if (!intersects)
                {
                    return new Point(x, y);
                }
                attempts++;
            } while (attempts < 50);

            return new Point(minX, minY);
        }

        private void ResolveDodgeZoneChallenge(string actionType, TimeSpan timeTaken)
        {
            currentVerticalZone = VerticalZone.None;
            currentHorizontalZone = HorizontalZone.None;
            dodgeZoneAreas.Clear();
            this.Invalidate();

            UpdateScore(actionType, timeTaken);
        }

        private bool IsMouseInRedZone()
        {
            Point mousePos = this.PointToClient(Cursor.Position);
            foreach (var area in dodgeZoneAreas)
            {
                if (area.Contains(mousePos))
                {
                    return true;
                }
            }
            return false;
        }

        private void ProgressBarUpdateTimer_Tick(object sender, EventArgs e)
        {
            if (!gameRunning)
            {
                progressBarUpdateTimer.Stop();
                return;
            }

            int totalInterval = ACTION_TIME_LIMIT_MS;
            int tickInterval = progressBarUpdateTimer.Interval;
            double percentageRemaining;

            if (currentTargetType == TargetType.DodgeZone || currentTargetType == TargetType.DodgeClick)
            {
                totalInterval = (currentTargetType == TargetType.DodgeZone) ? ACTION_TIME_LIMIT_DODGE_MS : DODGE_CLICK_TIME_MS;

                bool isInRedZone = IsMouseInRedZone();

                long timeToAdd = tickInterval;
                if (!isInRedZone)
                {
                    timeToAdd = (long)(tickInterval * DODGE_SPEED_MULTIPLIER);
                }

                virtualElapsedTimeMs += timeToAdd;

                percentageRemaining = 100 - ((double)virtualElapsedTimeMs / totalInterval * 100);

                progressBarTimer.Value = Math.Max(0, Math.Min(100, (int)percentageRemaining));

                if (virtualElapsedTimeMs >= totalInterval)
                {
                    progressBarUpdateTimer.Stop();
                    disappearTimer.Stop();

                    if (currentTargetType == TargetType.DodgeZone)
                    {
                        if (!isInRedZone)
                        {
                            ResolveDodgeZoneChallenge("Dodge Zone (Éxito por evasión)", TimeSpan.FromMilliseconds(virtualElapsedTimeMs));
                        }
                        else
                        {
                            Penalize("Fallo: El tiempo se agotó y el ratón estaba en la zona roja");
                        }
                    }
                    else if (currentTargetType == TargetType.DodgeClick)
                    {
                        Penalize("Fallo: El tiempo se agotó para hacer click en la zona segura");
                    }
                    return;
                }
                return;
            }

            if (currentTargetType == TargetType.HoldClick && isHolding)
            {
                totalInterval = HOLD_TIME_MS;
            }

            TimeSpan elapsedTime = DateTime.Now - actionStartTime;
            percentageRemaining = 100 - (elapsedTime.TotalMilliseconds / totalInterval * 100);

            progressBarTimer.Value = Math.Max(0, Math.Min(100, (int)percentageRemaining));
        }

        private void DisappearTimer_Tick(object sender, EventArgs e)
        {
            disappearTimer.Stop();
            progressBarUpdateTimer.Stop();
            progressBarTimer.Value = 0;

            if (!gameRunning) return;

            if (currentTargetType == TargetType.DodgeZone || currentTargetType == TargetType.DodgeClick) return;

            if (currentTargetType == TargetType.NoClick)
            {
                TimeSpan timeTaken = DateTime.Now - actionStartTime;
                UpdateScore("No Clickeo (Éxito por tiempo)", timeTaken);
            }
            else if (currentTargetType == TargetType.HoldClick && isHolding && disappearTimer.Interval == HOLD_TIME_MS)
            {
                TimeSpan totalTime = DateTime.Now - actionStartTime;
                UpdateScore($"Mantener Click ({HOLD_TIME_MS / 1000}s)", totalTime);
            }
            else
            {
                Penalize($"Fallo: Tiempo agotado para '{currentTargetType}'");
            }
        }

        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            if (gameRunning && e.Button == MouseButtons.Left)
            {
                Point clickLocation = e.Location;

                // Penalización por Click en Zonas Rojas
                if (currentTargetType == TargetType.DodgeZone || currentTargetType == TargetType.DodgeClick)
                {
                    foreach (var area in dodgeZoneAreas)
                    {
                        if (area.Contains(clickLocation))
                        {
                            disappearTimer.Stop();
                            progressBarUpdateTimer.Stop();
                            Penalize("Fallo: Clickeó dentro de la zona roja");
                            return;
                        }
                    }
                }

                // Lógica de penalización para 'No Click'
                if (currentTargetType == TargetType.NoClick && targetButton.Bounds.Contains(clickLocation))
                {
                    disappearTimer.Stop();
                    progressBarUpdateTimer.Stop();
                    Penalize("Fallo: Clickeó el objetivo 'No Clickeo'");
                }
            }
        }

        private void TargetButton_MouseDown(object sender, MouseEventArgs e)
        {
            if (!gameRunning || e.Button != MouseButtons.Left) return;

            // Lógica para DodgeClick
            if (currentTargetType == TargetType.DodgeClick)
            {
                disappearTimer.Stop();
                progressBarUpdateTimer.Stop();
                TimeSpan timeTaken = DateTime.Now - actionStartTime;
                ResolveDodgeZoneChallenge("Dodge Click (Éxito)", timeTaken);
                return;
            }

            if (currentTargetType == TargetType.DodgeZone) return;

            if (currentTargetType == TargetType.NoClick)
            {
                disappearTimer.Stop();
                progressBarUpdateTimer.Stop();
                Penalize("Fallo: Clickeó el objetivo 'No Clickeo'");
                return;
            }

            if (currentTargetType == TargetType.HoldClick)
            {
                isHolding = true;
                actionStartTime = DateTime.Now;
                disappearTimer.Stop();
                disappearTimer.Interval = HOLD_TIME_MS;
                disappearTimer.Start();
            }
            else if (currentTargetType == TargetType.SingleClick || currentTargetType == TargetType.DoubleClick)
            {
                if (clickCount == 0)
                {
                    disappearTimer.Stop();
                    progressBarUpdateTimer.Stop();
                }

                clickCount++;
                clickTimer.Start();

                if (currentTargetType == TargetType.SingleClick && clickCount == 1)
                {
                    clickTimer.Stop();
                    TimeSpan timeTaken = DateTime.Now - actionStartTime;
                    UpdateScore("Single Click", timeTaken);
                }
                else if (currentTargetType == TargetType.DoubleClick && clickCount == 2)
                {
                    clickTimer.Stop();
                    TimeSpan timeTaken = DateTime.Now - actionStartTime;
                    UpdateScore("Doble Click", timeTaken);
                }
            }
        }

        private void TargetButton_MouseUp(object sender, MouseEventArgs e)
        {
            if (!gameRunning || e.Button != MouseButtons.Left) return;

            if (currentTargetType == TargetType.HoldClick)
            {
                if (isHolding)
                {
                    if (disappearTimer.Interval == HOLD_TIME_MS && disappearTimer.Enabled)
                    {
                        disappearTimer.Stop();
                        progressBarUpdateTimer.Stop();
                        Penalize($"Fallo: Soltó antes de tiempo el 'Hold Click' ({HOLD_TIME_MS / 1000}s)");
                    }
                }
                isHolding = false;
                disappearTimer.Interval = ACTION_TIME_LIMIT_MS;
            }
        }

        private void ClickCountTimer_Tick(object sender, EventArgs e)
        {
            clickTimer.Stop();
            if (gameRunning && currentTargetType == TargetType.DoubleClick && clickCount == 1)
            {
                Penalize("Fallo: El tiempo de Doble Click se agotó (solo 1 click)");
            }
            clickCount = 0;
        }

        private void UpdateScore(string actionType, TimeSpan timeTaken)
        {
            score++;
            lblScore.Text = $"Puntuación: {score}";

            results.Add(new ResultEntry
            {
                Score = score,
                ActionType = actionType,
                TimeTaken = timeTaken,
                Timestamp = DateTime.Now
            });

            SpawnTarget();
        }

        private void Penalize(string reason)
        {
            if (!gameRunning) return;

            lives--;
            UpdateLifeIndicators();

            score = Math.Max(0, score - 1);
            lblScore.Text = $"Puntuación: {score} (¡Fallo! - {lives} vidas restantes)";

            results.Add(new ResultEntry
            {
                Score = score,
                ActionType = $"FALLO: {reason}",
                TimeTaken = TimeSpan.Zero,
                Timestamp = DateTime.Now
            });

            if (lives <= 0)
            {
                GameOver();
            }
            else
            {
                SpawnTarget();
            }
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            if (results.Count == 0)
            {
                MessageBox.Show("No hay resultados para exportar.", "Información", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                using (SaveFileDialog saveDialog = new SaveFileDialog())
                {
                    saveDialog.Filter = "Archivo CSV (*.csv)|*.csv";
                    saveDialog.FileName = $"AimTrainer_Resultados_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

                    if (saveDialog.ShowDialog() == DialogResult.OK)
                    {
                        var csvLines = new List<string>();
                        csvLines.Add("Puntuación;Tipo de Acción;Tiempo de Reacción (ms);Marca de Tiempo");

                        foreach (var result in results)
                        {
                            string timeMs = result.TimeTaken.TotalMilliseconds.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
                            string line = $"{result.Score};{result.ActionType};{timeMs};{result.Timestamp:yyyy-MM-dd HH:mm:ss.fff}";
                            csvLines.Add(line);
                        }

                        File.WriteAllLines(saveDialog.FileName, csvLines);
                        MessageBox.Show($"Resultados exportados exitosamente a:\n{saveDialog.FileName}", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al exportar a CSV: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Form1_MouseMove(object sender, MouseEventArgs e) { }
        private void Form1_MouseUp(object sender, MouseEventArgs e) { }
        private void lblScore_Click(object sender, EventArgs e) { }
        private void Form1_Load(object sender, EventArgs e) { }
    }
}
