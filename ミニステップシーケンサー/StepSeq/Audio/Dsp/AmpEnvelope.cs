namespace StepSeq.Audio.Dsp;

/// <summary>
/// A(ttack) -> D(ecay、Sustainレベルまで下降) -> Sustainレベルを Hold秒維持 -> R(elease)で0まで減衰。
/// JS版の envGain(a, d, sustain, hold, r) と同じ考え方。時間はすべて秒。ElapsedSeconds を渡すと
/// その瞬間のゲイン(0..Peak)を返す。
/// </summary>
public class AmpEnvelope
{
    public double Attack;
    public double Decay;
    public double SustainLevel; // Peakに対する比率 (0..1)
    public double Hold;
    public double Release;
    public double Peak = 1.0;

    public double TotalSeconds => Attack + Decay + Hold + Release;

    public double ValueAt(double t)
    {
        if (t < 0) return 0;
        if (t < Attack)
            return Peak * (Attack <= 0 ? 1 : t / Attack);

        double t2 = t - Attack;
        if (t2 < Decay)
        {
            double ratio = Decay <= 0 ? 1 : t2 / Decay;
            return Peak * Lerp(1.0, SustainLevel, ratio);
        }

        double t3 = t2 - Decay;
        if (t3 < Hold)
            return Peak * SustainLevel;

        double t4 = t3 - Hold;
        if (t4 < Release)
        {
            double ratio = Release <= 0 ? 1 : t4 / Release;
            return Peak * SustainLevel * (1.0 - ratio);
        }

        return 0;
    }

    private static double Lerp(double a, double b, double t) => a + (b - a) * t;
}
