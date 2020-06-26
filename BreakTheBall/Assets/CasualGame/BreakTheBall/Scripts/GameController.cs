using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using MightyDoodle.Rewards.RewardGames;

namespace PaintTheRings
{
    public enum GameState
    {
        Prepare,
        Playing,
        Pause,
        Revive,
        PassLevel,
        GameOver,
    }

    [System.Serializable]
    public struct LevelData
    {
        public int MinLevel;
        public int MaxLevel;
        public int MinRingNumber;
        public int MaxRingNumber;
        [Range(0, 8)] public int MinPaintedPiece;
        [Range(1, 8)] public int MaxPaintedPiece;
        [Range(1, 10)] public int MinPaintedBall;
        [Range(2, 10)] public int MaxPaintedBall;
        [Range(0, 360)] public float MinRotatingDegrees;
        [Range(0, 360)] public float MaxRotatingDegrees;
        public float MinRotatingSpeed;
        public float MaxRotatingSpeed;
        public int MinTimeToPaintOneRing;
        public float MaxTimeToPaintOneRing;
        public LerpType[] RotatingTypes;
        public Color[] RingColors;
    }

    public class GameController : MonoBehaviour
    {
        public static GameController Instance { private set; get; }
        public static event System.Action<GameState> GameStateChanged = delegate { };
        public static int CurrentLevel { set; get; }
        public static bool IsRestart { private set; get; }

        private const string MaxPassedLevel_PPK = "MaxPassedLevel";

        public GameState GameState
        {
            get
            {
                return gameState;
            }
            private set
            {
                if (value != gameState)
                {
                    gameState = value;
                    GameStateChanged(gameState);
                }
            }
        }

        [Header("Gameplay Testing")]
        [Header("Put a level number to test that level. Set 0 to disable this feature.")]
        [SerializeField] private int testingLevel = 0;

        [Header("Gameplay Config")]
        [SerializeField] private float ringMoveDownTime = 0.25f;
        [SerializeField] private float ringYPosition = 12f;
        [SerializeField] private float touchDelayTime = 0.1f;
        [SerializeField] private float paintedBallShootingSpeed = 60f;
        [SerializeField] private float paintedBallZPosition = -11f;
        [SerializeField] private float paintedBallSpace = 1f;
        [SerializeField] private float fadingCircleScale = 3f;
        [SerializeField] private float fadingRingScale = 15f;
        [SerializeField] private float circleFadingtime = 0.5f;
        [SerializeField] private float ringFadingTime = 1f;
        [SerializeField] private int[] savedLevels = null;
        [SerializeField] private LevelData[] levelsData = null;

        [Header("Gameplay References")]
        [SerializeField] private Transform rotatorTrans = null;
        [SerializeField] private Material paintedBallMaterial = null;
        [SerializeField] private GameObject ringPrefab = null;
        [SerializeField] private GameObject paintedBallPrefab = null;
        [SerializeField] private GameObject paintedBallExplodePrefab = null;
        [SerializeField] private GameObject fadingCirclePrefab = null;
        [SerializeField] private GameObject fadingRingPrefab = null;
        [SerializeField] private GameObject paintedBallImgPrefab = null;

        public LevelData CurrentLevelData { private set; get; }
        public Material CurrentRingMaterial { private set; get; }
        public float RingPieceHeight { private set; get; }
        public bool IsOutOfPaintedBall { private set; get; }

        private GameState gameState = GameState.GameOver;
        private List<PaintedBallController> listPaintedBallControl = new List<PaintedBallController>();
        private List<FadingObjectController> listFadingCircleControl = new List<FadingObjectController>();
        private List<FadingObjectController> listFadingRingControl = new List<FadingObjectController>();
        private List<ParticleSystem> listPaintedBallExplode = new List<ParticleSystem>();
        private Coroutine countDownTimeToPaint = null;
        private int ringNumber = 0;
        private int ringCount = 1;
        private int previousColorIndex = -1;
        private bool disableTouch = false;

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
            RewardGameManager.GameSessionFinish += OnGameSessionFinish;

            // Fire event
            GameState = GameState.Prepare;
            gameState = GameState.Prepare;

            // Set current level
            if (!IsRestart)
                CurrentLevel = 1;

            if (testingLevel != 0)
                CurrentLevel = testingLevel;

