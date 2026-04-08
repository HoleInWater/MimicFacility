using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MimicFacility.Core;

namespace MimicFacility.UI
{
    public class KeybindUI : MonoBehaviour
    {
        [Header("Layout")]
        [SerializeField] private RectTransform contentParent;
        [SerializeField] private float rowHeight = 40f;
        [SerializeField] private float rowSpacing = 4f;

        [Header("Buttons")]
        [SerializeField] private Button applyButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Button resetAllButton;

        [Header("Style")]
        [SerializeField] private Font font;
        [SerializeField] private TMP_FontAsset tmpFont;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color conflictColor = new Color(1f, 0.3f, 0.3f, 1f);
        [SerializeField] private Color listeningColor = new Color(1f, 0.85f, 0.3f, 1f);

        private readonly List<RowEntry> _rows = new List<RowEntry>();
        private readonly Dictionary<string, KeyCode> _pendingBindings = new Dictionary<string, KeyCode>();
        private string _listeningAction;
        private RowEntry _listeningRow;

        private struct RowEntry
        {
            public string ActionName;
            public TextMeshProUGUI KeyLabel;
            public Button RebindButton;
            public TextMeshProUGUI RebindButtonLabel;
            public Image Background;
        }

        private void OnEnable()
        {
            BuildUI();

            if (applyButton != null) applyButton.onClick.AddListener(OnApply);
            if (cancelButton != null) cancelButton.onClick.AddListener(OnCancel);
            if (resetAllButton != null) resetAllButton.onClick.AddListener(OnResetAll);
        }

        private void OnDisable()
        {
            _listeningAction = null;
            _listeningRow = default;

            if (applyButton != null) applyButton.onClick.RemoveListener(OnApply);
            if (cancelButton != null) cancelButton.onClick.RemoveListener(OnCancel);
            if (resetAllButton != null) resetAllButton.onClick.RemoveListener(OnResetAll);
        }

        private void Update()
        {
            if (_listeningAction == null)
                return;

            if (Input.anyKeyDown)
            {
                KeyCode pressed = DetectKeyPress();
                if (pressed != KeyCode.None)
                {
                    _pendingBindings[_listeningAction] = pressed;
                    _listeningRow.KeyLabel.text = FormatKeyName(pressed);
                    _listeningRow.RebindButtonLabel.text = "Rebind";
                    _listeningAction = null;
                    _listeningRow = default;
                    RefreshConflicts();
                }
            }
        }

        private void BuildUI()
        {
            var manager = KeybindManager.Instance;
            if (manager == null)
            {
                Debug.LogWarning("KeybindUI: KeybindManager instance not found.");
                return;
            }

            ClearRows();
            _pendingBindings.Clear();

            var bindings = manager.AllBindings;
            for (int i = 0; i < bindings.Count; i++)
            {
                Keybind bind = bindings[i];
                _pendingBindings[bind.actionName] = bind.key;
                CreateRow(bind.actionName, bind.key, i);
            }

            if (contentParent != null)
            {
                float totalHeight = bindings.Count * (rowHeight + rowSpacing);
                contentParent.sizeDelta = new Vector2(contentParent.sizeDelta.x, totalHeight);
            }

            RefreshConflicts();
        }

        private void CreateRow(string actionName, KeyCode currentKey, int index)
        {
            if (contentParent == null)
                return;

            float yOffset = -index * (rowHeight + rowSpacing);

            var rowGo = new GameObject($"Row_{actionName}", typeof(RectTransform), typeof(Image));
            var rowRect = rowGo.GetComponent<RectTransform>();
            rowRect.SetParent(contentParent, false);
            rowRect.anchorMin = new Vector2(0f, 1f);
            rowRect.anchorMax = new Vector2(1f, 1f);
            rowRect.pivot = new Vector2(0.5f, 1f);
            rowRect.anchoredPosition = new Vector2(0f, yOffset);
            rowRect.sizeDelta = new Vector2(0f, rowHeight);

            var rowImage = rowGo.GetComponent<Image>();
            rowImage.color = index % 2 == 0
                ? new Color(0.12f, 0.12f, 0.14f, 0.6f)
                : new Color(0.15f, 0.15f, 0.17f, 0.6f);

            TextMeshProUGUI actionLabel = CreateTMPLabel(rowGo.transform, "ActionLabel",
                FormatActionName(actionName), TextAlignmentOptions.MidlineLeft);
            var actionRect = actionLabel.GetComponent<RectTransform>();
            actionRect.anchorMin = new Vector2(0f, 0f);
            actionRect.anchorMax = new Vector2(0.4f, 1f);
            actionRect.offsetMin = new Vector2(12f, 0f);
            actionRect.offsetMax = Vector2.zero;

            TextMeshProUGUI keyLabel = CreateTMPLabel(rowGo.transform, "KeyLabel",
                FormatKeyName(currentKey), TextAlignmentOptions.Midline);
            var keyRect = keyLabel.GetComponent<RectTransform>();
            keyRect.anchorMin = new Vector2(0.4f, 0f);
            keyRect.anchorMax = new Vector2(0.7f, 1f);
            keyRect.offsetMin = Vector2.zero;
            keyRect.offsetMax = Vector2.zero;

            var btnGo = new GameObject("RebindButton", typeof(RectTransform), typeof(Image), typeof(Button));
            var btnRect = btnGo.GetComponent<RectTransform>();
            btnRect.SetParent(rowGo.transform, false);
            btnRect.anchorMin = new Vector2(0.72f, 0.1f);
            btnRect.anchorMax = new Vector2(0.98f, 0.9f);
            btnRect.offsetMin = Vector2.zero;
            btnRect.offsetMax = Vector2.zero;

            var btnImage = btnGo.GetComponent<Image>();
            btnImage.color = new Color(0.25f, 0.25f, 0.28f, 1f);

            var button = btnGo.GetComponent<Button>();
            var btnColors = button.colors;
            btnColors.normalColor = new Color(0.25f, 0.25f, 0.28f, 1f);
            btnColors.highlightedColor = new Color(0.35f, 0.35f, 0.38f, 1f);
            btnColors.pressedColor = new Color(0.2f, 0.2f, 0.22f, 1f);
            button.colors = btnColors;

            TextMeshProUGUI btnLabel = CreateTMPLabel(btnGo.transform, "ButtonLabel",
                "Rebind", TextAlignmentOptions.Midline);
            var btnLabelRect = btnLabel.GetComponent<RectTransform>();
            btnLabelRect.anchorMin = Vector2.zero;
            btnLabelRect.anchorMax = Vector2.one;
            btnLabelRect.offsetMin = Vector2.zero;
            btnLabelRect.offsetMax = Vector2.zero;
            btnLabel.fontSize = 14f;

            string capturedAction = actionName;
            var entry = new RowEntry
            {
                ActionName = actionName,
                KeyLabel = keyLabel,
                RebindButton = button,
                RebindButtonLabel = btnLabel,
                Background = rowImage
            };

            int rowIndex = _rows.Count;
            button.onClick.AddListener(() => StartListening(capturedAction, rowIndex));
            _rows.Add(entry);
        }

