using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.Audio;

public class MainMenuUI : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainPanel;
    public GameObject optionsPanel;
    public GameObject creditsPanel;
    public GameObject modeSelectPanel;

    [Header("Mode Select")]
    public Button MLPredatorButton;
    public Button BTPredatorButton;
    public Button playButton;
    public Text modeDescriptionText;
    public Button modeBackButton;

    [Header("Main Buttons")]
    public Button startButton;
    public Button optionsButton;
    public Button creditsButton;
    public Button quitButton;

    [Header("Audio")]
    public AudioMixer mainMixer;

    [Header("Options - Volume")]
    public Slider masterVolumeSlider;
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;

    [Header("Options - sens")]
    public Slider sensitivitySlider;

    [Header("Options - Controls")]
    public Button rebindMoveUpButton;
    public Button rebindMoveDownButton;
    public Button rebindMoveLeftButton;
    public Button rebindMoveRightButton;
    public Button rebindSprintButton;
    public Button rebindCrouchButton;
    public Button rebindJumpButton;

    public Text rebindMoveUpText;
    public Text rebindMoveDownText;
    public Text rebindMoveLeftText;
    public Text rebindMoveRightText;
    public Text rebindSprintText;
    public Text rebindCrouchText;
    public Text rebindJumpText;

    public Button optionsBackButton;

    [Header("Input Asset")]
    public InputActionAsset inputAsset;

    [Header("Credits")]
    public Button creditsBackButton;

    [Header("Game Scene Name")]
    public string gameSceneName = "GameScene";

    private InputActionRebindingExtensions.RebindingOperation rebindingOperation;

    private const int MoveUpIdx = 2;
    private const int MoveDownIdx = 3;
    private const int MoveLeftIdx = 4;
    private const int MoveRightIdx = 5;

    private const int SprintIdx = 0;
    private const int CrouchIdx = 0;
    private const int JumpIdx = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ShowPanel(mainPanel);

        startButton?.onClick.AddListener(() => ShowPanel(modeSelectPanel));
        optionsButton?.onClick.AddListener(() => { ShowPanel(optionsPanel); RefreshBindingLabels(); });
        creditsButton?.onClick.AddListener(() => ShowPanel(creditsPanel));
        quitButton?.onClick.AddListener(Application.Quit);
        optionsBackButton?.onClick.AddListener(() => ShowPanel(mainPanel));
        creditsBackButton?.onClick.AddListener(() => ShowPanel(mainPanel));
        modeBackButton?.onClick.AddListener(() => ShowPanel(mainPanel));

        MLPredatorButton?.onClick.AddListener(SelectMLPredator);
        BTPredatorButton?.onClick.AddListener(SelectBTPredator);

        playButton?.onClick.AddListener(LaunchGame);

        // load volume settings
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

        LoadSavedOverrides();

        rebindMoveUpButton?.onClick.AddListener(() => StartRebind("Move", MoveUpIdx, rebindMoveUpText));
        rebindMoveDownButton?.onClick.AddListener(() => StartRebind("Move", MoveDownIdx, rebindMoveDownText));
        rebindMoveLeftButton?.onClick.AddListener(() => StartRebind("Move", MoveLeftIdx, rebindMoveLeftText));
        rebindMoveRightButton?.onClick.AddListener(() => StartRebind("Move", MoveRightIdx, rebindMoveRightText));
        rebindSprintButton?.onClick.AddListener(() => StartRebind("Sprint", SprintIdx, rebindSprintText));
        rebindCrouchButton?.onClick.AddListener(() => StartRebind("Crouch", CrouchIdx, rebindCrouchText));
        rebindJumpButton?.onClick.AddListener(() => StartRebind("Jump", JumpIdx, rebindJumpText));

        SelectMLPredator(); // default to ML predator
        RefreshBindingLabels();
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

    void ShowPanel(GameObject panel)
    {
        mainPanel?.SetActive(panel == mainPanel);
        optionsPanel?.SetActive(panel == optionsPanel);
        creditsPanel?.SetActive(panel == creditsPanel);
        modeSelectPanel?.SetActive(panel == modeSelectPanel);
    }

    void SelectMLPredator()
    {
        PlayerPrefs.SetInt("UseBTPredator", 0);
        if (modeDescriptionText != null)
            modeDescriptionText.text = "ML Agent Predator \n Trained through reinforcement learning over 18 million steps. behaviour emerges from experience - unpredictable and adaptive.";

        HighlightModeButton(MLPredatorButton, BTPredatorButton);
    }

    void SelectBTPredator()
    {
        PlayerPrefs.SetInt("UseBTPredator", 1);
        if (modeDescriptionText != null)
            modeDescriptionText.text = "Behaviour Tree Predator \n Hand crafted decision logic using a priority ordered behaviour tree with NavMesh pathfinding. Rule based and deterministic.";

        HighlightModeButton(BTPredatorButton, MLPredatorButton);
    }

    void HighlightModeButton(Button selected, Button other)
    {
        if (selected != null)
        {
            ColorBlock cb = selected.colors;
            cb.normalColor = new Color(0.8f, 0.2f, 0.1f);
            selected.colors = cb;
        }
        if (other != null)
        {
            ColorBlock cb = other.colors;
            cb.normalColor = new Color(0.25f, 0.25f, 0.25f);
            other.colors = cb;
        }
    }

    public void LaunchGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    // =====================================
    //          control rebinding
    // =====================================

    public void StartRebind(string actionName, int bindingIndex, Text label)
    {
        // cancel rebinding
        rebindingOperation?.Cancel();
        rebindingOperation?.Dispose();

        var action = inputAsset.FindAction(actionName);
        if (action == null)
        {
            Debug.Log($"[Rebind] could not find action '{actionName}' in {inputAsset.name}");
            return;
        }


        action.Disable();

        if (label != null) label.text = " Press any 🔑... ";

        rebindingOperation = action
            .PerformInteractiveRebinding(bindingIndex)
            .WithControlsExcluding("<Pointer>/position")
            .WithControlsExcluding("<Pointer>/delta")
            .WithControlsExcluding("<Mouse>/position")
            .WithCancelingThrough("<Keyboard>/escape")
            .OnMatchWaitForAnother(0.1f)
            .OnComplete(op =>
            {
                action.Enable();

                string overrideJson = action.actionMap.asset.SaveBindingOverridesAsJson();
                PlayerPrefs.SetString("InputOverrides", overrideJson);
                PlayerPrefs.Save();

                RefreshBindingLabels();
                rebindingOperation.Dispose();
                rebindingOperation = null;

                Debug.Log($"[Rebind] {actionName}[{bindingIndex}] rebound to {action.bindings[bindingIndex].effectivePath}");
            })
            .OnCancel(op =>
            {
                action.Enable();
                RefreshBindingLabels();
                rebindingOperation.Dispose();
                rebindingOperation = null;
            })
            .Start();
    }

    void RefreshBindingLabels()
    {
        if (inputAsset == null) return;
        SetLabel(rebindMoveUpText, "Move", MoveUpIdx);
        SetLabel(rebindMoveDownText, "Move", MoveDownIdx);
        SetLabel(rebindMoveLeftText, "Move", MoveLeftIdx);
        SetLabel(rebindMoveRightText, "Move", MoveRightIdx);
        SetLabel(rebindSprintText, "Sprint", SprintIdx);
        SetLabel(rebindCrouchText, "Crouch", CrouchIdx);
        SetLabel(rebindJumpText, "Jump", JumpIdx);

    }

    void SetLabel(Text label, string actionName, int bindingIndex)
    {
        if (label == null) return;
        var action = inputAsset.FindAction(actionName);
        if (action == null) { label.text = "?"; return; }
        label.text = InputControlPath.ToHumanReadableString(action.bindings[bindingIndex].effectivePath, InputControlPath.HumanReadableStringOptions.OmitDevice);
    }

    void LoadSavedOverrides()
    {
        if (inputAsset == null) return;
        string json = PlayerPrefs.GetString("InputOverrides", "");
        if (!string.IsNullOrEmpty(json))
        {
            inputAsset.LoadBindingOverridesFromJson(json);
            Debug.Log("[Rebind] loaded saved overrides");
        }
    }

    // audio mixer helpoer
    private void SetMixerVolume(string parameterName, float sliderValue)
    {
        float dB = Mathf.Log10(Mathf.Max(sliderValue, 0.0001f)) * 20;
        mainMixer.SetFloat(parameterName, dB);
    }

    private void OnDestroy()
    {
        rebindingOperation?.Cancel();
        rebindingOperation?.Dispose();
    }
}
