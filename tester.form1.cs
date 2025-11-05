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
        private bool gameRunning = false;
        private Button targetButton;
        private Random random = new Random();
        private List<ResultEntry> results = new List<ResultEntry>();
        private Timer disappearTimer = new Timer();
        private Timer disappearTimer2 = new Timer();


        private DateTime actionStartTime;
        private bool isHolding = false;
        private const int ACTION_TIME_2MS = 3000;
        private const int ACTION_TIME_MS = 1000; 

        private enum TargetType
        {
            SingleClick,
            DoubleClick,
            HoldClick,
            NoClick
        }
        private TargetType currentTargetType;
        private TargetType lastTargetType = TargetType.NoClick;
        private int clickCount = 0;
        private Timer clickTimer = new Timer { Interval = 600 }; 
        private const int CLICK_TIMER_INTERVAL = 600;

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

            disappearTimer2.Interval = ACTION_TIME_2MS;
            disappearTimer2.Tick += DisappearTimer_Tick;

            disappearTimer.Interval = ACTION_TIME_MS;
            disappearTimer.Tick += DisappearTimer_Tick;

            clickTimer.Interval = CLICK_TIMER_INTERVAL;
            clickTimer.Tick += ClickCountTimer_Tick;

            this.MouseDown += Form1_MouseDown;

            this.Controls.Add(targetButton);
        }

        public class ResultEntry
        {
            public int Score { get; set; }
            public string ActionType { get; set; }
            public TimeSpan TimeTaken { get; set; }
            public DateTime Timestamp { get; set; }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            gameRunning = !gameRunning;
            btnStart.Text = gameRunning ? "Detener" : "Iniciar";
            if (gameRunning)
            {
                score = 0;
                lblScore.Text = "Puntuación: 0";
                results.Clear();
                SpawnTarget();
            }
            else
            {
                targetButton.Visible = false;
                disappearTimer.Stop();
                clickTimer.Stop();
                clickCount = 0;
                isHolding = false;
            }
        }

        private void SpawnTarget()
        {
            if (!gameRunning) return;

            disappearTimer2.Stop();
            disappearTimer.Stop();
            clickTimer.Stop();
            clickCount = 0;
            isHolding = false;

            TargetType newTargetType;
            int numTargetTypes = Enum.GetValues(typeof(TargetType)).Length;

            do
            {
                int randomValue = random.Next(numTargetTypes);
                newTargetType = (TargetType)randomValue;
            }
            while (newTargetType == lastTargetType); 

            currentTargetType = newTargetType;

            lastTargetType = currentTargetType;

            int targetSize = targetButton.Width;
            int minX = 10;
            int minY = btnStart.Bottom + 10;
            int maxX = this.ClientSize.Width - targetSize - 10;
            int maxY = this.ClientSize.Height - targetSize - 10;

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
                    targetButton.Text = $"MANTENER";
                    targetButton.BackColor = Color.Orange;
                    break;
                case TargetType.NoClick:
                    targetButton.Text = $"NO CLICK";
                    targetButton.BackColor = Color.LightCoral;
                    disappearTimer2.Start();
                    break;
            }

            targetButton.Visible = true;
            actionStartTime = DateTime.Now;
        }


        private void DisappearTimer_Tick(object sender, EventArgs e)
        {
            disappearTimer.Stop();
            disappearTimer2.Stop();

            if (!gameRunning) return;

            if (currentTargetType == TargetType.NoClick)
            {
                TimeSpan timeTaken = DateTime.Now - actionStartTime;
                UpdateScore("No Clickeo (Éxito)", timeTaken);
            }
            else if (currentTargetType == TargetType.HoldClick && isHolding)
            {
                TimeSpan timeTaken = DateTime.Now - actionStartTime;
                UpdateScore($"Mantener Click ({ACTION_TIME_MS / 1000}s)", timeTaken);
            }
        }

        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            if (gameRunning && currentTargetType == TargetType.NoClick && targetButton.Bounds.Contains(e.Location) && e.Button == MouseButtons.Left)
            {
                Penalize("Fallo: Clickeó el objetivo 'No Clickeo'");
            }
        }

        private void TargetButton_MouseDown(object sender, MouseEventArgs e)
        {
            if (!gameRunning || e.Button != MouseButtons.Left) return;

            if (currentTargetType == TargetType.NoClick)
            {
                Penalize("Fallo: Clickeó el objetivo 'No Clickeo'");
                return;
            }

            if (currentTargetType == TargetType.HoldClick)
            {
                isHolding = true;
                actionStartTime = DateTime.Now;
                disappearTimer.Start(); 
            }

            else if (currentTargetType == TargetType.SingleClick || currentTargetType == TargetType.DoubleClick)
            {
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

                if (clickCount >= 2) clickCount = 0;
            }
        }

        private void TargetButton_MouseUp(object sender, MouseEventArgs e)
        {
            if (!gameRunning || e.Button != MouseButtons.Left) return;

            if (currentTargetType == TargetType.HoldClick)
            {
                if (isHolding && disappearTimer.Enabled)
                {
                    disappearTimer.Stop();
                    Penalize("Fallo: Soltó antes de tiempo el 'Hold Click'");
                }
                isHolding = false;
            }
        }

        private void ClickCountTimer_Tick(object sender, EventArgs e)
        {
            clickTimer.Stop();
            clickCount = 0;
        }

        private void Form1_MouseUp(object sender, MouseEventArgs e) { }
        private void Form1_MouseMove(object sender, MouseEventArgs e) { }


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

            score = Math.Max(0, score - 1); 
            lblScore.Text = $"Puntuación: {score} (¡Fallo!)";

            results.Add(new ResultEntry
            {
                Score = score,
                ActionType = $"FALLO: {reason}",
                TimeTaken = TimeSpan.Zero,
                Timestamp = DateTime.Now
            });

            SpawnTarget(); 
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

        private void lblScore_Click(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}
