using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace CBRE.Editor {
    public class Timing {
        public const float Step = 1f / 60f;

        double accumulator = 0f;
        long lastMeasurementStop = 0;
        Stopwatch stopwatch = new Stopwatch();

        public void StartMeasurement() {
            if (!stopwatch.IsRunning) { stopwatch.Start(); }
        }
        public void EndMeasurement() {
            long elapsedTicks = stopwatch.ElapsedTicks;
            accumulator += (double)(elapsedTicks - lastMeasurementStop) / (double)Stopwatch.Frequency;
            lastMeasurementStop = elapsedTicks;
        }

        public void PerformTicks(Action action) {
            if (accumulator >= Step * 3f) { accumulator = 0f; }
            while (accumulator >= 0f) {
                accumulator -= Step;
                action();
            }
        }
    }
}
