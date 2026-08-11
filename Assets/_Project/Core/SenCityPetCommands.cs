using System;
using UnityEngine;

namespace SenCity.Core.Pet
{
    public interface ISenCityPetCommands
    {
        event Action<string> StateChanged;

        string CurrentStateLabel { get; }

        void GoToTarget(Transform target, string displayName);
        void Feed();
        void Play();
        void Sleep();
    }
}
