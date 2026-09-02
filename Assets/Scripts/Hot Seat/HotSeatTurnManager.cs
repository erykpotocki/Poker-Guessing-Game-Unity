using System;
using UnityEngine;

public class HotSeatTurnManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HotSeatTurnUI turnUI;

    [Header("Timer")]
    [SerializeField] private float turnDurationSeconds = 60f;

    public event Action RaiseRequested;
    public event Action CheckRequested;

    private float timeRemaining;
    private bool turnActive;
    private bool checkAvailable;
    private bool checkAlreadyTriggered;

    private void Awake()
    {
        if (turnUI != null)
        {
            turnUI.SetActions(
                HandleRaiseClicked,
                HandleCheckClicked
            );

            turnUI.Hide();
        }
    }

    private void Update()
    {
        if (!turnActive)
            return;

        timeRemaining -= Time.deltaTime;

        if (turnUI != null)
            turnUI.SetTimer(timeRemaining);

        if (checkAvailable &&
            timeRemaining <= 0f &&
            !checkAlreadyTriggered)
        {
            checkAlreadyTriggered = true;
            HandleCheckClicked();
        }
    }

    public void BeginTurn(
        bool canCheck,
        string currentBidName)
    {
        checkAvailable = canCheck;
        checkAlreadyTriggered = false;
        timeRemaining = turnDurationSeconds;
        turnActive = true;

        if (turnUI == null)
            return;

        turnUI.Show();
        turnUI.SetTimer(timeRemaining);
        turnUI.SetCurrentBid(currentBidName);
        turnUI.SetCheckAvailable(canCheck);
        turnUI.SetButtonsInteractable(true);
    }

    public void StopTurn()
    {
        turnActive = false;

        if (turnUI != null)
        {
            turnUI.SetButtonsInteractable(false);
            turnUI.Hide();
        }
    }

    private void HandleRaiseClicked()
    {
        if (!turnActive)
            return;

        StopTurn();
        RaiseRequested?.Invoke();
    }

    private void HandleCheckClicked()
    {
        if (!turnActive || !checkAvailable)
            return;

        StopTurn();
        CheckRequested?.Invoke();
    }
}