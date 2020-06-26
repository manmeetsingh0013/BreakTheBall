using System.Collections;
using UnityEngine;
using UnitySceneManagement = UnityEngine.SceneManagement;
using UnityEngine.UI;
using System;

namespace MightyDoodle.Rewards.RewardGames
{
    /// <summary>
    /// This script triggers event for game start, restart, gameover, going to next level(for level clearing games like Odd one out, Dot connect) and pause/unpause events.
    /// This script also handles the UI for game start, restart, gameover, level clear and pause and the in-game UI. The original game should have only in-game UI and other screens should be removed.
    /// This script also handles timer for the reward games
    /// </summary>

    public class RewardGameManager : MonoBehaviour
    {
        [SerializeField]
        public static RewardGameManager Instance;

        public delegate void eventHandler();
        public static event eventHandler GameStart;
        public static event eventHandler GameRestart;
        public static event eventHandler ProceedToNextLevel;
        public static event eventHandler GameOver;
        public static event eventHandler GameSessionFinish;

        public delegate void boolEventHandler(bool i);
        public static event boolEventHandler GamePauseStateChanged;

        [Header("Info")]
        [SerializeField]
        string gameName;
        [SerializeField]
        string message = "Time To Play!";
        [SerializeField]
        Sprite gameIcon;
        [SerializeField]
        AudioClip startClip;
        [SerializeField]
        public Sprite collectibleItem;

        [Header("Start canvas")]
        [Space]

        [Header("All the below fields are supposed to be fixed and hence, should be changed only in prefab mode")]
        [SerializeField]
        GameObject gameStartCanvas;
        [SerializeField]
        Image gameStartGameIcon;
        [SerializeField]
        Text gameTitle;
        [SerializeField]
        Text gameMessage;

        [Header("Restart Canvas")]
        [SerializeField]
        GameObject gameRestartCanvas;
        [SerializeField]
        Image gameRestartGameIcon;
        [SerializeField]
        Text scoreText;
        [SerializeField]
        Text highscoreText;
        [SerializeField]
        Text totalLevelsClearedText;
        [SerializeField]
        Transform gameRestartCollectibleItem;

        [Header("Level cleared")]
        [SerializeField]
        GameObject levelClearedCanvas;
        [SerializeField]
        Image levelClearedGameIcon;
        [SerializeField]
        Text levelsClearedText;
        [SerializeField]
        Transform levelClearedCollectibleItem;

        [Header("Gameover Canvas")]
        [SerializeField]
        GameObject gameOverCanvas;
        [SerializeField]
        Image gameOverGameIcon;
        [SerializeField]
        Text gameoverScoreText;
        [SerializeField]
        Text gameoverHighscoreText;
        [SerializeField]
        Text gameoverLevelClearedText;
        [SerializeField]
        Transform gameOverCollectibleItem;

        [Header("Pause Canvas")]
        [SerializeField]
        GameObject pauseCanvas;

        [Header("Timer")]
        [SerializeField]
        float warningInitiatingTime = 30; //In seconds
        [SerializeField]
        float timeLimitInMinutes = 3;
        [SerializeField]
        Animation timerAnimation;
        [SerializeField]
        Image timerBar;
        [SerializeField]
        AudioSource warningAudio;
        public float timeLeft;
        public bool isPaused;

        [Header("Game specific canvas")]
        [SerializeField]
        GameObject gameCanvas;

        AudioSource startAudioSource;
        float timerAnimationLength;
        string timerTimeOutAnimationName = "TimeUpClock";
        string timerWarningAnimationName = "TimerWarning"; //Dont change the name of this as it is used in multiple animation controller
        bool didTimerStart, timeRunning, isWarningSetOnTimer = false;

