﻿using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using rummy.Cards;
using rummy.UI;
using rummy.Utility;
using TMPro;
using System.Collections;
using UnityEngine.Networking;

namespace rummy
{
    public class GameMaster : MonoBehaviour
    {
        [SerializeField] private RammyApi api;
        [SerializeField] private BotData[] characters;
        public int PlayerCount = 4; // Set PlayerCount to 4
        public Transform PlayersParent;
        [SerializeField]
        private bool HumanPlayer = true;
        public void EnableHumanPlayer(bool enable) => HumanPlayer = enable;
        private bool showOpponentCards = false;
        public void ShowOpponentCards(bool show)
        {
            Players.Skip(1).ToList().ForEach(p => p.SetCardsVisible(show));
            showOpponentCards = show;
        }
        public GameObject HumanPlayerPrefab;
        public GameObject BotPlayerPrefab;
        public void ChangePlayerCount(bool increase)
        {
            // Ensure PlayerCount stays at 4
            if (increase)
                PlayerCount = 4;
            else
                PlayerCount = 4;
        }

        private static readonly int PLAYER_X = 15;
        private static readonly int PLAYER_Y = 14;
        private static readonly Vector3 LD = new(-PLAYER_X, -PLAYER_Y, 0);
        private static readonly Vector3 LU = new(-PLAYER_X, PLAYER_Y, 0);
        private static readonly Vector3 CD = new(0, -PLAYER_Y, 0);
        private static readonly Vector3 CU = new(0, PLAYER_Y, 0);
        private static readonly Vector3 RD = new(PLAYER_X, -PLAYER_Y, 0);
        private static readonly Vector3 RU = new(PLAYER_X, PLAYER_Y, 0);

        private readonly IDictionary<int, List<Vector3>> PlayerPos = new Dictionary<int, List<Vector3>>()
        {
            {4, new List<Vector3> { CD, LU, CU, RD } }
        };

        [SerializeField]
        private int CardsPerPlayer = 13;
        [SerializeField]
        private int EarliestLaydownRound = 2;
        public int MinimumLaySum { get; private set; } = 40;
        public void SetMinimumLaySum(int value) => MinimumLaySum = value;

        [SerializeField]
        private bool RandomizeSeed = true;
        public int Seed;
        public void SetSeed(int value) => Seed = value;
        [SerializeField]
        private int startingPlayer = 0;

        private float DefaultGameSpeed; // Stored during pause
        public float GameSpeed { get; private set; } = 1.0f;
        public void SetGameSpeed(float value) => GameSpeed = value;

        public bool AnimateCardMovement { get; private set; } = true;
        public void SetAnimateCardMovement(bool value) => AnimateCardMovement = value;

        private float drawWaitStartTime;
        public float DrawWaitDuration { get; private set; } = 0.5f;
        public void SetDrawWaitDuration(float value) => DrawWaitDuration = value;

        public float PlayWaitDuration { get; private set; } = 1f;
        public void SetPlayWaitDuration(float value) => PlayWaitDuration = value;

        public float CurrentCardMoveSpeed => gameState == GameState.DEALING ? DealCardMoveSpeed : PlayCardMoveSpeed;
        public float PlayCardMoveSpeed { get; private set; } = 30f;
        public float DealCardMoveSpeed { get; private set; } = 200f;
        public void SetPlayCardMoveSpeed(float value) => PlayCardMoveSpeed = value;
        public void SetDealCardMoveSpeed(float value) => DealCardMoveSpeed = value;

        public int RoundCount { get; private set; }
        public bool LayingAllowed() => RoundCount >= EarliestLaydownRound;

        public readonly List<Player> Players = new();
        private Player CurrentPlayer => Players[currentPlayerID];

        private bool isCardBeingDealt;
        private int currentPlayerID;
        private int currentStartingPlayerID;

        [SerializeField]
        private int SkipUntilRound = 0;
        private bool skippingDone;
        private float storedPlayerWaitDur, storedDrawWaitDur, storedGameSpeed;

        // === TIMER VARIABLES ===
        [SerializeField] private TMP_Text timerText; // TMP text to display the timer
        private float turnTimeLimit = 25f; // 25-second limit
        private float turnTimer; // Timer countdown
        private bool isTurnActive = false;

        private enum GameState
        {
            NONE = 0,
            DEALING = 1,
            PLAYING = 2,
            DRAWWAIT = 3
        }
        private GameState gameState = GameState.NONE;

        public class Event_GameOver : UnityEvent<Player> { }
        public Event_GameOver GameOver = new();
        public Event_GameOver TimeOut = new();

        [SerializeField]
        private CardStack CardStack;
        [SerializeField]
        private DiscardStack DiscardStack;
        [SerializeField]
        private CardStack.CardStackType CardStackType = CardStack.CardStackType.DEFAULT;

        private Scoreboard Scoreboard;

        private void Awake()
        {
            StartCoroutine(api.StartGameRammy());
            StartCoroutine(api.GetProfile());
        }
        private void Start()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;

