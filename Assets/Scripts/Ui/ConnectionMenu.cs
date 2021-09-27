using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.WSA;
using TMPro;

namespace com.jon_skoberne.UI
{

    public class ConnectionMenu : MonoBehaviour
    {
        public TextMeshProUGUI textIp;
        public GameObject[] status;
        Coroutine statusChecker;

        // Start is called before the first frame update
        void Start()
        {
            DisplayState();
        }

        private void OnDestroy()
        {
            if (statusChecker != null) StopCoroutine(statusChecker);
        }

        public void ConnectHololens()
        {

            HolographicRemoting.Connect(textIp.text);
        }

        public void DisconnectHololens()
        {
            HolographicRemoting.Disconnect();
        }

        private void DisplayState()
        {
            if(statusChecker != null)
            {
                StopCoroutine(statusChecker);
            }
            statusChecker = StartCoroutine("StatusCheck");
        }

        private void EnableStatus(int statusInd)
        {
            for(int i = 0; i < status.Length; i++)
            {
                status[i].SetActive(i == statusInd);
            }
        }

        IEnumerator StatusCheck()
        {
            while(true)
            {
                var state = HolographicRemoting.ConnectionState;
                Debug.Log("Checking HoloLens connection state: " + state);
                var indState = GetState(state);
                EnableStatus(indState);
                yield return new WaitForSeconds(1);
            }
        }

        private int GetState(HolographicStreamerConnectionState state)
        {
            switch(state)
            {
                case HolographicStreamerConnectionState.Disconnected: return 0;
                case HolographicStreamerConnectionState.Connecting: return 1;
                case HolographicStreamerConnectionState.Connected: return 2;
                default: return 0;
            }
        }
    }
}