            // Set currentLevelData, CurrentRingData
            foreach (LevelData o in levelsData)
            {
                if (CurrentLevel >= o.MinLevel && CurrentLevel < o.MaxLevel)
                {
                    CurrentLevelData = o;
                    ringNumber = Random.Range(CurrentLevelData.MinRingNumber, CurrentLevelData.MaxRingNumber);
                    break;
                }
            }

            // Set properties
            CurrentRingMaterial = GetRandomMaterial();
            RingPieceHeight = ringPrefab.transform.GetChild(0).GetComponent<MeshRenderer>().bounds.size.y;
            IsOutOfPaintedBall = false;

            //Set color for painted ball
            paintedBallMaterial.color = CurrentRingMaterial.color;

            //Update texts
            UIManager.Instance.UpdateLevel(CurrentLevel);
            UIManager.Instance.UpdateRingCount(ringCount, ringNumber);

            if (IsRestart)
                PlayingGame();
        }

        void OnGameSessionFinish()
        {
            RewardGameManager.GameSessionFinish -= OnGameSessionFinish;
            ResetStaticVariables();
            //Instance = null;
            //DestroyImmediate(gameObject);
        }

        void ResetStaticVariables()
        {
            IsRestart = false;
            CurrentLevel = 0;
        }

        /// <summary>
        /// Get material for the current ring
        /// </summary>
        /// <returns></returns>
        private Material GetRandomMaterial()
        {
            int index = Random.Range(0, CurrentLevelData.RingColors.Length);
            while (index == previousColorIndex)
            {
                index = Random.Range(0, CurrentLevelData.RingColors.Length);
            }
            previousColorIndex = index;
            Color ringColor = CurrentLevelData.RingColors[index];
            Material mat = new Material(Shader.Find("Legacy Shaders/Self-Illumin/Diffuse"));
            mat.color = ringColor;
            return mat;
        }

        /// <summary>
        /// Actual start the game
        /// </summary>
        public void PlayingGame()
        {
            //Fire event
            GameState = GameState.Playing;
            gameState = GameState.Playing;

            StartCoroutine(PlayBackgroundMusic());

            CreateNextRing(); //Create a ring
            CreatePaintedBall(); //Create painted balls
            StartCoroutine(WaitAndRunCountTimebar());
        }

        private IEnumerator PlayBackgroundMusic()
        {
            yield return new WaitForSeconds(0.5f);
            if (SoundManager.Instance.background != null)
                SoundManager.Instance.PlayMusic(SoundManager.Instance.background);
        }

        /// <summary>
        /// Create a ring at start position and move it to end position
        /// </summary>
        private void CreateNextRing()
        {
            Vector3 pos = rotatorTrans.position + Vector3.up * ringYPosition;
            RingController ring = Instantiate(ringPrefab, pos, Quaternion.identity).GetComponent<RingController>();
            ring.MoveToRotatorPosition(ringMoveDownTime, Random.Range(CurrentLevelData.MinPaintedPiece, CurrentLevelData.MaxPaintedPiece));
            disableTouch = true;
            StartCoroutine(WaitAndEnableTouch(ringMoveDownTime));
        }

        /// <summary>
        /// Wait a given time then enable touch
        /// </summary>
        /// <param name="delay"></param>
        /// <returns></returns>
        private IEnumerator WaitAndEnableTouch(float delay)
        {
            yield return new WaitForSeconds(delay);
            yield return null;
            disableTouch = false;
        }

        /// <summary>
        /// Create all painted ball and line them backward
        /// </summary>
        private void CreatePaintedBall()
        {
            //Create paited balls
            int number = Random.Range(CurrentLevelData.MinPaintedBall, CurrentLevelData.MaxPaintedBall);
            Vector3 pos = new Vector3(0, 0, paintedBallZPosition);
            for (int i = 0; i < number; i++)
            {
                GameObject paintedBall = Instantiate(paintedBallPrefab, pos, Quaternion.identity);
                listPaintedBallControl.Add(paintedBall.GetComponent<PaintedBallController>());
                pos = paintedBall.transform.position + Vector3.back * paintedBallSpace;
            }
        }

        /// <summary>
        /// Wait for the ring move down and run count down timebar coroutine
        /// </summary>
        /// <returns></returns>
        private IEnumerator WaitAndRunCountTimebar()
        {
            yield return new WaitForSeconds(ringMoveDownTime);
            float _timeToPaint = Random.Range(CurrentLevelData.MinTimeToPaintOneRing, CurrentLevelData.MaxTimeToPaintOneRing);
            UIManager.Instance.StartRunTimebar(_timeToPaint);
            if (countDownTimeToPaint != null)
                StopCoroutine(countDownTimeToPaint);
            countDownTimeToPaint = null;
            countDownTimeToPaint = StartCoroutine(CountingDownTimeToPaint(_timeToPaint));
        }