            if (RandomizeSeed)
                Seed = Random.Range(0, int.MaxValue);

            Scoreboard = GetComponentInChildren<Scoreboard>(true);
            StartGame(true);
        }

        private void StartGame(bool newGame)
        {
            StartCoroutine(api.GetProfile());
            Random.InitState(Seed);
            CardStack.CreateCardStack(CardStackType);

            if (SkipUntilRound > 0)
            {
                storedGameSpeed = GameSpeed;
                storedPlayerWaitDur = PlayWaitDuration;
                storedDrawWaitDur = DrawWaitDuration;

                skippingDone = false;
                AnimateCardMovement = false;
                PlayWaitDuration = 0f;
                DrawWaitDuration = 0f;
                GameSpeed = 10;
            }

            Time.timeScale = GameSpeed;
            RoundCount = 0;

            if (newGame)
            {
                Players.ClearAndDestroy();
                currentStartingPlayerID = startingPlayer - 1;
                CreatePlayers();
            }

            gameState = GameState.DEALING;
            if (currentStartingPlayerID >= 0)
                Players[currentStartingPlayerID].IsStarting.Invoke(false);
            currentStartingPlayerID = (currentStartingPlayerID + 1) % Players.Count;
            currentPlayerID = currentStartingPlayerID;
            CurrentPlayer.IsStarting.Invoke(true);

            
        }

        private void CreatePlayers()
        {

            var humanPlayer = Instantiate(HumanPlayerPrefab, PlayersParent).GetComponent<Player>();
            Players.Add(humanPlayer);
            
            humanPlayer.SetPlayerName($"{api.CurrentUserName}");
            //GetComponent<GUIScaler>().AddCanvasScaler(humanPlayer.transform.Find("OutputCanvas").GetComponent<CanvasScaler>());
            //GetComponent<GUIScaler>().AddCanvasScaler(humanPlayer.transform.Find("ShowButtonCanvas").GetComponent<CanvasScaler>());
            Transform avatarTransf = humanPlayer.transform.Find("Avatar");
            if (avatarTransf != null)
            {
                SpriteRenderer spriteRenderer = avatarTransf.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null)
                {
                    
                }
            }

            // Create 4 Human Players
            for (int i = 0; i < 3; i++)
            {
                var botPlayer = Instantiate(BotPlayerPrefab, PlayersParent).GetComponent<Player>();
                Players.Add(botPlayer);
                //botPlayer.SetPlayerName($"Player {i + 1}");
                //GetComponent<GUIScaler>().AddCanvasScaler(humanPlayer.transform.Find("OutputCanvas").GetComponent<CanvasScaler>());
                //GetComponent<GUIScaler>().AddCanvasScaler(humanPlayer.transform.Find("ShowButtonCanvas").GetComponent<CanvasScaler>());

                int randomIndex = Random.Range(0, characters.Length);
                BotData selectedCharacter = characters[randomIndex];

                botPlayer.SetPlayerName(selectedCharacter.name);

                Transform avatarTransform = botPlayer.transform.Find("Avatar"); // Make sure your child is named "Avatar"
                if (avatarTransform != null)
                {
                    SpriteRenderer spriteRenderer = avatarTransform.GetComponent<SpriteRenderer>();
                    if (spriteRenderer != null)
                    {
                        spriteRenderer.sprite = selectedCharacter.sprite;
                    }
                }

            }

            // Update player positions for the current player count
            List<Vector3> pos = PlayerPos[Players.Count];
            for (int i = 0; i < Players.Count; i++)
            {
                var p = Players[i];
                p.transform.position = pos[i];
                if (pos[i].y > 0) // Rotate the players in the top row
                    p.Rotate();
            }

            Scoreboard.Clear();
            Scoreboard.AddLine(Players, true);

