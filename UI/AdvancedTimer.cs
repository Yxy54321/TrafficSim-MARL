using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

public class AdvancedTimer : MonoBehaviour
{
    [SerializeField] private TextMeshPro timerText;
    [SerializeField] private bool startOnAwake = true;
    [SerializeField] private bool showMilliseconds = true;

    public UnityEvent onTimerStart;
    public UnityEvent onTimerStop;
    public UnityEvent<float> onTimerUpdate;

    private float currentTime = 0f;
    private bool isRunning = false;

    void Start()
    {
        if (startOnAwake)
        {
            StartTimer();
        }
        UpdateTimerDisplay();
    }

    void Update()
    {
        if (isRunning)
        {
            currentTime += Time.deltaTime;
            UpdateTimerDisplay();
            onTimerUpdate?.Invoke(currentTime);
        }
    }

    void UpdateTimerDisplay()
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(currentTime / 60);
            int seconds = Mathf.FloorToInt(currentTime % 60);

            if (showMilliseconds)
            {
                float milliseconds = (currentTime % 1) * 100;
                timerText.text = string.Format("{0:00}:{1:00}.{2:00}", minutes, seconds, milliseconds);
            }
            else
            {
                timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
            }
        }
    }

    public void StartTimer()
    {
        isRunning = true;
        onTimerStart?.Invoke();
    }

    public void StopTimer()
    {
        isRunning = false;
        onTimerStop?.Invoke();
    }

    public void ResetTimer()
    {
        currentTime = 0f;
        UpdateTimerDisplay();
    }

    public void ResetAndStart()
    {
        ResetTimer();
        StartTimer();
    }

    public void ToggleTimer()
    {
        if (isRunning)
            StopTimer();
        else
            StartTimer();
    }

    public float GetCurrentTime()
    {
        return currentTime;
    }

    public bool IsRunning()
    {
        return isRunning;
    }
}