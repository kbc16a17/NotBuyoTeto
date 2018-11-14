﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NotBuyoTeto.UI;
using NotBuyoTeto.Constants;
using NotBuyoTeto.SceneManagement;

namespace NotBuyoTeto.Ingame.Title {
    public class NetworkBattleButton : MonoBehaviour {
        [SerializeField]
        private SelectableToggler toggler;

        public void OnPressed() {
            toggler.ToggleAll();
            SceneController.Instance.LoadScene(SceneName.Matching, SceneTransition.Duration);
        }
    }
}