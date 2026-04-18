using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class PauseUI : MonoBehaviour
{
    [Header("Panels")]
    public GameObject pausePanel;
    public GameObject pauseOptionsPanel;

    [Header("Pause Buttons")]
    public Button resumeButton;
    public Button optionsButton;
    public Button menuButton;

    [Header("Audio")]
    public AudioMixer mainMixer;

    [Header("Options")]
    public Slider masterVolumeSlider;
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;
    public Slider sensitivitySlider;
    public Button optionsBackButton;

    [Header("Info")]
    public Text predatorModeText; // shows which predator is active

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        resumeButton?.onClick.AddListener(() => GameManager.instance.TogglePause());
        optionsButton?.onClick.AddListener(() => ShowPanel(pauseOptionsPanel));
        menuButton?.onClick.AddListener(() => GameManager.instance.GoToMainMenu());
        optionsBackButton?.onClick.AddListener(() => ShowPanel(pausePanel));

        float savedMaster = PlayerPrefs.GetFloat("MasterVolume", 1f);
        float savedMusic = PlayerPrefs.GetFloat("MusicVolume", 1f);
        float savedSFX = PlayerPrefs.GetFloat("SFXVolume", 1f);
        float savedSens = PlayerPrefs.GetFloat("MouseSensitivity", 120f);

        masterVolumeSlider?.SetValueWithoutNotify(savedMaster);
        musicVolumeSlider?.SetValueWithoutNotify(savedMusic);
        sfxVolumeSlider?.SetValueWithoutNotify(savedSFX);

        SetMixerVolume("MasterVol", savedMaster);
        masterVolumeSlider?.onValueChanged.AddListener(v =>
        {
            SetMixerVolume("MasterVol", v);
            PlayerPrefs.SetFloat("MasterVolume", v);
        });

        SetMixerVolume("MusicVol", savedMusic);
        musicVolumeSlider?.onValueChanged.AddListener(v =>
        {
            SetMixerVolume("MusicVol", v);
            PlayerPrefs.SetFloat("MusicVolume", v);
        });

        SetMixerVolume("SFXVol", savedSFX);
        sfxVolumeSlider?.onValueChanged.AddListener(v =>
        {
            SetMixerVolume("SFXVol", v);
            PlayerPrefs.SetFloat("SFXVolume", v);
        });

        if (sensitivitySlider != null)
        {
            sensitivitySlider.minValue = 10f;
            sensitivitySlider.maxValue = 200f;
            sensitivitySlider.value = savedSens;

            sensitivitySlider.onValueChanged.AddListener(delegate { OnSensitivityChanged(); });
        }

        Hide();
    }

    // mouse sens
    public void OnSensitivityChanged()
    {
        float val = sensitivitySlider.value;

        PlayerPrefs.SetFloat("MouseSensitivity", val);

        PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
        if (player != null)
        {
            player.UpdateSensitivity(val);
        }
    }

    public void Show()
    {
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        ShowPanel(pausePanel);

        // show which predator mode is active
        if (predatorModeText != null)
        {
            bool bt = PlayerPrefs.GetInt("UseBTPredator", 0) == 1;
            predatorModeText.text = bt ? "Mode: Behaviour Tree" : "Mode: ML Agent";
        }
    }

    public void Hide()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (pauseOptionsPanel != null) pauseOptionsPanel.SetActive(false);
    }

    // audio mixer helpoer
    private void SetMixerVolume(string parameterName, float sliderValue)
    {
        float dB = Mathf.Log10(Mathf.Max(sliderValue, 0.0001f)) * 20;
        mainMixer.SetFloat(parameterName, dB);
    }

    void ShowPanel(GameObject panel)
    {
        pausePanel?.SetActive(panel == pausePanel);
        pauseOptionsPanel?.SetActive(panel == pauseOptionsPanel);
    }
}
