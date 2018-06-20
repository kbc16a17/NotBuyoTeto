﻿using UnityEngine;

namespace NotTetrin.Ingame {
    public abstract class Director : MonoBehaviour {
        public abstract Ceiling Ceiling { get; }
        public abstract HoldMino HoldMino { get; }
        public abstract NextMino NextMino { get; }
    }
}
