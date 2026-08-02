using System;
using UnityEngine;

namespace DiaBlackJack.GameScene
{
    [DisallowMultipleComponent]
    public sealed class RevolverAnimationEventReceiver :
        PresentationAnimationEventReceiver
    {
        public event Action ShotImpact;

        public void NotifyShotImpact()
        {
            ShotImpact?.Invoke();
        }
    }
}
