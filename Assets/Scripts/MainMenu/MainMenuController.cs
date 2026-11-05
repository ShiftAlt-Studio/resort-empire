using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] GameObject mainButtons;
    [SerializeField] GameObject settingsPanel;

    [Header("Buttons")]
    [SerializeField] Button playButton;
    [SerializeField] Button settingsButton;
    [SerializeField] Button quitButton;
    [SerializeField] Button backButton;

    [Header("Scene")]
    [SerializeField] string gameSceneName = "Island View";
    [SerializeField] float loadDelay = 0.15f;

    [Header("Audio")]
    [SerializeField] AudioSource music;
    [SerializeField] AudioSource sfx;
    [SerializeField] AudioClip clickClip;

    void Awake()
    {
        playButton.onClick.AddListener(OnPlay);
        settingsButton.onClick.AddListener(OnSettings);
        backButton.onClick.AddListener(OnBack);

#if UNITY_IOS
        quitButton.gameObject.SetActive(false);
#else
        quitButton.onClick.AddListener(OnQuit);
#endif
    }

    void OnEnable()
    {
        ShowMain();
        playButton.interactable = true;
        if (music != null && !music.isPlaying) music.Play();
    }

    void ShowMain()
    {
        mainButtons.SetActive(true);
        settingsPanel.SetActive(false);
    }

    void OnPlay()
    {
        Click();
        playButton.interactable = false;

        if (!Application.CanStreamedLevelBeLoaded(gameSceneName))
        {
            Debug.LogError($"Scene '{gameSceneName}' not found. Add it in Build Profiles > Scene List.");
            playButton.interactable = true;
            return;
        }
        StartCoroutine(LoadGame());
    }

    IEnumerator LoadGame()
    {
        yield return new WaitForSeconds(loadDelay);
        SceneManager.LoadScene(gameSceneName);
    }

    void OnSettings()
    {
        Click();
        mainButtons.SetActive(false);
        settingsPanel.SetActive(true);
    }

    void OnBack()
    {
        Click();
        ShowMain();
    }

    void OnQuit()
    {
        Click();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void Click()
    {
        if (sfx != null && clickClip != null) sfx.PlayOneShot(clickClip);
    }
}