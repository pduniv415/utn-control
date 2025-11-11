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
        private Timer disappearTimer = new Timer(); // Temporizador principal para la lógica del juego
        private Timer progressBarUpdateTimer = new Timer(); // ¡NUEVO! Temporizador para la barra de progreso

        private DateTime actionStartTime;
        private bool isHolding = false;
        private const int ACTION_TIME_LIMIT_MS = 5000; // 5 segundos para cualquier acción
        private const int HOLD_TIME_MS = 1000; // Tiempo para 'Mantener Click'

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

            disappearTimer.Interval = ACTION_TIME_LIMIT_MS;
            disappearTimer.Tick += DisappearTimer_Tick;

            // Configuración del nuevo temporizador para la barra de progreso
            progressBarUpdateTimer.Interval = 50; // Actualizar cada 50 ms para una animación suave
            progressBarUpdateTimer.Tick += ProgressBarUpdateTimer_Tick;

            clickTimer.Interval = CLICK_TIMER_INTERVAL;
            clickTimer.Tick += ClickCountTimer_Tick;

            this.MouseDown += Form1_MouseDown;

            this.Controls.Add(targetButton);

            // Inicializar la barra de progreso
            progressBarTimer.Minimum = 0;
            progressBarTimer.Maximum = 100;
            progressBarTimer.Value = 100; // Empieza lleno
            progressBarTimer.Step = 1; // El paso no se usará directamente, pero es buena práctica
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
                progressBarUpdateTimer.Stop(); // Detener el temporizador de la barra de progreso
                progressBarTimer.Value = 100; // Restablecer la barra
                clickTimer.Stop();
                clickCount = 0;
                isHolding = false;
            }
        }

        private void SpawnTarget()
        {
            if (!gameRunning) return;

            disappearTimer.Stop();
            progressBarUpdateTimer.Stop(); // Detener cualquier actualización anterior de la barra
            progressBarTimer.Value = 100; // Reiniciar la barra de progreso al 100%

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
                    disappearTimer.Interval = ACTION_TIME_LIMIT_MS;
                    break;
                case TargetType.DoubleClick:
                    targetButton.Text = "2 CLICKS";
                    targetButton.BackColor = Color.LightBlue;
                    disappearTimer.Interval = ACTION_TIME_LIMIT_MS;
                    break;
                case TargetType.HoldClick:
                    targetButton.Text = $"MANTENER {HOLD_TIME_MS / 1000}s";
                    targetButton.BackColor = Color.Orange;
                    disappearTimer.Interval = ACTION_TIME_LIMIT_MS;
                    break;
                case TargetType.NoClick:
                    targetButton.Text = $"NO CLICK";
                    targetButton.BackColor = Color.LightCoral;
                    disappearTimer.Interval = ACTION_TIME_LIMIT_MS;
                    break;
            }

            targetButton.Visible = true;
            actionStartTime = DateTime.Now;
            disappearTimer.Start(); // Iniciar el temporizador principal del juego
            progressBarUpdateTimer.Start(); // Iniciar el temporizador de actualización de la barra
        }


        // ¡NUEVO! Manejador para el temporizador de la barra de progreso
        private void ProgressBarUpdateTimer_Tick(object sender, EventArgs e)
        {
            if (!gameRunning)
            {
                progressBarUpdateTimer.Stop();
                return;
            }

            // Calcular el tiempo transcurrido desde que apareció el objetivo
            TimeSpan elapsedTime = DateTime.Now - actionStartTime;

            // Determinar el tiempo total para la barra de progreso
            int totalInterval = ACTION_TIME_LIMIT_MS;
            if (currentTargetType == TargetType.HoldClick && isHolding)
            {
                totalInterval = HOLD_TIME_MS; // Si se está manteniendo, el tiempo de la barra es 1s
            }

            // Calcular el porcentaje restante
            double percentageRemaining = 100 - (elapsedTime.TotalMilliseconds / totalInterval * 100);

            // Asegurarse de que el valor esté dentro del rango de la barra de progreso
            progressBarTimer.Value = Math.Max(0, Math.Min(100, (int)percentageRemaining));
        }


        private void DisappearTimer_Tick(object sender, EventArgs e)
        {
            disappearTimer.Stop();
            progressBarUpdateTimer.Stop(); // Detener la actualización de la barra de progreso
            progressBarTimer.Value = 0; // Mostrar que el tiempo se agotó completamente

            if (!gameRunning) return;

            if (currentTargetType == TargetType.NoClick)
            {
                TimeSpan timeTaken = DateTime.Now - actionStartTime;
                UpdateScore("No Clickeo (Éxito por tiempo)", timeTaken);
            }
            else if (currentTargetType == TargetType.HoldClick && isHolding && disappearTimer.Interval == HOLD_TIME_MS)
            {
                // Éxito para 'Hold Click' si el timer de 1s se agota
                TimeSpan totalTime = DateTime.Now - actionStartTime;
                UpdateScore($"Mantener Click ({HOLD_TIME_MS / 1000}s)", totalTime);
            }
            else
            {
                // Penalización: El tiempo se agotó para SingleClick, DoubleClick, o HoldClick (no iniciado).
                Penalize($"Fallo: Tiempo agotado para '{currentTargetType}'");
            }
        }

        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            if (gameRunning && currentTargetType == TargetType.NoClick && targetButton.Bounds.Contains(e.Location) && e.Button == MouseButtons.Left)
            {
                disappearTimer.Stop();
                progressBarUpdateTimer.Stop(); // Detener barra
                Penalize("Fallo: Clickeó el objetivo 'No Clickeo'");
            }
        }

        private void TargetButton_MouseDown(object sender, MouseEventArgs e)
        {
            if (!gameRunning || e.Button != MouseButtons.Left) return;

            if (currentTargetType == TargetType.NoClick)
            {
                disappearTimer.Stop();
                progressBarUpdateTimer.Stop(); // Detener barra
                Penalize("Fallo: Clickeó el objetivo 'No Clickeo'");
                return;
            }

            if (currentTargetType == TargetType.HoldClick)
            {
                isHolding = true;
                actionStartTime = DateTime.Now; // Reiniciar el tiempo para el "mantener"
                disappearTimer.Stop(); // Detiene el timer de 5s
                disappearTimer.Interval = HOLD_TIME_MS; // Establece el tiempo de espera de 1s
                disappearTimer.Start();
                // No reiniciar progressBarTimer aquí, el ProgressBarUpdateTimer_Tick lo ajustará.
            }
            else if (currentTargetType == TargetType.SingleClick || currentTargetType == TargetType.DoubleClick)
            {
                if (clickCount == 0)
                {
                    disappearTimer.Stop(); // Detiene el timer de 5s al primer click
                    progressBarUpdateTimer.Stop(); // Detener la barra al primer click para Single/Double
                    progressBarTimer.Value = 100; // Opcional: reiniciar barra si la acción es inmediata
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
                        progressBarUpdateTimer.Stop(); // Detener barra
                        Penalize($"Fallo: Soltó antes de tiempo el 'Hold Click' ({HOLD_TIME_MS / 1000}s)");
                    }
                }
                isHolding = false;
                disappearTimer.Interval = ACTION_TIME_LIMIT_MS; // Restablecer a 5s
                // No reiniciar progressBarTimer aquí, el ProgressBarUpdateTimer_Tick lo ajustará al siguiente objetivo.
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