        /// <summary>
        /// Stat running coundown time to paint the ring
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        private IEnumerator CountingDownTimeToPaint(float time)
        {
            float t = 0;
            while (t < time)
            {
                t += Time.deltaTime;
                yield return null;
                while (gameState != GameState.Playing)
                {
                    yield return null;
                }
            }
            GameOver();
        }

        /// <summary>
        /// Call GameOver event
        /// </summary>
        public void GameOver()
        {
            //Fire event
            GameState = GameState.GameOver;
            gameState = GameState.GameOver;

            //Add another actions here
            IsRestart = true;
            SoundManager.Instance.PlaySound(SoundManager.Instance.gameOver);
        }

        private void Update()
        {
            if (UIManager.Instance.isGameOver)
                return;

            if (gameState == GameState.Playing)
            {
                if (Input.GetMouseButtonDown(0) && !disableTouch)
                {
                    if (EventSystem.current.currentSelectedGameObject == null)
                    {
                        SoundManager.Instance.PlaySound(SoundManager.Instance.throwBall);
                        disableTouch = true;
                        listPaintedBallControl[0].Shoot(paintedBallShootingSpeed);
                        listPaintedBallControl.RemoveAt(0);

                        if (listPaintedBallControl.Count == 0)
                            IsOutOfPaintedBall = true;
                        else
                            IsOutOfPaintedBall = false;
                    }
                }
            }
        }

        /// <summary>
        /// Pause the game
        /// </summary>
        public void PauseGame()
        {
            //Fire event
            GameState = GameState.Pause;
            gameState = GameState.Pause;

            StartCoroutine(PauseBackgroundMusic());
        }

        private IEnumerator PauseBackgroundMusic()
        {
            yield return new WaitForSeconds(0.5f);
            if (SoundManager.Instance.background != null)
                SoundManager.Instance.PauseMusic();
        }

        /// <summary>
        /// Unpause the game
        /// </summary>
        public void UnPauseGame()
        {
            //Fire event
            GameState = GameState.Playing;
            gameState = GameState.Playing;

            //Add another actions here
            StartCoroutine(ResumeBackgroundMusic());
        }

        private IEnumerator ResumeBackgroundMusic()
        {
            yield return new WaitForSeconds(0.5f);
            if (SoundManager.Instance.background != null)
                SoundManager.Instance.ResumeMusic();
        }

        /// <summary>
        /// Call PassLevel event
        /// </summary>
        public void PassLevel()
        {
            //Fire event
            GameState = GameState.PassLevel;
            gameState = GameState.PassLevel;

            IsRestart = true;
            SoundManager.Instance.PlaySound(SoundManager.Instance.passLevel);
        }

        public void LoadScene(string sceneName, float delay)
        {
            StartCoroutine(LoadingScene(sceneName, delay));
        }

        private IEnumerator LoadingScene(string sceneName, float delay)
        {
            yield return new WaitForSeconds(delay);
            SceneManager.LoadScene(sceneName);
        }

        /// <summary>
        /// Increase CurrentLevel by 1
        /// </summary>
        public void IncreaseCurrentLevel()
        {
            CurrentLevel++;
        }

        /// <summary>
        /// Decrease CurrentLevel by 1
        /// </summary>
        public void DecreaseCurrentLevel()
        {
            if (CurrentLevel > 2)
                CurrentLevel--;
        }

        /// <summary>
        /// Handle events after painted ball hit normal ring's piece (not painted piece)
        /// </summary>
        public void HandleHitNormalRingPiece()
        {
            if (listPaintedBallControl.Count > 0)
                StartCoroutine(MovingForwardAllPaintedBall());
            else
            {
                if (ringCount == ringNumber) //Win this level
                    PassLevel();
                else //Create next ring
                {
                    SoundManager.Instance.PlaySound(SoundManager.Instance.finishedRing);

                    ringCount++;
                    CurrentRingMaterial = GetRandomMaterial();
                    paintedBallMaterial.color = CurrentRingMaterial.color;

                    CreateNextRing(); //Create next ring to paint
                    CreatePaintedBall(); //Create painted balls
                    UIManager.Instance.UpdateRingCount(ringCount, ringNumber);
                    UIManager.Instance.StopCountTimebar();
                    StartCoroutine(WaitAndRunCountTimebar());
                }
            }
        }

