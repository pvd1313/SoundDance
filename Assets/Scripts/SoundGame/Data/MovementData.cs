using System;
using UnityEngine;

namespace Pingle.SoundGame
{
    [Serializable]
    public class MovementData
    {
        public EMovementType MovementType => _movementType;
        public Sprite Icon => _icon;
        public KeyCode Key => _key;

        [SerializeField] private EMovementType _movementType;
        [SerializeField] private Sprite _icon;
        [SerializeField] private KeyCode _key;
    }
}
