using Unity.Netcode;
using UnityEngine;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// Holds the logical state of a game and synchronizes it across the network
    /// </summary>
    internal class MatchDataSynchronizer : NetworkBehaviour
    {
        internal NetworkVariable<uint> MatchCountdown = new NetworkVariable<uint>();
        internal NetworkVariable<bool> MatchEnded = new NetworkVariable<bool>();
        internal NetworkVariable<bool> MatchStarted = new NetworkVariable<bool>();

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsClient)
            {
                MatchCountdown.OnValueChanged += OnClientMatchCountdownChanged;
                MatchEnded.OnValueChanged += OnClientMatchEndedChanged;
                MatchStarted.OnValueChanged += OnClientMatchStartedChanged;
            }
        }

        void OnClientMatchCountdownChanged(uint previousValue, uint newValue)
        {
            GameApplication.Instance.Broadcast(new CountdownChangedEvent(newValue));
        }

        void OnClientMatchEndedChanged(bool previousValue, bool newValue)
        {
            //you can block inputs here, play animations and so on
            Debug.Log($"New match ended value: {newValue}");
        }

        void OnClientMatchStartedChanged(bool previousValue, bool newValue)
        {
            //you can enable inputs here, play animations and so on
            Debug.Log($"New match started value: {newValue}");
        }

        [ClientRpc]
        internal void OnClientMatchResultComputedClientRpc(ulong winnerClientId)
        {
            GameApplication.Instance.Broadcast(new MatchResultComputedEvent(winnerClientId));
        }
    }
}