        /// <summary>
        /// Moving all painted ball forward
        /// </summary>
        /// <returns></returns>
        private IEnumerator MovingForwardAllPaintedBall()
        {
            yield return null;
            for (int i = 0; i < listPaintedBallControl.Count; i++)
                listPaintedBallControl[i].MoveForward(paintedBallSpace, touchDelayTime);
            
            StartCoroutine(WaitAndEnableTouch(touchDelayTime));
        }

        /// <summary>
        /// Handle events after painted ball hit normal ring's piece (not painted piece)
        /// </summary>
        public void HandleHitPaintedRingPiece()
        {
            GameOver();
        }

        /// <summary>
        /// Play painted ball explode particle at given position
        /// </summary>
        /// <param name="pos"></param>
        public void PlayPaintedBallExplode(Vector3 pos)
        {
            ParticleSystem paintedBallExplode = GetPaintedBallExplode();
            paintedBallExplode.transform.position = pos;
            paintedBallExplode.gameObject.SetActive(true);
            StartCoroutine(PlayParticle(paintedBallExplode));
        }

        /// <summary>
        /// Get an inactive painted ball explode particle
        /// </summary>
        /// <returns></returns>
        private ParticleSystem GetPaintedBallExplode()
        {
            //Find in the list
            foreach (ParticleSystem o in listPaintedBallExplode)
            {
                if (!o.gameObject.activeInHierarchy)
                    return o;
            }

            //Didn't find one -> create new one
            ParticleSystem par = Instantiate(paintedBallExplodePrefab, Vector3.zero, Quaternion.identity).GetComponent<ParticleSystem>();
            listPaintedBallExplode.Add(par);
            par.gameObject.SetActive(false);
            return par;
        }

        /// <summary>
        /// Play a given particle system
        /// </summary>
        /// <param name="par"></param>
        /// <returns></returns>
        private IEnumerator PlayParticle(ParticleSystem par)
        {
            par.Play();
            yield return new WaitForSeconds(par.main.startLifetimeMultiplier);
            par.gameObject.SetActive(false);
        }

        /// <summary>
        /// Create a fading circle at given position
        /// </summary>
        /// <param name="pos"></param>
        public void CreateFadingCircle(Vector3 pos, Transform parent)
        {
            FadingObjectController fadingCircleControl = GetFadingCircleControl();
            fadingCircleControl.transform.position = pos;
            fadingCircleControl.transform.SetParent(parent);
            fadingCircleControl.gameObject.SetActive(true);
            fadingCircleControl.CircleFading(Vector3.one * fadingCircleScale, circleFadingtime);
        }

        /// <summary>
        /// Get an inactive fading circle object
        /// </summary>
        /// <returns></returns>
        private FadingObjectController GetFadingCircleControl()
        {
            //Find in the list
            foreach (FadingObjectController o in listFadingCircleControl)
            {
                if (!o.gameObject.activeInHierarchy)
                    return o;
            }

            //Didn't find one -> create new one
            FadingObjectController fadingCircle = Instantiate(fadingCirclePrefab, Vector3.zero, Quaternion.identity).GetComponent<FadingObjectController>();
            listFadingCircleControl.Add(fadingCircle);
            fadingCircle.gameObject.SetActive(false);
            return fadingCircle;
        }

        /// <summary>
        /// Create a fading ring at given position
        /// </summary>
        /// <param name="pos"></param>
        public void CreateFadingRing(Vector3 pos)
        {
            FadingObjectController fadingRingControl = GetFadingRingControl();
            fadingRingControl.transform.position = pos;
            fadingRingControl.gameObject.SetActive(true);
            fadingRingControl.RingFading(Vector3.one * fadingRingScale, ringFadingTime);
        }

        /// <summary>
        /// Get an inactive fading ring object
        /// </summary>
        /// <returns></returns>
        private FadingObjectController GetFadingRingControl()
        {
            //Find in the list
            foreach (FadingObjectController o in listFadingRingControl)
            {
                if (!o.gameObject.activeInHierarchy)
                    return o;
            }

            //Didn't find one -> create new one
            FadingObjectController fadingRing = Instantiate(fadingRingPrefab, Vector3.zero, Quaternion.identity).GetComponent<FadingObjectController>();
            listFadingRingControl.Add(fadingRing);
            fadingRing.gameObject.SetActive(false);
            return fadingRing;
        }
    }
}