        private void Awake()
        {
            Time.timeScale = 1;
            isWarningSetOnTimer = false;

            if (Instance == null)
                Instance = this;
            else if (Instance != this)
            {
                DestroyImmediate(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);

            GetReferences();
            SetAllGameCanvasProperties();
            ResetCanvases();

            if (gameCanvas != null)
                gameCanvas.SetActive(false);
        }

        void GetReferences()
        {
            startAudioSource = gameStartCanvas.GetComponent<AudioSource>();
        }

        void SetAllGameCanvasProperties()
        {
            gameTitle.text = gameName;
            gameMessage.text = message;

            if (gameIcon != null)
            {
                gameStartGameIcon.sprite = gameIcon;
                gameStartGameIcon.gameObject.SetActive(true);

                gameRestartGameIcon.sprite = gameIcon;
                gameRestartGameIcon.gameObject.SetActive(true);

                levelClearedGameIcon.sprite = gameIcon;
                levelClearedGameIcon.gameObject.SetActive(true);

                gameOverGameIcon.sprite = gameIcon;
                gameOverGameIcon.gameObject.SetActive(true);
            }
            else
            {
                gameStartGameIcon.gameObject.SetActive(false);
                gameRestartGameIcon.gameObject.SetActive(false);
                levelClearedGameIcon.gameObject.SetActive(false);
                gameOverGameIcon.gameObject.SetActive(false);
            }

            if (collectibleItem != null)
            {
                gameRestartCollectibleItem.GetChild(0).GetComponent<Image>().sprite = collectibleItem;
                gameRestartCollectibleItem.gameObject.SetActive(true);

                levelClearedCollectibleItem.GetChild(0).GetComponent<Image>().sprite = collectibleItem;
                levelClearedCollectibleItem.gameObject.SetActive(true);

                gameOverCollectibleItem.GetChild(0).GetComponent<Image>().sprite = collectibleItem;
                gameOverCollectibleItem.gameObject.SetActive(true);
            }
            else
            {
                gameRestartCollectibleItem.gameObject.SetActive(false);
                levelClearedCollectibleItem.gameObject.SetActive(false);
                gameOverCollectibleItem.gameObject.SetActive(false);
            }
        }

        void ResetCanvases()
        {
            gameStartCanvas.SetActive(true);
            gameRestartCanvas.SetActive(false);
            levelClearedCanvas.SetActive(false);
            gameOverCanvas.SetActive(false);
            pauseCanvas.SetActive(false);
        }

        private void OnEnable()
        {
            //SceneManager.sceneLoaded += OnSceneLoad;
        }

        private void OnDisable()
        {
            //SceneManager.sceneLoaded -= OnSceneLoad;
        }

        void OnSceneLoad(UnitySceneManagement.Scene scene, UnitySceneManagement.LoadSceneMode mode)
        {

            GetGameCanvas();
        }

        void GetGameCanvas()
        {
            if (gameCanvas == null)
                gameCanvas = GameObject.FindGameObjectWithTag("InGameUI");
        }

        private void Start()
        {
            GetGameCanvas();
            if (gameCanvas != null)
                gameCanvas.SetActive(false);

            if (startClip != null)
            {
                startAudioSource.clip = startClip;
                startAudioSource.Play();
            }
        }

        private void Update()
        {
            UpdateTimer();
        }

        void UpdateTimer()
        {
            if (timeLeft > 0 && timeRunning)
            {
                // Count down the time
                timeLeft -= Time.deltaTime;
                UpdateTimerIcon();

                if (timeLeft <= 0)
                {
                    timeRunning = false;
                    OnTimeOut();
                }
                else if (timeLeft <= warningInitiatingTime && !isWarningSetOnTimer)
                {
                    if (timerAnimation)
                    {
                        isWarningSetOnTimer = true;
                        InitiateTimerWarningAnim();
                    }
                }
            }
            else
            {

            }
        }

        void UpdateTimerIcon()
        {
            // Update the timer circle, if we have one
            if (timerBar)
            {
                // If the timer is running, display the fill amount left. Otherwise refill the amount back to 100%
                timerBar.fillAmount = timeLeft / (timeLimitInMinutes * 60);
            }

            // Play animation when time's up
            if (timeLeft <= 0)
            {
                if (timerAnimation)
                {
                    isWarningSetOnTimer = false;
                    InitiateTimerTimeOutAnim();
                }
            }
        }

        void OnTimeOut()
        {
            pauseCanvas.SetActive(false);
            if (gameCanvas != null)
                gameCanvas.SetActive(false);

            PauseTheGameOnTimeOut();
            Invoke("OnTimerAnimationComplete", timerAnimationLength);
        }

        void InitiateTimerWarningAnim()
        {
            timerAnimation.wrapMode = WrapMode.Loop;
            timerAnimation.Play(timerWarningAnimationName, PlayMode.StopAll);
            warningAudio.Play();
        }

        void InitiateTimerTimeOutAnim()
        {
            timerAnimation.wrapMode = WrapMode.Once;
            timerAnimation.Play(timerTimeOutAnimationName, PlayMode.StopAll);
            timerAnimationLength = timerAnimation[timerTimeOutAnimationName].length;
        }

        void PauseTheGameOnTimeOut()
        {
            isPaused = true;
            AudioListener.pause = isPaused;
            if (GamePauseStateChanged != null)
                GamePauseStateChanged(isPaused);
        }

        void OnTimerAnimationComplete()
        {
            Time.timeScale = 0;
            if (GameOver != null)
                GameOver();
        }

        public void OnPlay()
        {
            Time.timeScale = 1;
            startAudioSource.Stop();

            if (GameStart != null)
                GameStart();

            gameStartCanvas.SetActive(false);
            EnableInGameCanvas();
        }

        void EnableInGameCanvas()
        {
            pauseCanvas.SetActive(true);

            if (gameCanvas != null)
                gameCanvas.SetActive(true);
        }

        public void StartTimer()
        {
            // A condition to prevent timer from starting from first again, in case any games call it multiple times
            if (didTimerStart)
                return;

            timeLeft = timeLimitInMinutes * 60;
            didTimerStart = timeRunning = true;
        }

        public void OnRestart()
        {
            Time.timeScale = 1;

            if (GameRestart != null)
                GameRestart();
            gameRestartCanvas.SetActive(false);
            EnableInGameCanvas();
        }

        public void Pause()
        {
            isPaused = !isPaused;
            if (GamePauseStateChanged != null)
                GamePauseStateChanged(isPaused);
        }

        public void HandleIfGameIsAlreadyPlayed()
        {
            gameStartCanvas.SetActive(false);
            EnableInGameCanvas();
        }

        /// <summary>
        /// When the game is over, call this function to show the restart popup with game scores. If levels are there convert them to score
        /// </summary>
        /// <param name="score"></param>
        /// <param name="highscore"></param>
        /// <param name="levelsCleared"></param>
        public void RestartGame(int score = -1)
        {
            try
            {
                pauseCanvas.SetActive(false);
                if (gameCanvas != null)
                    gameCanvas.SetActive(false);

                ShowGameScores(score);

                gameRestartCanvas.SetActive(true);
                Time.timeScale = 0;
            }
            catch(Exception e)
            {
                Debug.LogError(e.Message);
            }
        }

        void ShowGameScores(int score)
        {
            if (score != -1)
            {
                if (collectibleItem != null)
                {
                    gameRestartCollectibleItem.GetChild(1).GetComponent<Text>().text = score.ToString();
                    gameRestartCollectibleItem.gameObject.SetActive(true);
                    scoreText.gameObject.SetActive(false);
                }
                else
                {
                    scoreText.text = "Score: " + score.ToString();
                    scoreText.gameObject.SetActive(true);
                    gameRestartCollectibleItem.gameObject.SetActive(false);
                }
                gameRestartGameIcon.rectTransform.anchoredPosition = new Vector2(-60, -11);
            }
            else
            {
                scoreText.gameObject.SetActive(false);
                gameRestartCollectibleItem.gameObject.SetActive(false);
                gameRestartGameIcon.rectTransform.anchoredPosition = new Vector2(0, -11);
            }

            highscoreText.gameObject.SetActive(false);
            totalLevelsClearedText.gameObject.SetActive(false);
        }

        /// <summary>
        /// When a level is cleared in the game, call this to show level cleated popup with the level cleared paramter
        /// </summary>
        /// <param name="levelCleared"></param>
        public void LevelCleared(int levelCleared)
        {
            pauseCanvas.SetActive(false);
            if (gameCanvas != null)
                gameCanvas.SetActive(false);

            if (collectibleItem != null)
            {
                levelClearedCollectibleItem.GetChild(1).GetComponent<Text>().text = levelCleared.ToString();
                levelClearedCollectibleItem.gameObject.SetActive(true);
                levelsClearedText.gameObject.SetActive(false);
            }
            else
            {
                levelsClearedText.text = "Levels Cleared: " + levelCleared.ToString();
                levelsClearedText.gameObject.SetActive(true);
                levelClearedCollectibleItem.gameObject.SetActive(false);
            }
            levelClearedCanvas.SetActive(true);
            Time.timeScale = 0;
        }

        /// <summary>
        /// When it's timedout and GameOver event is called, call this function to show gameover popup with game scores. If levels are there convert to score
        /// </summary>
        /// <param name="score"></param>
        /// <param name="highScore"></param>
        /// <param name="levelsCleared"></param>
        public void ShowGameOverCanvas(int score = -1)
        {
            if (score != -1)
            {
                if (collectibleItem != null)
                {
                    gameOverCollectibleItem.GetChild(1).GetComponent<Text>().text = score.ToString();
                    gameOverCollectibleItem.gameObject.SetActive(true);
                    gameoverScoreText.gameObject.SetActive(false);
                }
                else
                {
                    gameoverScoreText.text = "Score: " + score.ToString();
                    gameoverScoreText.gameObject.SetActive(true);
                    gameOverCollectibleItem.gameObject.SetActive(false);
                }
                gameOverGameIcon.rectTransform.anchoredPosition = new Vector2(-60, -11);
            }
            else
            {
                gameoverScoreText.gameObject.SetActive(false);
                gameOverCollectibleItem.gameObject.SetActive(false);
                gameOverGameIcon.rectTransform.anchoredPosition = new Vector2(0, -11);
            }

            gameoverHighscoreText.gameObject.SetActive(false);
            gameoverLevelClearedText.gameObject.SetActive(false);

            gameOverCanvas.SetActive(true);
        }

        public void OnPlayNextLevel()
        {
            Time.timeScale = 1;
            levelClearedCanvas.SetActive(false);
            pauseCanvas.SetActive(true);
            if (gameCanvas != null)
                gameCanvas.SetActive(true);

            if (ProceedToNextLevel != null)
                ProceedToNextLevel();
        }

        public void OnFinish()
        {
            StartCoroutine(ProceedToNextScene());
        }

        IEnumerator ProceedToNextScene()
        {
            if (GameSessionFinish != null)
                GameSessionFinish();

            Time.timeScale = 1;

            yield return new WaitForEndOfFrame();

            AudioListener.pause = false;

            Destroy(gameObject);
        }

        void OnDestroy()
        {
            StopAllCoroutines();
        }
    }
}