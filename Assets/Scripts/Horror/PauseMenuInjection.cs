using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MimicFacility.Horror
{
    public struct InjectedPhrase
    {
        public string Text;
        public Vector2 Position;
        public float Alpha;
        public int FontSize;
    }

    public class PauseMenuInjection : MonoBehaviour
    {
        [SerializeField] private int maxPhraseHistory = 20;
        [SerializeField] private int maxInjections = 3;
        [SerializeField] private float injectionAlpha = 0.6f;
        [SerializeField] private int injectionFontSize = 10;

        private readonly Queue<string> _recentPlayerPhrases = new Queue<string>();
        private List<InjectedPhrase> _currentInjections = new List<InjectedPhrase>();
        private bool _hasActiveInjection;

        public void FeedPlayerPhrase(string phrase)
        {
            if (string.IsNullOrEmpty(phrase)) return;

            _recentPlayerPhrases.Enqueue(phrase);
            while (_recentPlayerPhrases.Count > maxPhraseHistory)
                _recentPlayerPhrases.Dequeue();
        }

        public List<string> InjectIntoPauseMenu()
        {
            if (_recentPlayerPhrases.Count == 0)
                return new List<string>();

            var phrases = _recentPlayerPhrases.ToArray();
            var selected = new List<string>();
            int count = Mathf.Min(maxInjections, phrases.Length);

            var indices = new HashSet<int>();
            while (indices.Count < count)
                indices.Add(Random.Range(0, phrases.Length));

            foreach (int idx in indices)
                selected.Add(phrases[idx]);

            return selected;
        }

        public List<InjectedPhrase> ConsumePhrases()
        {
            if (!_hasActiveInjection)
                return new List<InjectedPhrase>();

            var result = new List<InjectedPhrase>(_currentInjections);
            _currentInjections.Clear();
            _hasActiveInjection = false;
            return result;
        }

        public void OnPauseMenuOpened()
        {
            var phrases = InjectIntoPauseMenu();
            if (phrases.Count == 0) return;

            var positions = GenerateInjectionPositions(phrases.Count);
            _currentInjections.Clear();

            for (int i = 0; i < phrases.Count; i++)
            {
                _currentInjections.Add(new InjectedPhrase
                {
                    Text = phrases[i],
                    Position = positions[i],
                    Alpha = injectionAlpha,
                    FontSize = injectionFontSize
                });
            }

            _hasActiveInjection = true;
        }

        public void ClearPhrases()
        {
            _recentPlayerPhrases.Clear();
            _currentInjections.Clear();
            _hasActiveInjection = false;
        }

        private List<Vector2> GenerateInjectionPositions(int count)
        {
            var positions = new List<Vector2>();
            float screenW = Screen.width;
            float screenH = Screen.height;
            float margin = 40f;

            for (int i = 0; i < count; i++)
            {
                int edge = Random.Range(0, 4);
                Vector2 pos;

                switch (edge)
                {
                    case 0: // top
                        pos = new Vector2(Random.Range(margin, screenW - margin), screenH - Random.Range(margin, margin * 2));
                        break;
                    case 1: // bottom
                        pos = new Vector2(Random.Range(margin, screenW - margin), Random.Range(margin, margin * 2));
                        break;
                    case 2: // left
                        pos = new Vector2(Random.Range(margin, margin * 2), Random.Range(margin, screenH - margin));
                        break;
                    default: // right
                        pos = new Vector2(screenW - Random.Range(margin, margin * 2), Random.Range(margin, screenH - margin));
                        break;
                }

                positions.Add(pos);
            }

            return positions;
        }

        public bool HasPhraseHistory()
        {
            return _recentPlayerPhrases.Count > 0;
        }
    }
}
