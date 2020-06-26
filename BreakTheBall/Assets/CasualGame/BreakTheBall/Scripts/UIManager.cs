using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using MightyDoodle.Rewards.RewardGames;

namespace PaintTheRings
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { private set; get; }

        //Gameplay UI
        [SerializeField]
        private GameObject gameplayUI;
        [SerializeField]
        Image collectibleItem;
        [SerializeField]
        private Text currentLevelTxt;
        [SerializeField]
        private Text ringCountTxt;
        [SerializeField]
        private Image timeBarImg;

        private Coroutine timebarCoroutine = null;

        public bool isGameOver { get;private set; }

        private void OnEnable()
        {
            RewardGameManager.GameStart += OnGameStart;
            RewardGameManager.GameRestart += OnRestart;
            RewardGameManager.ProceedToNextLevel += OnProceedToNextLevel;
            RewardGameManager.GamePauseStateChanged += GamePauseStatusChanged;
            RewardGameManager.GameOver += OnGameOver;

            GameController.GameStateChanged += GameStateChanged;
        }

        private void OnDisable()
        {
            RewardGameManager.GameStart -= OnGameStart;
            RewardGameManager.GameRestart -= OnRestart;
            RewardGameManager.ProceedToNextLevel -= OnProceedToNextLevel;
            RewardGameManager.GamePauseStateChanged -= GamePauseStatusChanged;
            RewardGameManager.GameOver -= OnGameOver;

            GameController.GameStateChanged -= GameStateChanged;
        }

        void OnGameStart()
        {
            RewardGameManager.Instance.StartTimer();
            GameController.Instance.PlayingGame();
        }

        void OnRestart()
        {
            GameController.Instance.LoadScene(SceneManager.GetActiveScene().name, 0.1f);
        }

        void OnProceedToNextLevel()
        {
            GameController.Instance.IncreaseCurrentLevel();
            GameController.Instance.LoadScene(SceneManager.GetActiveScene().name, 0.1f);
        }

        void OnGameOver()
        {
            isGameOver = true;

            RewardGameManager.Instance.ShowGameOverCanvas(GameController.CurrentLevel - 1);
            GameController.CurrentLevel = 0;
        }

        void PreLvlBtn()
        {
            GameController.Instance.DecreaseCurrentLevel();
            GameController.Instance.LoadScene(SceneManager.GetActiveScene().name, 0.1f);
        }

        void GamePauseStatusChanged(bool isPaused)
        {
            if (isPaused)
                OnPause();
            else
                OnUnPause();
        }

        void OnPause()
        {
            GameController.Instance.PauseGame();
        }

        void OnUnPause()
        {
            GameController.Instance.UnPauseGame();
        }

        private void GameStateChanged(GameState obj)
        {
            if (obj == GameState.GameOver)
                StartCoroutine(ShowGameOverUI(0.5f));
            else if (obj == GameState.PassLevel)
                StartCoroutine(ShowPassLevelUI(0.5f));
            else if (obj == GameState.Playing)
                gameplayUI.SetActive(true);
        }

        private IEnumerator ShowGameOverUI(float delay)
        {
            yield return new WaitForSeconds(delay);
            //Debug.Log(RewardGameManager.Instance);

            RewardGameManager.Instance.RestartGame(GameController.CurrentLevel - 1);
            yield break;
        }

        private IEnumerator ShowPassLevelUI(float delay)
        {
            yield return new WaitForSeconds(delay);
            currentLevelTxt.text = GameController.CurrentLevel.ToString();
            RewardGameManager.Instance.LevelCleared(GameController.CurrentLevel);
            yield break;
        }

        void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
            {
                DestroyImmediate(Instance.gameObject);
                Instance = this;
            }
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        void Start()
        {
            collectibleItem.sprite = RewardGameManager.Instance.collectibleItem;
            if (!GameController.IsRestart) // This is the first load
                gameplayUI.SetActive(false);
        }

        public void PlayButtonSound()
        {
            SoundManager.Instance.PlaySound(SoundManager.Instance.button);
        }

        /// <summary>
        /// Update level text on Gameplay UI
        /// </summary>
        /// <param name="currentLevel"></param>
        public void UpdateLevel(int currentLevel)
        {
            currentLevelTxt.text = (currentLevel - 1).ToString();
        }

        /// <summary>
        /// Update ringCount text on UI
        /// </summary>
        public void UpdateRingCount(int currentCount, int maxCount)
        {
            ringCountTxt.text = currentCount.ToString() + " / " + maxCount.ToString();
        }

        /// <summary>
        /// Stat running coundown timebar coroutine
        /// </summary>
        /// <param name="time"></param>
        public void StartRunTimebar(float time)
        {
            timebarCoroutine = StartCoroutine(RunningTimebar(time));
        }

        private IEnumerator RunningTimebar(float time)
        {
            timeBarImg.fillAmount = 1;
            float t = 0;
            while (t < time)
            {
                t += Time.deltaTime;
                float factor = t / time;
                timeBarImg.fillAmount = Mathf.Lerp(1, 0, factor);
                yield return null;

                while (GameController.Instance.GameState != GameState.Playing)
                {
                    yield return null;
                }
            }
        }

        /// <summary>
        /// Stop counting down timebar coroutine
        /// </summary>
        public void StopCountTimebar()
        {
            StopCoroutine(timebarCoroutine);
            timebarCoroutine = null;
        }
    }
}