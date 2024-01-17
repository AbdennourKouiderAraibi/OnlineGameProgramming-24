using System;
using Unity.Netcode;
using Unity.Services.Matchmaker.Models;
using UnityEngine;
using static Unity.Services.Matchmaker.Models.MultiplayAssignment;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    internal class MatchmakerController : Controller<MetagameApplication>
    {
        MatchmakerView View => App.View.Matchmaker;

        void Awake()
        {
            AddListener<EnterMatchmakerQueueEvent>(OnEnterMatchmakerQueue);
            AddListener<ExitMatchmakerQueueEvent>(OnExitMatchmakerQueue);
            AddListener<MatchLoadingEvent>(OnMatchLoading);
            AddListener<ExitMatchLoadingEvent>(OnExitMatchLoading);
        }

        void OnDestroy()
        {
            RemoveListeners();
        }

        void OnApplicationQuit()
        {
            StopMatchmaker();
        }

        internal override void RemoveListeners()
        {
            RemoveListener<EnterMatchmakerQueueEvent>(OnEnterMatchmakerQueue);
            RemoveListener<ExitMatchmakerQueueEvent>(OnExitMatchmakerQueue);
            RemoveListener<MatchLoadingEvent>(OnMatchLoading);
            RemoveListener<ExitMatchLoadingEvent>(OnExitMatchLoading);
        }

        void OnEnterMatchmakerQueue(EnterMatchmakerQueueEvent evt)
        {
            View.Show();
            CustomNetworkManager.Singleton.OnEnteredMatchmaker();
            UnityServicesInitializer.Instance.Matchmaker.FindMatch(evt.QueueName, OnMatchSearchCompleted, View.UpdateTimer);
        }

        void OnExitMatchmakerQueue(ExitMatchmakerQueueEvent evt)
        {
            StopMatchmaker();
            View.Hide();
        }

        void OnMatchSearchCompleted(MultiplayAssignment assignment)
        {
            var error = string.Empty;
            if (assignment == null)
            {
                error = "The matchmaker request is invalid, please try again to find a match.";
            }
            else
            {
                switch (assignment.Status)
                {
                    case StatusOptions.Found:
                        Debug.Log("Match found!");
                        CustomNetworkManager.s_AssignmentForCurrentGame = assignment;
                        CustomNetworkManager.Singleton.InitializeNetworkLogic(false, true);
                        break;
                    case StatusOptions.Failed:
                        error = $"Failed to get ticket status. See logged exception for more details: {assignment.Message}";
                        break;
                    case StatusOptions.Timeout:
                        //note: this is a good spot where to plug-in a fake pvp matchmaking logic that redirects the player to a PvE game
                        error = "Could not find enough players in a reasonable amount of times";
                        break;
                    case StatusOptions.InProgress:
                        error = "An error occured during matchmaking, please try again to find a match.";
                        break;
                    default:
                        throw new InvalidOperationException("Assignment status was a value other than 'In Progress', 'Found', 'Timeout' or 'Failed'! " +
                            $"Mismatch between Matchmaker SDK expected responses and service API values! Status value: '{assignment.Status}'");
                }
            }

            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogError(error);
                Broadcast(new ExitMatchmakerQueueEvent());
            }
        }

        void StopMatchmaker()
        {
            if (UnityServicesInitializer.Instance.Matchmaker)
            {
#pragma warning disable CS4014 // Can't await, so the method execution will continue
                UnityServicesInitializer.Instance.Matchmaker.StopSearch();
#pragma warning restore CS4014 // Can't await, so the method execution will continue
            }
        }

        void OnMatchLoading(MatchLoadingEvent evt)
        {
            if (CustomNetworkManager.Singleton.AutoConnectOnStartup)
            {
                return; //we're starting a match from the main menu
            }
            View.Hide();
            App.View.LoadingScreen.Show();
        }

        void OnExitMatchLoading(ExitMatchLoadingEvent evt)
        {
            App.View.LoadingScreen.Hide();
            View.Show();
            NetworkManager.Singleton.Shutdown();
            Broadcast(new EnterMatchmakerQueueEvent(UnityServicesInitializer.Instance.Matchmaker.LastQueueName));
        }
    }
}
