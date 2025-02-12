using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Linq;

public class ControlPanelManager : MonoBehaviour
{
    [SerializeField] private RawImage simulationDisplay;
    [SerializeField] private RenderTexture renderTexture;
    [SerializeField] private GameObject parameterPanel;
    [SerializeField] private Button startPauseButton; // Combined button
    [SerializeField] private Button stopButton;
    [SerializeField] public InputField parameter1Input;
    [SerializeField] public InputField parameter2Input;
    [SerializeField] public InputField parameter3Input;

    public float parameter1Value;
    public float parameter2Value;
    public float parameter3Value;

    private TooltipManager TTMng;

    private bool isSimulationRunning = false;
    private bool isPaused = false;
    private Camera simulationCamera;
    private Text startPauseButtonText; // Reference to button text

    private void Start()
    {
        TTMng = FindObjectOfType<TooltipManager>();
        SetImageDisplay(Imgpath);

        // Initialize button text component
        startPauseButtonText = startPauseButton.GetComponentInChildren<Text>();
        startPauseButtonText.text = "Start";

        startPauseButton.onClick.AddListener(HandleStartPauseButton);
        stopButton.onClick.AddListener(StopSimulation);
        parameter1Input.onEndEdit.AddListener(OnParameter1Changed);
        parameter2Input.onEndEdit.AddListener(OnParameter2Changed);
        parameter3Input.onEndEdit.AddListener(OnParameter3Changed);
        // Initial state
        stopButton.interactable = false;
    }

    // Add methods to handle parameter changes
    private void OnParameter1Changed(string value)
    {
        if (float.TryParse(value, out float parsedValue))
        {
            parameter1Value = parsedValue;
            Debug.Log($"Parameter 1 set to: {parameter1Value}");
        }
        else
        {
            Debug.LogWarning("Invalid input for Parameter 1");
            parameter1Input.text = parameter1Value.ToString();
        }
    }

    private void OnParameter2Changed(string value)
    {
        if (float.TryParse(value, out float parsedValue))
        {
            parameter2Value = parsedValue;
            Debug.Log($"Parameter 2 set to: {parameter2Value}");
        }
        else
        {
            Debug.LogWarning("Invalid input for Parameter 2");
            parameter2Input.text = parameter2Value.ToString();
        }
    }

    private void OnParameter3Changed(string value)
    {
        if (float.TryParse(value, out float parsedValue))
        {
            parameter3Value = parsedValue;
            Debug.Log($"Parameter 3 set to: {parameter3Value}");
        }
        else
        {
            Debug.LogWarning("Invalid input for Parameter 3");
            parameter3Input.text = parameter3Value.ToString();
        }
    }

    private void HandleStartPauseButton()
    {
        if (!isSimulationRunning)
        {
            StartSimulation();
        }
        else
        {
            TogglePause();
        }
    }

    private void StartSimulation()
    {
        TTMng.SetVisible(false);
        SceneManager.LoadSceneAsync("simulation", LoadSceneMode.Additive).completed += (op) =>
        {
            Debug.Log("Simulation scene loaded");

            Scene simulationScene = SceneManager.GetSceneByName("simulation");
            SceneManager.SetActiveScene(simulationScene);

            Camera[] cameras = simulationScene.GetRootGameObjects()
                .SelectMany(go => go.GetComponentsInChildren<Camera>())
                .ToArray();

            simulationCamera = cameras.FirstOrDefault(cam => cam.CompareTag("MainCamera"));
            Debug.Log("Found simulation camera: " + simulationCamera.name);

            simulationCamera.transform.position = new Vector3(121.1f, 249.65f, 105.71f);
            simulationCamera.transform.rotation = Quaternion.Euler(90, 0, 0);

            simulationCamera.targetTexture = renderTexture;
            Debug.Log("Set camera target texture: " + (renderTexture != null));

            simulationDisplay.texture = renderTexture;
            Debug.Log("Set display texture: " + (simulationDisplay.texture != null));

            var simulationController = FindObjectOfType<HttpServer>();

            isSimulationRunning = true;
            stopButton.interactable = true;
            startPauseButtonText.text = "Pause"; // Update button text
        };
    }

    public void TogglePause()
    {
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0 : 1;
        startPauseButtonText.text = isPaused ? "Resume" : "Pause";
    }

    private void StopSimulation()
    {
        TTMng.SetTooltipText(TTMng.tip1);
        TTMng.SetVisible(true);
        if (isSimulationRunning)
        {
            RawImageClickCoordinates.Instance.ClearSelectedPoints();
            SceneManager.UnloadSceneAsync("simulation");
            simulationCamera.targetTexture = null;
            simulationDisplay.texture = null;

            isSimulationRunning = false;
            isPaused = false;
            Time.timeScale = 1;

            stopButton.interactable = false;
            startPauseButtonText.text = "Start"; // Reset button text
            SetImageDisplay(Imgpath);
        }
    }

    // Rest of the existing methods remain the same
    private bool isShowingCamera = true;
    private Texture2D loadedImage = null;
    private string Imgpath = "D:\\School\\MFCO\\full_proj\\My project (4)\\Assets\\UI\\map.png";

    public void SetCameraDisplay()
    {
        if (simulationCamera != null)
        {
            simulationCamera.targetTexture = renderTexture;
            simulationDisplay.texture = renderTexture;
        }
    }

    public void SetImageDisplay(string imagePath)
    {
        if (loadedImage != null)
        {
            Destroy(loadedImage);
        }

        byte[] imageData = System.IO.File.ReadAllBytes(imagePath);
        loadedImage = new Texture2D(2, 2);
        loadedImage.LoadImage(imageData);

        if (simulationCamera != null)
        {
            simulationCamera.targetTexture = null;
        }
        simulationDisplay.texture = loadedImage;
    }

    private void OnDestroy()
    {
        if (loadedImage != null)
        {
            Destroy(loadedImage);
        }
    }

    public void ResetSimulationView()
    {
        Scene simulationScene = SceneManager.GetSceneByName("simulation");
        Camera[] cameras = simulationScene.GetRootGameObjects()
            .SelectMany(go => go.GetComponentsInChildren<Camera>())
            .ToArray();

        simulationCamera = cameras.FirstOrDefault(cam => cam.CompareTag("MainCamera"));

        if (simulationCamera != null)
        {
            simulationCamera.transform.position = new Vector3(121.1f, 249.65f, 105.71f);
            simulationCamera.transform.rotation = Quaternion.Euler(90, 0, 0);
            simulationCamera.aspect = 1.0f;
            var camera = simulationCamera.gameObject;
            simulationCamera.enabled = true;
            simulationCamera.targetTexture = renderTexture;
            simulationDisplay.texture = renderTexture;
        }
    }
}