            ShowOpponentCards(showOpponentCards);
        }


        public void NextGame(bool newGame)
        {
            if (RandomizeSeed)
            {
                int prevSeed = Seed;
                do
                {
                    Seed = Random.Range(0, int.MaxValue);
                } while (Seed == prevSeed);
            }
            else
                Seed += 1;

            RestartGame(newGame);
            
        }

        public void RestartGame(bool newGame)
        {
            gameState = GameState.NONE;
            if (SkipUntilRound > 0)
            {
                GameSpeed = storedGameSpeed;
                PlayWaitDuration = storedPlayerWaitDur;
                DrawWaitDuration = storedDrawWaitDur;
            }

            DiscardStack.RemoveCards();
            foreach (var p in Players)
                p.ResetPlayer();
            FindObjectsOfType<Card>().ToList().ClearAndDestroy();

            StartGame(newGame);
        }

        

        private void Update()
        {
            Time.timeScale = GameSpeed;

            /*foreach (var player in Players)
            {
                // Ensure only non-IDLE players can interact with their cards
                player.SetCardsVisible(player.State != Player.PlayerState.IDLE);
            }*/


            Time.timeScale = GameSpeed;

            // Update turn timer if active
            if (isTurnActive)
            {
                UpdateTurnTimer();
            }


            if (gameState == GameState.DEALING)
            {
                if (!isCardBeingDealt)
                {
                    isCardBeingDealt = true;
                    CurrentPlayer.DrawCard(true);
                    
                    StartTurnTimer();
                }
                else if (CurrentPlayer.State == Player.PlayerState.IDLE)
                {
                    isCardBeingDealt = false;
                    currentPlayerID = (currentPlayerID + 1) % Players.Count;
                    if (currentPlayerID == currentStartingPlayerID &&
                        CurrentPlayer.HandCardCount == CardsPerPlayer)
                    {
                        drawWaitStartTime = Time.time;
                        gameState = GameState.DRAWWAIT;
                        RoundCount = 1;
                        TryStopSkipping();
                    }
                }
            }
            else if (gameState == GameState.DRAWWAIT)
            {
                if (Time.time - drawWaitStartTime > DrawWaitDuration)
                    gameState = GameState.PLAYING;
            }
            else if (gameState == GameState.PLAYING)
            {
                if (CurrentPlayer.State == Player.PlayerState.IDLE)
                {
                    CurrentPlayer.TurnFinished.AddListener(PlayerFinished);
                    CurrentPlayer.BeginTurn();
                    StartTurnTimer();
                }
            }
        }

        private void StartTurnTimer()
        {
            turnTimer = turnTimeLimit;
            isTurnActive = true;
            UpdateTimerUI();
        }

        private void UpdateTurnTimer()
        {
            if (turnTimer > 0)
            {
                turnTimer -= Time.deltaTime;
                UpdateTimerUI();
            }
            else
            {
                isTurnActive = false;
                StartCoroutine(api.EndGameRammy(Players[0].PlayerName));
                TimeOut.Invoke(CurrentPlayer);
                gameState = GameState.NONE;
                return;
            }
        }

        private void UpdateTimerUI()
        {
            if (timerText != null)
            {
                timerText.text = $"Time: {Mathf.Ceil(turnTimer)}s";
            }
        }

        private void PlayerFinished()
        {
            CurrentPlayer.TurnFinished.RemoveAllListeners();
            if (CurrentPlayer.HandCardCount == 0)
            {
                Scoreboard.AddLine(Players, false);
                StartCoroutine(api.EndGameRammy(CurrentPlayer.PlayerName));
                GameOver.Invoke(CurrentPlayer);
                gameState = GameState.NONE;
                return;
            }

            currentPlayerID = (currentPlayerID + 1) % Players.Count;
            if (currentPlayerID == 0)
            {
                RoundCount++;
                TryStopSkipping();

                if (IsGameADraw())
                {
                    Scoreboard.AddLine(Players, false);
                    GameOver.Invoke(null);
                    gameState = GameState.NONE;
                    return;
                }
            }

            if (CardStack.CardCount == 0)
            {
                var discardedCards = DiscardStack.RecycleDiscardedCards();
                CardStack.Restock(discardedCards);
            }

            drawWaitStartTime = Time.time;
            gameState = GameState.DRAWWAIT;
        }

        private void TryStopSkipping()
        {
            if (!skippingDone && RoundCount == SkipUntilRound)
            {
                skippingDone = true;
                AnimateCardMovement = true;
                GameSpeed = storedGameSpeed;
                PlayWaitDuration = storedPlayerWaitDur;
                DrawWaitDuration = storedDrawWaitDur;
            }
        }

        private bool IsGameADraw()
        {
           
            if (Players.Any(p => p.HandCardCount >= 3))
                return false;

            foreach (var p in Players)
            {
                var cardSpots = p.GetPlayerCardSpots();
                if (cardSpots.Any(spot => !spot.IsFull(true)))
                    return false;
            }
            return true;
        }

        public void TogglePause()
        {
            if (GameSpeed > 0)
            {
                DefaultGameSpeed = GameSpeed;
                GameSpeed = 0;
            }
            else
            {
                GameSpeed = DefaultGameSpeed;
            }
        }

        public List<CardSpot> GetAllCardSpots()
        {
            var cardSpots = new List<CardSpot>();
            foreach (var player in Players)
                cardSpots.AddRange(player.GetPlayerCardSpots());
            return cardSpots;
        }

        public List<Card> GetAllCardSpotCards()
        {
            var cards = new List<Card>();
            var cardSpots = GetAllCardSpots();
            foreach (var spot in cardSpots)
                cards.AddRange(spot.Objects);
            return cards;
        }

        public void Log(string message, LogType type)
        {
            string prefix = "[Seed " + Seed + ", Round " + RoundCount + "] ";
            switch (type)
            {
                case LogType.Error:
                    Debug.LogError(prefix + message);
                    break;
                case LogType.Warning:
                    Debug.LogWarning(prefix + message);
                    break;
                default:
                    Debug.Log(prefix + message);
                    break;
            }
        }

        public enum LogType
        {
            Default,
            Error,
            Warning
        }
    }
}