using UnityEngine;

namespace PartTips
{
    class TooltipState : MonoBehaviour
    {
        public static bool open = false;

        protected void Start()
        {
            //open = false;
        }

        protected void OnEnable()
        {
            open = true;
        }

        protected void OnDisable()
        {
            open = false;
            PartTips.Instance.Close();
        }
    }
}
