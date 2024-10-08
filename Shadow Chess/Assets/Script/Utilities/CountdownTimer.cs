using System;


public class CountdownTimer
{
    private float initialTime;
    private float Time { get; set; }
    public bool IsRunning { get; protected set; }

    public float Progress => Time / initialTime;
    
    public Action OnTimerStart = delegate { };
    public Action OnTimerStop = delegate { };

    public CountdownTimer(float value)
    {
        initialTime = value;
        IsRunning = false;
    }

    public void Tick(float deltaTime)
    {
        if (IsRunning && Time > 0)
        {
            Time -= deltaTime;
        }

        if (IsRunning && Time <= 0)
        {
            Stop();
        }
    }
    
    public void Start() {
        Time = initialTime;
        if (!IsRunning) {
            IsRunning = true;
            OnTimerStart.Invoke();
        }
    }

    
    public void Stop() {
        if (IsRunning) {
            IsRunning = false;
            OnTimerStop.Invoke();
        }
    }

    public bool IsFinished => Time <= 0;

    public void Reset() => Time = initialTime;

    public void Reset(float newTime)
    {
        initialTime = newTime;
        Reset();
    }
}