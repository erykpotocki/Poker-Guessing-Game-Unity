using System;
using UnityEngine;

public class HotSeatBidController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HotSeatTurnManager turnManager;
    [SerializeField] private HotSeatHandRankPanelUI handRankPanel;

    public event Action<string> RaiseConfirmed;
    public event Action CheckConfirmed;

    private string currentBid = "";
    private bool canCheckCurrentTurn;

    public string CurrentBid => currentBid;

    private void Awake()
    {
        if (turnManager != null)
        {
            turnManager.RaiseRequested += HandleRaiseRequested;
            turnManager.CheckRequested += HandleCheckRequested;
        }

        if (handRankPanel != null)
        {
            handRankPanel.RaiseChosen += HandleRaiseChosen;
            handRankPanel.CheckChosen += HandleCheckRequested;
            handRankPanel.CancelChosen += HandleCancelChosen;
            handRankPanel.Close();
        }
    }

    public void BeginNewRound()
    {
        currentBid = "";
        BeginTurn(false);
    }

    public void BeginTurn(bool canCheck)
    {
        canCheckCurrentTurn = canCheck;

        if (turnManager != null)
        {
            turnManager.BeginTurn(
                canCheckCurrentTurn,
                currentBid
            );
        }
    }

    private void HandleRaiseRequested()
    {
        if (handRankPanel == null)
            return;

        handRankPanel.Open(
            canCheckCurrentTurn,
            currentBid
        );
    }

    private void HandleRaiseChosen(string chosenRank)
    {
        if (string.IsNullOrWhiteSpace(chosenRank))
            return;

        currentBid = chosenRank;
        RaiseConfirmed?.Invoke(currentBid);
    }

    private void HandleCheckRequested()
    {
        if (!canCheckCurrentTurn)
            return;

        if (handRankPanel != null)
            handRankPanel.Close();

        CheckConfirmed?.Invoke();
    }

    private void HandleCancelChosen()
    {
        if (turnManager == null)
            return;

        turnManager.BeginTurn(
            canCheckCurrentTurn,
            currentBid
        );
    }

    private void OnDestroy()
    {
        if (turnManager != null)
        {
            turnManager.RaiseRequested -= HandleRaiseRequested;
            turnManager.CheckRequested -= HandleCheckRequested;
        }

        if (handRankPanel != null)
        {
            handRankPanel.RaiseChosen -= HandleRaiseChosen;
            handRankPanel.CheckChosen -= HandleCheckRequested;
            handRankPanel.CancelChosen -= HandleCancelChosen;
        }
    }
}
