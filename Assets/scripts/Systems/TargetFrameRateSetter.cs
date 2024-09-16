using UnityEngine;

namespace Systems
{
    public class TargetFrameRateSetter : MonoBehaviour
    {
        [SerializeField] private int targetFrameRate;
        void Start()
        {
            Application.targetFrameRate = targetFrameRate;
        }
    }
}