        private TextMeshProUGUI CreateTMPLabel(Transform parent, string name, string text,
            TextAlignmentOptions alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);

            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.alignment = alignment;
            tmp.fontSize = 16f;
            tmp.color = normalColor;
            if (tmpFont != null) tmp.font = tmpFont;

            return tmp;
        }

        private void StartListening(string actionName, int rowIndex)
        {
            if (_listeningAction != null && _listeningRow.RebindButtonLabel != null)
                _listeningRow.RebindButtonLabel.text = "Rebind";

            _listeningAction = actionName;
            _listeningRow = _rows[rowIndex];
            _listeningRow.RebindButtonLabel.text = "Press any key...";
            _listeningRow.RebindButtonLabel.color = listeningColor;
        }

        private void RefreshConflicts()
        {
            var keyCounts = new Dictionary<KeyCode, int>();
            foreach (var kvp in _pendingBindings)
            {
                if (!keyCounts.ContainsKey(kvp.Value))
                    keyCounts[kvp.Value] = 0;
                keyCounts[kvp.Value]++;
            }

            for (int i = 0; i < _rows.Count; i++)
            {
                RowEntry row = _rows[i];
                if (!_pendingBindings.TryGetValue(row.ActionName, out KeyCode key))
                    continue;

                bool conflict = keyCounts.TryGetValue(key, out int count) && count > 1;
                row.KeyLabel.color = conflict ? conflictColor : normalColor;
                row.RebindButtonLabel.color = normalColor;
            }
        }

        private void OnApply()
        {
            var manager = KeybindManager.Instance;
            if (manager == null) return;

            foreach (var kvp in _pendingBindings)
                manager.RebindKey(kvp.Key, kvp.Value);

            manager.Save();
        }

        private void OnCancel()
        {
            _listeningAction = null;
            _listeningRow = default;
            BuildUI();
        }

        private void OnResetAll()
        {
            var manager = KeybindManager.Instance;
            if (manager == null) return;

            manager.ResetToDefaults();
            BuildUI();
        }

        private void ClearRows()
        {
            _rows.Clear();
            if (contentParent == null) return;

            for (int i = contentParent.childCount - 1; i >= 0; i--)
                Destroy(contentParent.GetChild(i).gameObject);
        }

        private static KeyCode DetectKeyPress()
        {
            foreach (KeyCode key in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (key == KeyCode.None) continue;
                if (Input.GetKeyDown(key))
                    return key;
            }
            return KeyCode.None;
        }

        private static string FormatActionName(string actionName)
        {
            if (string.IsNullOrEmpty(actionName))
                return actionName;

            var sb = new System.Text.StringBuilder(actionName.Length + 4);
            sb.Append(actionName[0]);

            for (int i = 1; i < actionName.Length; i++)
            {
                if (char.IsUpper(actionName[i]) && !char.IsUpper(actionName[i - 1]))
                    sb.Append(' ');
                sb.Append(actionName[i]);
            }

            return sb.ToString();
        }

        private static string FormatKeyName(KeyCode key)
        {
            return key switch
            {
                KeyCode.Mouse0 => "LMB",
                KeyCode.Mouse1 => "RMB",
                KeyCode.Mouse2 => "MMB",
                KeyCode.LeftShift => "L-Shift",
                KeyCode.RightShift => "R-Shift",
                KeyCode.LeftControl => "L-Ctrl",
                KeyCode.RightControl => "R-Ctrl",
                KeyCode.LeftAlt => "L-Alt",
                KeyCode.RightAlt => "R-Alt",
                KeyCode.Return => "Enter",
                KeyCode.Escape => "Esc",
                KeyCode.BackQuote => "`",
                _ => key.ToString()
            };
        }
    }
}
