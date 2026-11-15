using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace ResortEmpire.Fx
{
    public class FxAutoKill : MonoBehaviour
    {
        public float life = 2f;
        void Update()
        {
            life -= Time.unscaledDeltaTime;
            if (life <= 0f) Destroy(gameObject);
        }
    }
}