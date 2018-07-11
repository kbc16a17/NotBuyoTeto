﻿using System;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using NotTetrin.UI;
using NotTetrin.Constants;
using NotTetrin.SceneManagement;
using NotTetrin.Ingame.Marathon;

namespace NotTetrin.Ingame.MultiPlay.Marathon {
    [RequireComponent(typeof(PhotonView))]
    public class GameManager : SceneBase {
        [SerializeField] NetworkDirector director;
        [SerializeField] BGMManager bgmManager;
        [SerializeField] GroupManager groupManager;
        [SerializeField] IngameSfxManager sfxManager;
        [SerializeField] MinoManager minoManager;
        [SerializeField] GarbageMinoManager garbageMinoManager;

        [SerializeField] MessageWindow messageWindow;

        private PhotonView photonView;
        private double gameOverTime = 0.0;
        private bool startedGame = false;
        private bool acceptedResult = false;
        private bool quit = false;

        protected override void Awake() {
            base.Awake();

            photonView = GetComponent<PhotonView>();
            groupManager.MinoDeleted += onMinoDeleted;

            director.PlayerNameLabel.text = IdentificationNameUtility.ParseName(PhotonNetwork.player.NickName);
            director.OpponentNameLabel.text = IdentificationNameUtility.ParseName(PhotonNetwork.otherPlayers[0].NickName);
            director.PlayerYouLabel.enabled = true;
        }

        private void Start() {
            PhotonNetwork.sendRate = 10;
            PhotonNetwork.sendRateOnSerialize = 10;
            StartCoroutine(updateAndSendPing());
        }

        protected override void OnSceneReady(object sender, EventArgs args) {
            base.OnSceneReady(sender, args);
            minoManager.HitMino += onHitMino;
            ready();
        }

        private void Update() {
            if (Input.GetButtonDown(@"Escape")) {
                ExitGame();
            }
        }

        public void ExitGame() {
            quit = true;
            StopAllCoroutines();
            if (PhotonNetwork.connected) { PhotonNetwork.Disconnect(); }
            SceneController.Instance.LoadScene(SceneName.Title, 0.7f);
        }

        private IEnumerator updateAndSendPing() {
            while (true) {
                var ping = PhotonNetwork.GetPing();
                director.PlayerPingLabel.text = $"Ping: { ping }ms";
                photonView.RPC(@"OnUpdateOpponentPing", PhotonTargets.Others, ping);
                yield return new WaitForSeconds(2.0f);
            }
        }

        private void reset() {
            startedGame = false;
            acceptedResult = false;
            minoManager.Reset();
            garbageMinoManager.Clear();
        }

        private void ready() {
            reset();
            director.Floor.SetActive(true);
            photonView.RPC(@"OnReadyOpponent", PhotonTargets.Others);
        }

        private void gamestart() {
            startedGame = true;
            photonView.RPC(@"OnGamestartOpponent", PhotonTargets.Others);

            sfxManager.Stop(IngameSfxType.GameOver);
            sfxManager.Play(IngameSfxType.GameStart);
            bgmManager.RandomPlay();
            minoManager.Next();
        }

        private void gameover() {
            gameOverTime = PhotonNetwork.time;
            photonView.RPC(@"OnGameoverOpponent", PhotonTargets.Others, gameOverTime);
        }

        private void next() {
            minoManager.Next();
        }

        private void onMinoDeleted(object sender, DeleteMinoInfo info) {
            photonView.RPC(@"OnDeleteMinoOpponent", PhotonTargets.Others, info.LineCount, info.ObjectCount);
        }

        private void onHitMino(object sender, EventArgs args) {
            minoManager.Release();

            // 天井に当たったらゲームオーバー
            if (director.Ceiling.IsHit) {
                gameover();
            } else {
                groupManager.DeleteMino();
                StartCoroutine(fallGarbageAndNext());
            }
        }

        private IEnumerator fallGarbageAndNext() {
            if (garbageMinoManager.Fall()) {
                yield return new WaitWhile(() => garbageMinoManager.IsFalling);
            }
            next();
        }

        private void OnDisconnectedFromPhoton() {
            if (quit) { return; }
            messageWindow.Show("通信が切断されました。");
            Invoke(@"ExitGame", 3.0f);
        }

        private void OnPhotonPlayerDisconnected(PhotonPlayer player) {
            messageWindow.Show("対戦相手が切断されました。");
            Invoke(@"ExitGame", 3.0f);
        }

        [PunRPC]
        private void OnReadyOpponent() {
            if (startedGame) { return; }
            gamestart();
        }

        [PunRPC]
        private void OnGamestartOpponent() {
            if (startedGame) { return; }
            gamestart();
        }

        [PunRPC]
        private void OnGameoverOpponent(double timestamp) {
            Debug.Log(@"OnGameoverOpponent (" + timestamp + ")");
            minoManager.Destroy();

            if (timestamp < gameOverTime) {
                photonView.RPC(@"OnWinAccepted", PhotonTargets.Others);
                OnLoseAccepted();
            } else {
                photonView.RPC(@"OnLoseAccepted", PhotonTargets.Others);
                OnWinAccepted();
            }
        }

        [PunRPC]
        private void OnWinAccepted() {
            if (acceptedResult) { return; }
            Debug.Log($"you win.");
            director.PlayerWinsCounter.Increment();

            bgmManager.Stop();
            sfxManager.Play(IngameSfxType.GameOver);
            Invoke("ready", 9.0f);

            acceptedResult = true;
        }

        [PunRPC]
        private void OnLoseAccepted() {
            if (acceptedResult) { return; }
            Debug.Log($"you lose.");
            director.OpponentWinsCounter.Increment();

            bgmManager.Stop();
            director.Floor.SetActive(false);
            sfxManager.Play(IngameSfxType.GameOver);
            Invoke("ready", 9.0f);

            acceptedResult = true;
        }

        [PunRPC]
        private void OnDeleteMinoOpponent(int lineCount, int objectCount) {
            var info = new DeleteMinoInfo(lineCount, objectCount);
            garbageMinoManager.Add(info);
        }

        [PunRPC]
        private void OnUpdateOpponentPing(int ping) {
            director.OpponentPingLabel.text = $"Ping: { ping }ms";
        }
    }
}
