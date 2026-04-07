using UnityEngine;

namespace MimicFacility.Horror
{
    public class SubliminalFrameSystem : MonoBehaviour
    {
        [SerializeField] private float messageAlpha = 0.4f;
        [SerializeField] private int fontSize = 14;

        private static readonly string[] SubliminalMessages =
        {
            "YOU ARE BEING OBSERVED",
            "SESSION DATA LOGGED",
            "SUBJECT PROFILE COMPLETE",
            "TRUST INDEX UPDATED",
            "BEHAVIORAL PATTERN RECORDED"
        };

        private bool _hasPendingFrame;
        private string _pendingMessage;

        public float MessageAlpha => messageAlpha;
        public int FontSize => fontSize;

        public void RenderSubliminalFrame()
        {
            _pendingMessage = SubliminalMessages[Random.Range(0, SubliminalMessages.Length)];
            _hasPendingFrame = true;
        }

        public (bool hasFrame, string message) ConsumeFrame()
        {
            if (!_hasPendingFrame)
                return (false, null);

            string msg = _pendingMessage;
            _hasPendingFrame = false;
            _pendingMessage = null;
            return (true, msg);
        }

        public void TriggerRandom()
        {
            RenderSubliminalFrame();
        }

        public void TriggerSpecific(string message)
        {
            _pendingMessage = message;
            _hasPendingFrame = true;
        }

        public bool HasPending()
        {
            return _hasPendingFrame;
        }

        public string PeekMessage()
        {
            return _pendingMessage;
        }
    }
}
